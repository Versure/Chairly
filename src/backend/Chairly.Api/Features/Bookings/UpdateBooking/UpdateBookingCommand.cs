using System.ComponentModel.DataAnnotations;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812 // Instantiated via ASP.NET Core model binding
namespace Chairly.Api.Features.Bookings.UpdateBooking;

internal sealed class UpdateBookingCommand : IRequest<OneOf<BookingResponse, NotFound, Conflict>>
{
    public Guid Id { get; set; }

    [Required]
    public Guid ClientId { get; set; }

    [Required]
    public Guid StaffMemberId { get; set; }

    [Required]
    public DateTimeOffset StartTime { get; set; }

    [Required]
    [MinLength(1)]
#pragma warning disable CA1002, MA0016 // Command DTO — mutable list required for model binding
    public List<Guid> ServiceIds { get; set; } = [];
#pragma warning restore CA1002, MA0016

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
#pragma warning restore CA1812
