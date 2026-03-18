---
name: backend-endpoint
description: >
  Minimal API endpoint patterns for Chairly backend.
  Use when wiring up API endpoints in VSA slices.
---

# Endpoint Patterns

## POST (returns 201 Created)

```csharp
using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.{Context}.{UseCase};

internal static class {UseCase}Endpoint
{
    public static void Map{UseCase}(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (
            {UseCase}Command command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return Results.Created($"/api/{context}/{result.Id}", result);
        });
    }
}
```

## PUT/PATCH with OneOf (200 OK or 404)

```csharp
group.MapPut("/{id:guid}", async (
    Guid id,
    {UseCase}Command command,
    IMediator mediator,
    CancellationToken cancellationToken) =>
{
    command.Id = id;
    var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
    return result.Match(
        entity => Results.Ok(entity),
        _ => Results.NotFound());
});
```

## DELETE with OneOf (204 No Content or 404)

```csharp
group.MapDelete("/{id:guid}", async (
    Guid id,
    IMediator mediator,
    CancellationToken cancellationToken) =>
{
    var result = await mediator.Send(new {UseCase}Command(id), cancellationToken).ConfigureAwait(false);
    return result.Match(
        _ => Results.NoContent(),
        _ => Results.NotFound());
});
```

## GET list (200 OK)

```csharp
group.MapGet("/", async (
    IMediator mediator,
    CancellationToken cancellationToken) =>
{
    var result = await mediator.Send(new Get{Entities}ListQuery(), cancellationToken).ConfigureAwait(false);
    return Results.Ok(result);
});
```

## Endpoint group file

Location: `Chairly.Api/Features/{Context}/{Context}Endpoints.cs`

```csharp
namespace Chairly.Api.Features.{Context};

internal static class {Context}Endpoints
{
    public static IEndpointRouteBuilder Map{Context}Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/{context}");

        group.Map{UseCase1}();
        group.Map{UseCase2}();

        return app;
    }
}
```

Register in `Program.cs`: `app.Map{Context}Endpoints();`
