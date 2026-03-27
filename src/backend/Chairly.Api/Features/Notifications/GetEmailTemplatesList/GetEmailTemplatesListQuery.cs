using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Notifications.GetEmailTemplatesList;

internal sealed record GetEmailTemplatesListQuery : IRequest<List<EmailTemplateResponse>>;
