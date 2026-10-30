namespace fluxel.Models.Maps.Flags;

public enum MapSetStatus
{
    /// <summary>
    /// fully blocked from being ranked
    /// </summary>
    Blacklisted = -1,

    /// <summary>
    /// map has not been submitted to the queue
    /// </summary>
    Unsubmitted = 0,

    /// <summary>
    /// this map is in the queue
    /// </summary>
    Pending = 1,

    /// <summary>
    /// has been denied from being ranked but can still re-submit
    /// </summary>
    Impure = 2,

    /// <summary>
    /// map is ranked and can be scored on
    /// </summary>
    Pure = 3,

    /// <summary>
    /// map can show up in ranked matches. this shows up as "PURE" with a gold background
    /// </summary>
    Ranked = 4
}
