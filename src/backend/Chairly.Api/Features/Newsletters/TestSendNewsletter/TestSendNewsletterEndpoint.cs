using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Newsletters.TestSendNewsletter;

internal static class TestSendNewsletterEndpoint
{
    public static void MapTestSendNewsletter(this RouteGroupBuilder group)
    {
        group.MapPost("/campaigns/{id:guid}/test-send", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new TestSendNewsletterCommand(id), cancellationToken).ConfigureAwait(false);
            return result.Match(
                _ => Results.Accepted(),
                _ => Results.NotFound(),
                err => Results.UnprocessableEntity(new { message = err.Message }));
        });
    }
}
