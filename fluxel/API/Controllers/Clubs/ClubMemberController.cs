using fluxel.Database;
using fluxel.Database.Extensions;
using fluxel.Models.Users;
using fluXis.Online.API.Models.Clubs;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Networking;

namespace fluxel.API.Controllers.Clubs;

[Controller("/clubs/:id/members")]
public class ClubMemberController
{
    private readonly ClubManager clubs;
    private readonly UserManager users;
    private readonly ChatManager chats;

    public ClubMemberController(ClubManager clubs, UserManager users, ChatManager chats)
    {
        this.clubs = clubs;
        this.users = users;
        this.chats = chats;
    }

    [Authenticated]
    [HttpRoute("/", APIMethod.Put)]
    public APIReturn<object> AddMember(User auth, long id, [Source(ParameterSource.Form)] long member)
    {
        var club = clubs.Get(id);

        if (club == null)
            return Returns.NotFound("club");
        if (member != auth.ID)
            return Returns.Message(HttpStatusCode.BadRequest, "You cannot add other people to clubs.");
        if (club.JoinType != ClubJoinType.Open)
            return Returns.Message(HttpStatusCode.Forbidden, "This club is not open to join.");

        var user = users.Get(member);

        if (user == null)
            return Returns.NotFound("user");
        if (club.IsInClub(user.ID))
            return Returns.Message(HttpStatusCode.BadRequest, "You already are in this club!");

        club.AddMember(user.ID, clubs, chats);
        return Returns.Created();
    }

    [Authenticated]
    [HttpRoute("/:member")]
    public APIReturn<object> RemoveMember(User auth, long id, long member)
    {
        var club = clubs.Get(id);

        if (club == null)
            return Returns.NotFound("club");

        var isClubOwner = auth.ID == club.OwnerID;
        var removingSelf = auth.ID == member;
        var isDeveloper = auth.IsDeveloper();

        if (isClubOwner && removingSelf)
            return Returns.Message(HttpStatusCode.BadRequest, "You can't remove yourself from the club you own.");
        if (!isClubOwner && !removingSelf && !isDeveloper)
            return Returns.Message(HttpStatusCode.Forbidden, "Only the club owner can remove other members.");
        if (!club.IsInClub(member))
            return Returns.Message(HttpStatusCode.BadRequest, "This user is not a member of this club.");

        club.RemoveMember(member, clubs, chats);
        return Returns.NoContent();
    }
}
