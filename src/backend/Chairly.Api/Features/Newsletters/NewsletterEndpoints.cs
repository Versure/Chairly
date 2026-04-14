using Chairly.Api.Features.Newsletters.CancelNewsletterCampaign;
using Chairly.Api.Features.Newsletters.CreateNewsletterCampaign;
using Chairly.Api.Features.Newsletters.DeleteNewsletterCampaign;
using Chairly.Api.Features.Newsletters.GetNewsletterCampaignDetail;
using Chairly.Api.Features.Newsletters.GetNewsletterCampaignsList;
using Chairly.Api.Features.Newsletters.PreviewNewsletter;
using Chairly.Api.Features.Newsletters.ScheduleNewsletterCampaign;
using Chairly.Api.Features.Newsletters.SendNewsletterCampaign;
using Chairly.Api.Features.Newsletters.TestSendNewsletter;
using Chairly.Api.Features.Newsletters.UnsubscribeNewsletter;
using Chairly.Api.Features.Newsletters.UpdateNewsletterCampaign;

namespace Chairly.Api.Features.Newsletters;

internal static class NewsletterEndpoints
{
    public static IEndpointRouteBuilder MapNewsletterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/newsletters")
            .RequireAuthorization("RequireManager");

        group.MapGetNewsletterCampaignsList();
        group.MapGetNewsletterCampaignDetail();
        group.MapCreateNewsletterCampaign();
        group.MapUpdateNewsletterCampaign();
        group.MapDeleteNewsletterCampaign();
        group.MapScheduleNewsletterCampaign();
        group.MapCancelNewsletterCampaign();
        group.MapSendNewsletterCampaign();
        group.MapTestSendNewsletter();
        group.MapPreviewNewsletter();

        // Public anonymous unsubscribe — mapped at root, not under the secured group.
        app.MapUnsubscribeNewsletter();

        return app;
    }
}
