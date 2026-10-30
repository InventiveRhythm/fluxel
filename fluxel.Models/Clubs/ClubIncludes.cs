using System;

namespace fluxel.Models.Clubs;

[Flags]
public enum ClubIncludes
{
    Owner = 1 << 0,
    JoinType = 1 << 1,
    Members = 1 << 2,
    Statistics = 1 << 3,

    Everything = int.MaxValue
}
