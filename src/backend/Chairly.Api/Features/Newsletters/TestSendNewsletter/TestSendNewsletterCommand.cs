using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Newsletters.TestSendNewsletter;

internal sealed record TestSendNewsletterCommand(Guid Id) : IRequest<OneOf<Success, NotFound, Unprocessable>>;
