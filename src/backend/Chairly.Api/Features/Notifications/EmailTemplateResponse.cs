namespace Chairly.Api.Features.Notifications;

internal sealed record EmailTemplateResponse(
    string TemplateType,
    string Subject,
    string MainMessage,
    string ClosingMessage,
    bool IsCustomized,
    string[] AvailablePlaceholders);
