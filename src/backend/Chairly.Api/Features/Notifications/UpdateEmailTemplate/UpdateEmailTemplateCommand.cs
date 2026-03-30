using System.ComponentModel.DataAnnotations;
using Chairly.Api.Shared.Mediator;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Notifications.UpdateEmailTemplate;

internal sealed class UpdateEmailTemplateCommand : IRequest<OneOf<EmailTemplateResponse, BadRequest>>
{
    public string TemplateType { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MaxLength(5000)]
    public string MainMessage { get; set; } = string.Empty;

    [Required]
    [MaxLength(3000)]
    public string ClosingMessage { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? DateLabel { get; set; }

    [MaxLength(200)]
    public string? ServicesLabel { get; set; }
}
#pragma warning restore CA1812
