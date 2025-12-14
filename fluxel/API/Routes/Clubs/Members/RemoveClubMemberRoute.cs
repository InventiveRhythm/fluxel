using System.Net.Http;
using System.Threading.Tasks;
using fluxel.API.Components;
using fluxel.Constants;
using fluxel.Database.Extensions;
using Midori.API.Components.Interfaces;
using Midori.Networking;

namespace fluxel.API.Routes.Clubs.Members;

public class RemoveClubMemberRoute : IFluxelAPIRoute, INeedsAuthorization
{
    public string RoutePath => "/club/:clubid/members/:memberid";
    public HttpMethod Method => HttpMethod.Delete;

    public async Task Handle(FluxelAPIInteraction interaction)
    {
        if (!interaction.TryGetLongParameter("clubid", out var clubid))
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, ResponseStrings.InvalidParameter("clubid", "long"));
            return;
        }

        if (!interaction.TryGetLongParameter("memberid", out var memberid))
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, ResponseStrings.InvalidParameter("memberid", "long"));
            return;
        }

        var club = interaction.Cache.GetClub(clubid);

        if (club == null)
        {
            await interaction.ReplyMessage(HttpStatusCode.NotFound, ResponseStrings.ClubNotFound);
            return;
        }

        var isClubOwner = interaction.UserID == club.OwnerID;
        var removingSelf = interaction.UserID == memberid;
        var isDeveloper = interaction.User.IsDeveloper();

        if (isClubOwner && removingSelf)
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "You can't remove yourself from the club you own.");
            return;
        }

        if (!isClubOwner && !removingSelf && !isDeveloper)
        {
            await interaction.ReplyMessage(HttpStatusCode.Forbidden, "Only the club owner can remove other members.");
            return;
        }

        if (!club.IsInClub(memberid))
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "This user is not a member of this club.");
            return;
        }

        club.RemoveMember(memberid);
        await interaction.Reply(HttpStatusCode.NoContent);
    }
}
