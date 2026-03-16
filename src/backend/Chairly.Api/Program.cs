using Chairly.Api.Features.Billing;
using Chairly.Api.Features.Bookings;
using Chairly.Api.Features.Clients;
using Chairly.Api.Features.Config;
using Chairly.Api.Features.Notifications;
using Chairly.Api.Features.Services;
using Chairly.Api.Features.Settings;
using Chairly.Api.Features.Staff;
using Chairly.Api.Features.Tenants;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Keycloak;
using Chairly.Infrastructure.Messaging;
using Chairly.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddMediator();
builder.Services.AddScoped<InvoiceLineItemBuilder>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ChairlyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ChairlyDb")));

builder.AddRabbitMQClient("messaging");
builder.Services.AddScoped<IBookingEventPublisher, BookingEventPublisher>();
builder.Services.AddHostedService<Chairly.Api.Features.Notifications.Infrastructure.BookingEventConsumer>();
builder.Services.Configure<Chairly.Api.Features.Notifications.Infrastructure.SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<Chairly.Api.Features.Notifications.Infrastructure.IEmailSender, Chairly.Api.Features.Notifications.Infrastructure.SmtpEmailSender>();
builder.Services.AddHostedService<Chairly.Api.Features.Notifications.Infrastructure.NotificationDispatcher>();

// Tenant context: scoped TenantContext resolved via ITenantContext interface.
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

var jwksCache = new KeycloakJwksCache();
builder.Services.AddSingleton(jwksCache);

// JWT Bearer authentication — dynamic multi-issuer validation for realm-per-tenant.
var keycloakUrl = builder.Configuration["Keycloak:Url"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudience = "account",
            IssuerValidator = (issuer, _, _) =>
            {
                if (issuer.StartsWith(keycloakUrl + "/realms/", StringComparison.Ordinal))
                {
                    return issuer;
                }

                throw new SecurityTokenInvalidIssuerException("Untrusted issuer");
            },
            IssuerSigningKeyResolver = (_, securityToken, _, _) =>
            {
                return jwksCache.GetSigningKeys(securityToken.Issuer);
            },
        };
    });

// Authorization policies — role-based access control via Keycloak realm roles.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireOwner", p => p.RequireRole("owner"));
    options.AddPolicy("RequireManager", p => p.RequireRole("owner", "manager"));
    options.AddPolicy("RequireStaff", p => p.RequireRole("owner", "manager", "staff_member"));
});

// Claims transformation: map Keycloak realm_access roles to ClaimTypes.Role.
builder.Services.AddScoped<IClaimsTransformation, KeycloakRoleClaimTransformer>();

// Keycloak Admin API service.
builder.Services.AddKeycloakAdmin();

var app = builder.Build();

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerPathFeature>();

        if (feature?.Error is ValidationException validationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";

            var problemDetails = new ValidationProblemDetails(
                validationException.Errors.ToDictionary(e => e.Key, e => e.Value, StringComparer.Ordinal))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
            };

            await context.Response.WriteAsJsonAsync(problemDetails).ConfigureAwait(false);
        }
    });
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantContextMiddleware>();

app.MapBillingEndpoints();
app.MapBookingEndpoints();
app.MapServiceCategoryEndpoints();
app.MapServiceEndpoints();
app.MapStaffEndpoints();
app.MapClientEndpoints();
app.MapRecipeEndpoints();
app.MapSettingsEndpoints();
app.MapNotificationEndpoints();
app.MapConfigEndpoints();
app.MapTenantEndpoints();

// Rollout model: startup migrations are safe for single-leader and rolling deployments.
// A PostgreSQL advisory lock (key 1_000_000_001) serialises concurrent migration attempts
// so only one instance applies outstanding migrations at a time; all others wait and then
// skip already-applied migrations once they acquire the lock.
//
// For zero-downtime production rollouts with strict control, set Migrations:RunOnStartup=false
// and run "dotnet ef database update" as a dedicated pre-deployment step.
var runMigrations = app.Configuration.GetValue<bool>("Migrations:RunOnStartup", defaultValue: true);

if (runMigrations)
{
    var stoppingToken = app.Lifetime.ApplicationStopping;

    // Use a dedicated scope/connection for the advisory lock so that the migration
    // scope can manage its own connection lifecycle. This allows EF Core to correctly
    // bootstrap __EFMigrationsHistory on a fresh database (pre-opening the migration
    // connection bypasses EF Core's own connection setup and breaks the history check).
    var lockScope = app.Services.CreateAsyncScope();
    try
    {
        var dbLock = lockScope.ServiceProvider.GetRequiredService<ChairlyDbContext>();

        await dbLock.Database.OpenConnectionAsync(stoppingToken).ConfigureAwait(false);
        var lockConn = dbLock.Database.GetDbConnection();

        using (var lockCmd = lockConn.CreateCommand())
        {
            lockCmd.CommandText = "SELECT pg_advisory_lock(1000000001)";
            await lockCmd.ExecuteNonQueryAsync(stoppingToken).ConfigureAwait(false);
        }

        var migrateScope = app.Services.CreateAsyncScope();
        try
        {
            var db = migrateScope.ServiceProvider.GetRequiredService<ChairlyDbContext>();
            await db.Database.MigrateAsync(stoppingToken).ConfigureAwait(false);
        }
        finally
        {
            await migrateScope.DisposeAsync().ConfigureAwait(false);

            using (var unlockCmd = lockConn.CreateCommand())
            {
                unlockCmd.CommandText = "SELECT pg_advisory_unlock(1000000001)";
                await unlockCmd.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
            }

            await dbLock.Database.CloseConnectionAsync().ConfigureAwait(false);
        }
    }
    finally
    {
        await lockScope.DisposeAsync().ConfigureAwait(false);
    }
}

await app.RunAsync().ConfigureAwait(false);
