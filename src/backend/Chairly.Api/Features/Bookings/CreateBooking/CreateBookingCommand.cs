using System.ComponentModel.DataAnnotations;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812 // Instantiated via ASP.NET Core model binding
namespace Chairly.Api.Features.Bookings.CreateBooking;

internal sealed class CreateBookingCommand : IRequest<OneOf<BookingResponse, NotFound, Conflict>>
{
    [Required]
    public Guid ClientId { get; set; }

    [Required]
    public Guid StaffMemberId { get; set; }

    [Required]
    public DateTimeOffset StartTime { get; set; }

#pragma warning disable CA2227, CA1002, MA0016 // Required for ASP.NET Core model binding
    public List<Guid> ServiceIds { get; set; } = [];
#pragma warning restore CA2227, CA1002, MA0016

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
#pragma warning restore CA1812
