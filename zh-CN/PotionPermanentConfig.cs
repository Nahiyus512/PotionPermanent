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
    [Label("增益控制器")]
    [Description("左键点击左上角任意增益图标打开控制面板：面板里列出当前生效的增益，悬浮显示说明，左键开关，Alt+左键收藏。被关掉的增益会立刻消失并持续压制，包括便携工作站等其它来源给的增益。")]
    public bool BuffController { get; set; } = true;

    [Client]
    [Label("便携蜡烛增益")]
    [Description("随身携带水蜡烛等蜡烛类家具时让它们真正点亮（便携工作站只统计数量、点不亮增益）。开启后可正常显示，并能在增益控制器里开关。")]
    public bool RestoreCandleBuffs { get; set; } = true;

    [Client]
    [Label("调试日志")]
    [Description("输出详细的扫描日志到日志文件，便于排查问题。")]
    public bool DebugLogging { get; set; }
}
