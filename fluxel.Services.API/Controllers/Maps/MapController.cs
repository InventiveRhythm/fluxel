using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using fluxel.Components;
using fluxel.Components.IPC;
using fluxel.Constants;
using fluxel.Database.Extensions;
using fluxel.Models;
using fluxel.Models.Maps;
using fluxel.Models.Users;
using fluXis.Online.API.Models.Maps;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Networking;
using Newtonsoft.Json;

namespace fluxel.Services.API.Controllers.Maps;

[Controller("/maps")]
public class MapController
{
    private readonly DatabaseContext database;
    private readonly ModelTranslator translator;
    private readonly Valkey? valkey;

    public MapController(DatabaseContext database, ModelTranslator translator, Valkey? valkey = null)
    {
        this.database = database;
        this.translator = translator;
        this.valkey = valkey;
    }

    [HttpRoute("/:id")]
    public APIReturn<APIMap> Get(long id)
    {
        if (!database.Maps.TryFind(out var map, id))
            return Returns.NotFound("map");

        return translator.ToAPI(map);
    }

    #region Rate Vote

    [Authenticated]
    [HttpRoute("/:id/rate", APIMethod.Post)]
    public APIReturn<object> SubmitRateVote(User auth, long id, [Source(ParameterSource.Body)] RateVotePayload payload)
    {
        if (!database.Maps.TryFind(out var map, id))
            return Returns.NotFound("map");

        if (map.MapperIDs.Contains(auth.ID))
            return Returns.Message(HttpStatusCode.BadRequest, ResponseStrings.CannotRateOwnMap);

        using (database.EditAndSave())
        {
            var state = database.MapUserStates.Find(id, auth.ID);
            state ??= database.MapUserStates.Add(new MapUserState { Map = map, User = auth }).Entity;

            if (state.RateChart != null)
                return Returns.Message(HttpStatusCode.BadRequest, ResponseStrings.AlreadyVoted);

            state.RateIsPurifier = database.GroupMembers.Any(x => x.UserID == auth.ID && x.GroupID == "purifier");
            state.RateChart = payload.Base;
            state.RateReading = payload.Reading;
            state.RateTracking = payload.Tracking;
            state.RatePerception = payload.Perception;
        }

        _ = valkey?.Publish(Valkey.MapRating, new ValkeyIDEvent(id));
        return Returns.Okay();
    }

    public class RateVotePayload
    {
        [JsonProperty("base")]
        [Required, Range(0, 20)]
        public float Base { get; set; }

        [JsonProperty("read")]
        [Required, Range(0, 5)]
        public float Reading { get; set; }

        [JsonProperty("track")]
        [Required, Range(0, 5)]
        public float Tracking { get; set; }

        [JsonProperty("percept")]
        [Required, Range(0, 5)]
        public float Perception { get; set; }
    }

    // TODO: move to admin
    /*[Authenticated(Scopes.DEV)]
    [HttpRoute("/:id/refresh-rate")]
    public APIReturn<double> RefreshRating(long id)
    {
        var map = translator.Cache.Maps.Get(id);
        if (map == null) return Returns.NotFound();

        var rating = map.RecalculateRating(maps);
        maps.QuickUpdate(map.ID, m => m.Rating = rating);
        return rating;
    }*/

    #endregion
}
