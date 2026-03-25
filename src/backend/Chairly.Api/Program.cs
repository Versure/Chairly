using Chairly.Api.Features.Admin;
using Chairly.Api.Features.Billing;
using Chairly.Api.Features.Bookings;
using Chairly.Api.Features.Clients;
using Chairly.Api.Features.Config;
using Chairly.Api.Features.Dashboard;
using Chairly.Api.Features.Notifications;
using Chairly.Api.Features.Onboarding;
using Chairly.Api.Features.Services;
using Chairly.Api.Features.Settings;
using Chairly.Api.Features.Staff;
using Chairly.Api.Features.Tenants;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Persistence;
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

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddMediator();
builder.Services.AddScoped<InvoiceLineItemBuilder>();
builder.Services.AddScoped<Chairly.Api.Features.Billing.SendInvoice.IInvoicePdfGenerator, Chairly.Api.Features.Billing.SendInvoice.InvoicePdfGenerator>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ChairlyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ChairlyDb")));

builder.Services.AddDbContext<WebsiteDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("WebsiteDb")));

builder.AddRabbitMQClient("messaging");
builder.Services.AddScoped<IBookingEventPublisher, BookingEventPublisher>();
builder.Services.AddHostedService<Chairly.Api.Features.Notifications.Infrastructure.BookingEventConsumer>();
builder.Services.Configure<Chairly.Api.Features.Notifications.Infrastructure.SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<Chairly.Api.Features.Notifications.Infrastructure.IEmailSender, Chairly.Api.Features.Notifications.Infrastructure.SmtpEmailSender>();
builder.Services.AddHostedService<Chairly.Api.Features.Notifications.Infrastructure.NotificationDispatcher>();
builder.Services.Configure<OnboardingSettings>(builder.Configuration.GetSection("Onboarding"));
builder.Services.AddScoped<IOnboardingEventPublisher, Chairly.Api.Features.Onboarding.OnboardingEventPublisher>();

// Tenant context: scoped TenantContext resolved via ITenantContext interface.
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

// JWT Bearer authentication — dynamic multi-issuer validation for realm-per-tenant.
var keycloakUrl = (builder.Configuration["Keycloak:Url"] ?? string.Empty).TrimEnd('/');
var requireHttpsMetadata = !keycloakUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
var jwksCache = new KeycloakJwksCache(requireHttpsMetadata);
builder.Services.AddSingleton(jwksCache);
var keycloakClientId = builder.Configuration["Keycloak:ClientId"];
var adminPortalClientId = builder.Configuration["Keycloak:AdminPortalClientId"];
var validAudiences = new List<string> { "account" };
if (!string.IsNullOrWhiteSpace(keycloakClientId))
{
    validAudiences.Add(keycloakClientId);
}

