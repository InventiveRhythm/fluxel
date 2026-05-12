using System.Collections.Generic;
using System.Linq;
using fluxel.Components;
using fluxel.Database;
using fluXis.Online.API.Models.Maps;
using JetBrains.Annotations;
using Midori.API.Attributes;
using Midori.API.Components;
using Newtonsoft.Json;

namespace fluxel.API.Controllers.Leaderboards;

[Controller("/leaderboards/maps")]
public class MapLeaderboardsController
{
    private readonly ScoreManager scores;
    private readonly ModelTranslator translator;

    public MapLeaderboardsController(ScoreManager scores, ModelTranslator translator)
    {
        this.scores = scores;
        this.translator = translator;
    }

    [HttpRoute("/plays")]
    public APIReturn<List<LeaderboardMap>> MapPlays()
    {
        var list = new List<LeaderboardMap>();

        foreach (var score in scores.All)
        {
            if (score.MapID == 0)
                continue;

            var map = translator.Cache.Maps.Get(score.MapID);

            if (map == null)
                continue;

            var leaderboardMap = list.FirstOrDefault(m => m.Map?.ID == map.ID);

            if (leaderboardMap == null)
            {
                leaderboardMap = new LeaderboardMap
                {
                    Map = translator.ToAPI(map),
                    Plays = 1
                };
                list.Add(leaderboardMap);
            }
            else
            {
                leaderboardMap.Plays++;
            }
        }

        list = list.OrderByDescending(m => m.Plays).ToList();

        for (var i = 0; i < list.Count; i++)
        {
            list[i].Rank = i + 1;
        }

        return list;
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class LeaderboardMap
    {
        [JsonProperty("playcount")]
        public int Plays { get; set; }

        [JsonProperty("map")]
        public APIMap? Map { get; init; }

        [JsonProperty("rank")]
        public int Rank { get; set; }
    }
}
