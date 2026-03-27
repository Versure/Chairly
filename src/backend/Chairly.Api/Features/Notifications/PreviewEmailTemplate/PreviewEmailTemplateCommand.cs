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
    [MaxLength(2000)]
    public string MainMessage { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string ClosingMessage { get; set; } = string.Empty;
}
#pragma warning restore CA1812
