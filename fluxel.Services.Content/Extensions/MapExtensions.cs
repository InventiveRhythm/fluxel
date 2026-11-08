using System;
using fluxel.Models.Maps;

namespace fluxel.Services.Content.Extensions;

public static class MapExtensions
{
    public static double CalculateRating(this Map map, MapUserState[] states, out bool refresh)
    {
        refresh = false;

        if (states.Length == 0) return 0;

        var count = 0;
        double chart = 0, read = 0, track = 0, percept = 0;

        foreach (var state in states)
        {
            if (state.RateChart == null
                || state.RateReading == null
                || state.RateTracking == null
                || state.RatePerception == null
               ) continue;

            var factor = state.RateIsPurifier ?? false ? 25 : 1;

            chart += state.RateChart.Value * factor;
            read += state.RateReading.Value * factor;
            track += state.RateTracking.Value * factor;
            percept += state.RatePerception.Value * factor;
            count += factor;
        }

        var baseRate = chart / count;
        var effectRate = (read + track + percept) / 3 / count;
        var rating = baseRate + effectRate * 2;

        if (Math.Abs(rating - map.DifficultyRating) > 0.001f)
            refresh = true;

        return rating;
    }
}
