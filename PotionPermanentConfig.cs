using TerrariaModder.Core.Config;

namespace PotionPermanent;

public class PotionPermanentConfig : ModConfig
{
    public override int Version => 1;

    [Client]
    [Label("总开关")]
    [Description("开启后，当背包或随身存储中某药水/食物的数量达标时，其增益效果永久保持。")]
    public bool Enabled { get; set; } = true;

    [Client]
    [Label("生效数量")]
    [Description("同一效果的药水/食物堆叠总数达到该数量时，其增益效果永久保持。")]
    [Range(1.0, 999.0)]
    public int Threshold { get; set; } = 30;

    [Client]
    [Label("调试日志")]
    [Description("输出详细的扫描日志到日志文件，便于排查问题。")]
    public bool DebugLogging { get; set; }
}
