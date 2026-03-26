using Chairly.Api.Shared.Mediator;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Staff.ResetStaffPassword;

internal sealed record ResetStaffPasswordCommand(Guid Id) : IRequest<OneOf<Success, NotFound>>;
