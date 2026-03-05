using Chairly.Api.Features.Services;
using Chairly.Api.Features.Staff;
using Chairly.Api.Shared.Mediator;
using Chairly.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddMediator();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ChairlyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ChairlyDb")));

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

app.MapServiceCategoryEndpoints();
app.MapServiceEndpoints();
app.MapStaffEndpoints();

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
