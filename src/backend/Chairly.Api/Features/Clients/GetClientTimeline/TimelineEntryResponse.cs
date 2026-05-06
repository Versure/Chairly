namespace Chairly.Api.Features.Clients.GetClientTimeline;

internal sealed record TimelineEntryResponse(
    BookingTimelineCardResponse Booking,
    ClientRecipeSummaryResponse? Recipe,
    ClientTimelineInvoiceResponse? Invoice);
