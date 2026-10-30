using System;

namespace fluxel.Models.Users.Flags;

[Flags]
public enum UserExcludes
{
    Club = 1 << 0
}
