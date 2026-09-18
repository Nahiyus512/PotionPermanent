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
    [Label("Buff controller")]
    [Description("Left click any buff icon in the top-left corner to open the controller: it lists every active buff, shows its description on hover, toggles it with a left click, and favourites it with Alt + left click. Buffs you turn off disappear at once and stay suppressed, even those granted by other mods such as Portable Stations.")]
    public bool BuffController { get; set; } = true;

    [Client]
    [Label("Portable candle buffs")]
    [Description("Makes candle furniture carried in your inventory (water candle and friends) actually light up. Portable Stations only counts them without granting the buff; with this on they show up and can be toggled in the buff controller.")]
    public bool RestoreCandleBuffs { get; set; } = true;

    [Client]
    [Label("Debug logging")]
    [Description("Write detailed scan logs to the log file for troubleshooting.")]
    public bool DebugLogging { get; set; }
}
