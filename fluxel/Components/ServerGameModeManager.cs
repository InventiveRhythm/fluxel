using fluXis.Modes;
using fluXis.Modes.Keys;

namespace fluxel.Components;

public partial class ServerGameModeManager : GameModeManager
{
    public ServerGameModeManager()
        : base(null!, null!)
    {
        Items.Add(new KeysGameMode());
    }
}
