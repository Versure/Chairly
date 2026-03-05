using System.ComponentModel.DataAnnotations;
using Chairly.Api.Shared.Mediator;

#pragma warning disable CA1812 // Instantiated via ASP.NET Core model binding
namespace Chairly.Api.Features.Staff.CreateStaffMember;

internal sealed class CreateStaffMemberCommand : IRequest<StaffMemberResponse>
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [AllowedValues("manager", "staff_member")]
    public string Role { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Color { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? PhotoUrl { get; set; }

    public Dictionary<string, ShiftBlockCommand[]>? Schedule { get; set; }
}
#pragma warning restore CA1812
