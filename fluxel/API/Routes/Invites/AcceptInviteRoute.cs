using System.Net.Http;
using System.Threading.Tasks;
using fluxel.API.Components;
using fluxel.Constants;
using fluxel.Database.Helpers;
using Midori.API.Components.Interfaces;
using Midori.Networking;

namespace fluxel.API.Routes.Invites;

public class AcceptInviteRoute : IFluxelAPIRoute, INeedsAuthorization
{
    public string RoutePath => "/invites/:code";
    public HttpMethod Method => HttpMethod.Post;

    public async Task Handle(FluxelAPIInteraction interaction)
    {
        if (!interaction.TryGetStringParameter("code", out var code))
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, ResponseStrings.InvalidParameter("code", "string"));
            return;
        }

        if (interaction.User.Club != null)
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "You are already in a club");
            return;
        }

        var invite = ClubHelper.GetInvite(code);

        if (invite == null)
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "The provided invite code is invalid.");
            return;
        }

        if (invite.UserID != interaction.UserID)
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "This invite was not made for you.");
            return;
        }

        var club = ClubHelper.Get(invite.ClubID);

        if (club == null)
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "The club this invite goes to doesn't exist.");
            return;
        }

        if (club.IsInClub(interaction.UserID))
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "You are already in this club.");
            return;
        }

        club.AddMember(interaction.UserID);
        await interaction.ReplyMessage(HttpStatusCode.OK, "You have joined the club.");

        ClubHelper.RemoveForUser(interaction.UserID);
    }
}
