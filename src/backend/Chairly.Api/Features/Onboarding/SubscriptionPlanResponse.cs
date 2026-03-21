namespace Chairly.Api.Features.Onboarding;

internal sealed record SubscriptionPlanResponse(
    string Slug,
    string Name,
    int MaxStaff,
    decimal MonthlyPrice,
    decimal AnnualPricePerMonth);
