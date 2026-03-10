namespace Chairly.Api.Features.Clients;

internal sealed record RecipeProductResponse(
    Guid Id,
    string Name,
    string? Brand,
    string? Quantity,
    int SortOrder);
