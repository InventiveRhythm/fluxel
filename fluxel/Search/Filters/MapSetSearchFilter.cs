using System;
using System.Collections.Generic;
using System.Linq;
using fluxel.Database.Extensions;
using fluxel.Models.Maps;
using fluxel.Utils;
using fluXis.Online.API.Models.Maps;

namespace fluxel.Search.Filters;

public class MapSetSearchFilter : SearchFilters<MapSet>
{
    public StatusFlags? Status;
    private MapEffectType effects = 0;
    private bool? effectExtra; // false is none, true is any

    private List<long>? keys;

    private float? bpm;
    private SearchOperator bpmOp;

    public override bool ParseKeyword(string key, string value, SearchOperator op)
    {
        switch (key)
        {
            case "s":
            case "status":
                if (value is "a" or "all")
                {
                    Status = (StatusFlags)int.MaxValue;
                    return true;
                }

                StatusFlags? s = value switch
                {
                    "p" or "pure" or "r" or "ranked" => StatusFlags.Pure,
                    "i" or "impure" => StatusFlags.Impure,
                    "pen" or "pending" => StatusFlags.Pending,
                    "u" or "unsub" or "unsubmitted" => StatusFlags.Unsubmitted,
                    _ => null
                };

                if (s != null)
                {
                    Status ??= 0;
                    Status |= s;
                }

                return true;

            case "bpm":
                if (!float.TryParse(value, out var b))
                    return false;

                bpm = b;
                bpmOp = op;
                return true;

            case "k":
            case "key":
            case "keys":
            {
                if (!int.TryParse(value, out var k))
                    return false;

                keys ??= new List<long>();
                keys.Add(k);
                return true;
            }

            case "e":
            case "eff":
            case "effect":
            case "effects":
            {
                switch (value)
                {
                    case "none":
                        effectExtra = false;
                        return true;

                    case "any":
                        effectExtra = true;
                        return true;

                    case "sv":
                        effects |= MapEffectType.ScrollVelocity;
                        return true;

                    case "ls":
                    case "lane":
                    case "laneswitch":
                        effects |= MapEffectType.LaneSwitch;
                        return true;

                    case "flash":
                        effects |= MapEffectType.Flash;
                        return true;

                    case "pulse":
                        effects |= MapEffectType.Pulse;
                        return true;

                    case "move":
                    case "playfieldmove":
                        effects |= MapEffectType.PlayfieldMove;
                        return true;

                    case "scale":
                    case "playfieldscale":
                        effects |= MapEffectType.PlayfieldScale;
                        return true;

                    case "rotate":
                    case "playfieldrotate":
                        effects |= MapEffectType.PlayfieldRotate;
                        return true;

                    case "fade":
                        effects |= MapEffectType.LayerFade;
                        return true;

                    case "shake":
                        effects |= MapEffectType.Shake;
                        return true;

                    case "shader":
                        effects |= MapEffectType.Shader;
                        return true;

                    case "beatpulse":
                        effects |= MapEffectType.BeatPulse;
                        return true;

                    case "ease":
                    case "hitease":
                        effects |= MapEffectType.HitObjectEase;
                        return true;

                    case "sm":
                    case "scrollmultiply":
                        effects |= MapEffectType.HitObjectEase;
                        return true;

                    case "to":
                    case "offset":
                    case "timeoffset":
                        effects |= MapEffectType.HitObjectEase;
                        return true;

                    default:
                        return false;
                }
            }
        }

        return false;
    }

    public override bool Match(MapSet set)
    {
        Status ??= StatusFlags.Pure;

        if (!Status.Value.HasFlag(set.Status switch
            {
                MapStatus.Blacklisted or MapStatus.Unsubmitted => StatusFlags.Unsubmitted,
                MapStatus.Pending => StatusFlags.Pending,
                MapStatus.Impure => StatusFlags.Impure,
                MapStatus.Pure or MapStatus.Ranked => StatusFlags.Pure,
                _ => throw new ArgumentOutOfRangeException()
            }))
            return false;

        if (bpm != null)
        {
            switch (bpmOp)
            {
                case SearchOperator.Equal:
                    return set.MapsList.Any(x => x.BPM == bpm.Value);

                case SearchOperator.Less:
                    return set.MapsList.Any(x => x.BPM < bpm.Value);

                case SearchOperator.LessOrEqual:
                    return set.MapsList.Any(x => x.BPM <= bpm.Value);

                case SearchOperator.Greater:
                    return set.MapsList.Any(x => x.BPM > bpm.Value);

                case SearchOperator.GreaterOrEqual:
                    return set.MapsList.Any(x => x.BPM >= bpm.Value);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        if (!matchKeys(set))
            return false;

        if (!matchEffects(set))
            return false;

        if (!matchText(set))
            return false;

        return true;
    }

    private bool matchKeys(MapSet set)
    {
        if (keys is null || keys.Count == 0)
            return true;

        return set.MapsList.Any(m => keys.Contains(m.Mode));
    }

    private bool matchEffects(MapSet set)
    {
        if (effectExtra is null && effects is 0)
            return true;

        var any = false;

        foreach (var map in set.MapsList)
        {
            var match = false;

            if (effectExtra is not null)
            {
                if (effectExtra.Value)
                    match = map.Effects > 0;
                else
                    match = map.Effects == 0;
            }
            else if (effects > 0)
                match = map.Effects.HasFlag(effects);

            any |= match;
        }

        return any;
    }

    private bool matchText(MapSet set)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        var words = SearchText.Split(' ');
        var creator = set.GetCreator();
        var tags = set.Tags;

        var matches = true;

        foreach (var word in words)
        {
            var wordMatch = false;
            wordMatch |= set.Title.ContainsLower(word);
            wordMatch |= set.TitleRomanized.ContainsLower(word);
            wordMatch |= set.Artist.ContainsLower(word);
            wordMatch |= set.ArtistRomanized.ContainsLower(word);
            wordMatch |= set.Source.ContainsLower(word);
            wordMatch |= tags.Any(x => x.ContainsLower(word));

            if (creator is not null)
            {
                wordMatch |= creator.Username.ContainsLower(word);

                if (!string.IsNullOrWhiteSpace(creator.DisplayName))
                    wordMatch |= creator.DisplayName.ContainsLower(word);
            }

            matches &= wordMatch;
        }

        return matches;
    }
}

[Flags]
public enum StatusFlags
{
    Unsubmitted = 1 << 0,
    Pending = 1 << 1,
    Impure = 1 << 2,
    Pure = 1 << 3
}
