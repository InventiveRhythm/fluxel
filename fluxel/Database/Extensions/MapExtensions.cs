using System.Collections.Generic;
using fluxel.Components;
using fluxel.Models.Maps;
using fluxel.Models.Users;
using fluXis.Online.API.Models.Maps;

namespace fluxel.Database.Extensions;

public static class MapExtensions
{
    private static int getVote(this MapSet map, long user) => map.Votes.GetValueOrDefault(user.ToString(), 0);

    public static void SetVote(this MapSet map, long user, int vote) => map.Votes[user.ToString()] = vote;

    public static APIMapVotes GetVotes(this MapSet map, long user = 0) => new()
    {
        MapID = map.ID,
        YourVote = user == 0 ? 0 : map.getVote(user),
        UpVotes = map.UpVotes,
        DownVotes = map.DownVotes
    };

    public static User? GetCreator(this MapSet set, RequestCache cache) => cache.Users.Get(set.CreatorID);

    public static bool AllowScores(this MapSet set) => set.Status >= MapStatus.Pure;
}
