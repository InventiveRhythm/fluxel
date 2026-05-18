namespace fluxel.Config;

public partial class ServerConfig
{
    public class LimitsConfig
    {
        public int MaxMapSets { get; set; } = 9;
        public int IncreasePerPure { get; set; } = 3;
        public int IncreasePerYear { get; set; } = 4;
    }
}
