using TerrariaModder.Core.Config;

namespace PotionPermanent;

public class PotionPermanentConfig : ModConfig
{
    public override int Version => 1;

    [Client]
    [Label("Enabled")]
    [Description("When enabled, a buff is kept permanently as soon as the required amount of that potion or food is stored in your inventory or portable storage.")]
    public bool Enabled { get; set; } = true;

    [Client]
    [Label("Required amount")]
    [Description("The buff is kept permanently once the combined stack of potions or food with that buff reaches this amount.")]
    [Range(1.0, 999.0)]
    public int Threshold { get; set; } = 30;

    [Client]
    [Label("Debug logging")]
    [Description("Write detailed scan logs to the log file for troubleshooting.")]
    public bool DebugLogging { get; set; }
}
