using fluxel.Components;
using fluxel.Database;
using fluxel.Models.Users;
using fluXis.Online.API.Models.Clubs;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Networking;

namespace fluxel.API.Controllers;

[Controller("/invites")]
public class InvitesController
{
    private readonly ClubManager clubs;
    private readonly ChatManager chats;
    private readonly UserManager users;
    private readonly ModelTranslator translator;

    public InvitesController(ClubManager clubs, ModelTranslator translator, ChatManager chats, UserManager users)
    {
        this.clubs = clubs;
        this.translator = translator;
        this.chats = chats;
        this.users = users;
    }

    [HttpRoute("/:code")]
    public APIReturn<APIClubInvite> Get(string code)
    {
        var invite = clubs.GetInvite(code);

        if (invite == null)
            return Returns.Message(HttpStatusCode.BadRequest, "The provided invite code is invalid.");

        var club = clubs.Get(invite.ClubID);

        if (club == null)
            return Returns.Message(HttpStatusCode.BadRequest, "The club this invite goes to doesn't exist.");

        return translator.ToAPI(invite);
    }

    [Authenticated]
    [HttpRoute("/:code", APIMethod.Post)]
    public APIReturn<object> Accept(User auth, string code)
    {
        if (auth.GetClub(clubs) != null)
            return Returns.Message(HttpStatusCode.BadRequest, "You are already in a club");

        var invite = clubs.GetInvite(code);

        if (invite == null)
            return Returns.Message(HttpStatusCode.BadRequest, "The provided invite code is invalid.");
        if (invite.UserID != auth.ID)
            return Returns.Message(HttpStatusCode.BadRequest, "This invite was not made for you.");

        var club = clubs.Get(invite.ClubID);

        if (club == null)
            return Returns.Message(HttpStatusCode.BadRequest, "The club this invite goes to doesn't exist.");
        if (auth.ClubID == club.ID)
            return Returns.Message(HttpStatusCode.BadRequest, "You are already in this club.");

        users.UpdateLocked(auth.ID, x => x.ClubID = club.ID);
        chats.AddToChannel(club.ChatChannel, auth.ID);
        clubs.RemoveForUser(auth.ID);
        return Returns.Okay();
    }
}