if (!string.IsNullOrWhiteSpace(adminPortalClientId))
{
    validAudiences.Add(adminPortalClientId);
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudiences = validAudiences.ToArray(),
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
    options.AddPolicy("RequirePlatformAdmin", p => p.RequireRole("platform_admin"));
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
app.MapDashboardEndpoints();
app.MapServiceCategoryEndpoints();
app.MapServiceEndpoints();
app.MapStaffEndpoints();
app.MapClientEndpoints();
app.MapRecipeEndpoints();
app.MapSettingsEndpoints();
app.MapNotificationEndpoints();
app.MapConfigEndpoints();
app.MapTenantEndpoints();
app.MapOnboardingEndpoints();
app.MapAdminEndpoints();

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
    var migrationLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Chairly.Migrations");
    var stoppingToken = app.Lifetime.ApplicationStopping;

    // Use a dedicated, non-pooled connection for the advisory lock so that:
    // 1. The lock is guaranteed to be released when the connection is disposed
    //    (session-level advisory locks are released on session end).
    // 2. The migration scope can manage its own connection lifecycle, allowing
    //    EF Core to correctly bootstrap __EFMigrationsHistory on a fresh database.
    // 3. If the process crashes, the non-pooled connection is closed by the OS,
    //    releasing the lock immediately — no stale locks in the connection pool.
    var connString = app.Configuration.GetConnectionString("ChairlyDb")!;

    // Append Pooling=false so this connection is truly closed on dispose, not
    // returned to Npgsql's pool where a session-level advisory lock would persist.
    var lockConnString = connString.Contains("Pooling=", StringComparison.OrdinalIgnoreCase)
        ? connString
        : connString + ";Pooling=false";

    var lockConn = new Npgsql.NpgsqlConnection(lockConnString);
    try
    {
        await lockConn.OpenAsync(stoppingToken).ConfigureAwait(false);

        var lockCmd = lockConn.CreateCommand();
        try
        {
            lockCmd.CommandText = "SELECT pg_advisory_lock(1000000001)";
            await lockCmd.ExecuteNonQueryAsync(stoppingToken).ConfigureAwait(false);
        }
        finally
        {
            await lockCmd.DisposeAsync().ConfigureAwait(false);
        }

        // On a fresh database, EF Core logs a 'fail' for the initial SELECT from
        // __EFMigrationsHistory because the table doesn't exist yet. This is expected —
        // EF creates the table automatically and proceeds with migrations.
        MigrationLog.ApplyingMigrations(migrationLogger);

        var migrateScope = app.Services.CreateAsyncScope();
        try
        {
            var db = migrateScope.ServiceProvider.GetRequiredService<ChairlyDbContext>();
            await db.Database.MigrateAsync(stoppingToken).ConfigureAwait(false);
        }
        finally
        {
            await migrateScope.DisposeAsync().ConfigureAwait(false);
        }

        MigrationLog.MigrationsApplied(migrationLogger);

        // Explicitly unlock before closing. If this fails (e.g. broken connection),
        // disposing the non-pooled connection will end the session and release the lock.
        try
        {
            var unlockCmd = lockConn.CreateCommand();
            try
            {
                unlockCmd.CommandText = "SELECT pg_advisory_unlock(1000000001)";
                await unlockCmd.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                await unlockCmd.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch (Npgsql.NpgsqlException)
        {
            // Connection already broken — disposing the connection releases the lock.
        }
    }
    finally
    {
        // Truly close the non-pooled TCP connection, releasing any remaining
        // session-level advisory locks even if unlock failed above.
        await lockConn.DisposeAsync().ConfigureAwait(false);
    }
}

// Website database migrations — same advisory-lock pattern with a different lock key (1_000_000_002).
if (runMigrations)
{
    var websiteMigrationLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Chairly.WebsiteMigrations");
    var websiteStoppingToken = app.Lifetime.ApplicationStopping;

    var websiteConnString = app.Configuration.GetConnectionString("WebsiteDb")!;

    var websiteLockConnString = websiteConnString.Contains("Pooling=", StringComparison.OrdinalIgnoreCase)
        ? websiteConnString
        : websiteConnString + ";Pooling=false";

    var websiteLockConn = new Npgsql.NpgsqlConnection(websiteLockConnString);
    try
    {
        await websiteLockConn.OpenAsync(websiteStoppingToken).ConfigureAwait(false);

        var websiteLockCmd = websiteLockConn.CreateCommand();
        try
        {
            websiteLockCmd.CommandText = "SELECT pg_advisory_lock(1000000002)";
            await websiteLockCmd.ExecuteNonQueryAsync(websiteStoppingToken).ConfigureAwait(false);
        }
        finally
        {
            await websiteLockCmd.DisposeAsync().ConfigureAwait(false);
        }

        MigrationLog.ApplyingWebsiteMigrations(websiteMigrationLogger);

        var websiteMigrateScope = app.Services.CreateAsyncScope();
        try
        {
            var websiteDb = websiteMigrateScope.ServiceProvider.GetRequiredService<WebsiteDbContext>();
            await websiteDb.Database.MigrateAsync(websiteStoppingToken).ConfigureAwait(false);
        }
        finally
        {
            await websiteMigrateScope.DisposeAsync().ConfigureAwait(false);
        }

        MigrationLog.WebsiteMigrationsApplied(websiteMigrationLogger);

        try
        {
            var websiteUnlockCmd = websiteLockConn.CreateCommand();
            try
            {
                websiteUnlockCmd.CommandText = "SELECT pg_advisory_unlock(1000000002)";
                await websiteUnlockCmd.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                await websiteUnlockCmd.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch (Npgsql.NpgsqlException)
        {
            // Connection already broken — disposing the connection releases the lock.
        }
    }
    finally
    {
        await websiteLockConn.DisposeAsync().ConfigureAwait(false);
    }
}

if (app.Environment.IsDevelopment())
{
    try
    {
        await KeycloakDevSeeder.SeedAsync(
            app.Services, app.Configuration, app.Lifetime.ApplicationStopping).ConfigureAwait(false);
    }
    catch (HttpRequestException ex)
    {
        var seederLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("KeycloakDevSeeder");
        KeycloakDevSeeder.LogSeederFailed(seederLogger, ex);
    }
    catch (InvalidOperationException ex)
    {
        var seederLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("KeycloakDevSeeder");
        KeycloakDevSeeder.LogSeederFailed(seederLogger, ex);
    }
}

await app.RunAsync().ConfigureAwait(false);
