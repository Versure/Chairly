using Chairly.Api.Shared.Mediator;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Notifications.ResetEmailTemplate;

internal sealed record ResetEmailTemplateCommand(string TemplateType)
    : IRequest<OneOf<Success, UpdateEmailTemplate.BadRequest>>;
