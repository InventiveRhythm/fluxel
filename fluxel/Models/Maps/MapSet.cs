/*
using fluxel.Database;
using fluXis.Online.API.Models.Maps.Modding;
using Newtonsoft.Json;

namespace fluxel.Models.Maps;

[JsonObject(MemberSerialization.OptIn)]
internal class LegacyMapSet
{
    public bool AddModdingEntry(APIModdingActionType type, long user, MapManager maps, ScoreManager scores, out string error)
    {
        error = "";

        var isVote = type is APIModdingActionType.Approve or APIModdingActionType.Deny;

        if (!isVote)
            return true;

        if (QueueVotes.Any(x => x.UserID == user))
        {
            error = "You have already voted for this map.";
            return false;
        }

        var approve = type == APIModdingActionType.Approve;

        if (approve && Maps.Any(map => !maps.HasRateVoted(user, map)))
        {
            error = "You have not submitted a rate vote for all maps.";
            return false;
        }

        QueueVotes.Add(new ModQueueVote(user, approve));

        if (approve)
        {
            if (QueueVotes.Count(x => x.Approve) >= MapManager.REQUIRED_VOTES)
            {
                Status = MapStatus.Pure;
                DateRanked = DateTimeOffset.UtcNow;

                foreach (var map in Maps)
                    scores.DeleteAllFromMap(map);
            }
        }
        else
            Status = MapStatus.Impure;

        maps.Update(this);
        error = string.Empty;
        return true;
    }
}
*/
