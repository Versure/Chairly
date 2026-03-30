namespace Chairly.Api.Features.Notifications;

internal sealed record EmailTemplateResponse(
    string TemplateType,
    string Subject,
    string MainMessage,
    string ClosingMessage,
    string? DateLabel,
    string? ServicesLabel,
    bool IsCustomized,
    string[] AvailablePlaceholders);
