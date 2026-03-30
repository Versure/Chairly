using System.ComponentModel.DataAnnotations;
using Chairly.Api.Shared.Mediator;
using OneOf;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Notifications.PreviewEmailTemplate;

internal sealed class PreviewEmailTemplateCommand : IRequest<OneOf<PreviewEmailTemplateResponse, UpdateEmailTemplate.BadRequest>>
{
    [Required]
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
