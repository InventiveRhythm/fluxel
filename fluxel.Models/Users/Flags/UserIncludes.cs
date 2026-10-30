using System;

namespace fluxel.Models.Users.Flags;

[Flags]
public enum UserIncludes
{
    CreatedAt = 1 << 0,
    LastLogin = 1 << 1,
    Statistics = 1 << 2,
    Following = 1 << 3,
    Flags = 1 << 4
}
