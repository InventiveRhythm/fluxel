using System;
using System.Linq;
using fluxel.Components;
using fluxel.Components.IPC;
using fluxel.Database.Extensions;
using fluxel.Models;
using fluxel.Models.Chat;
using fluxel.Models.Users;
using fluXis.Online.API.Models.Clubs;
using Microsoft.EntityFrameworkCore;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Networking;

namespace fluxel.Services.API.Controllers;

[Controller("/invites")]
public class InvitesController
{
    private readonly DatabaseContext database;
    private readonly ModelTranslator translator;
    private readonly Valkey? valkey;

    public InvitesController(ModelTranslator translator, DatabaseContext database, Valkey? valkey = null)
    {
        this.translator = translator;
        this.database = database;
        this.valkey = valkey;
    }

    [HttpRoute("/:code")]
    public APIReturn<APIClubInvite> Get(string code)
    {
        var invite = database.ClubInvites
                             .Include(x => x.Club)
                             .FirstOrDefault(x => string.Equals(x.InviteCode, code, StringComparison.InvariantCultureIgnoreCase));

        if (invite == null)
            return Returns.Message(HttpStatusCode.BadRequest, "The provided invite code is invalid.");

        return translator.ToAPI(invite);
    }

    [Authenticated]
    [HttpRoute("/:code", APIMethod.Post)]
    public APIReturn<object> Accept(User auth, string code)
    {
        if (auth.ClubID != null)
            return Returns.Message(HttpStatusCode.BadRequest, "You are already in a club");
        if (!database.ClubInvites.TryFind(out var invite, code))
            return Returns.Message(HttpStatusCode.BadRequest, "The provided invite code is invalid.");
        if (invite.UserID != auth.ID)
            return Returns.Message(HttpStatusCode.BadRequest, "This invite was not made for you.");

        using (database.EditAndSave())
        {
            auth.ClubID = invite.ClubID;
            database.ClubInvites.Remove(invite);
            database.ChatChannelMembers.Add(new ChatChannelMember($"club_{invite.ClubID}", auth.ID));
            _ = valkey?.Publish(Valkey.ChatChannelMemberAdd, new ValkeyChannelMemberEvent($"club_{invite.ClubID}", auth.ID));
        }

        return Returns.Okay();
    }
}
