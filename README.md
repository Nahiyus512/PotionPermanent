# 药水永续（PotionPermanent）

TerrariaModder 模组：当背包或随身存储中同一效果的药水 / 食物达到指定数量时，其增益效果永久保持。

## 功能

- 背包、猪猪存钱罐、保险箱、保卫者熔炉、虚空保险库中的同款消耗品累计达到阈值后，对应增益永久生效
- 永久生效期间增益图标不显示倒计时（与篝火、心灯一致）
- 食物类增益自动保留最高等级效果（完美饱食 > 大饱食 > 饱食），互不冲突

## 配置

在游戏内模组菜单 →「配置」页修改（实时生效），文件保存在 `core/configs/potion-permanent.client.json`：

| 配置项 | 说明 |
| --- | --- |
| 启用 | 药水永续功能总开关 |
| 阈值 | 触发永续所需的最小数量（默认 30） |
| 调试日志 | 输出调试日志 |

## 说明

- 存储位置任意（背包或随身容器）都会计入数量
- 数量低于阈值后，已生效的增益会在其时间耗尽后正常消失

## 安装

把 `bin\` 里的 `manifest.json` 和 `PotionPermanent.dll` 复制到 `TerrariaModder\mods\potion-permanent\`，
用 `TerrariaInjector.exe` 启动游戏。

## 编译

需要 Windows、[.NET SDK](https://dotnet.microsoft.com/download)、Terraria 1.4.5，以及已安装的 TerrariaModder。
游戏目录等路径在 `Directory.Build.props` 里改，或者建一个不入库的 `local.props` 覆盖。

```powershell
dotnet build -c Release                     # 编译，产物在 bin\
dotnet build -c Release -p:DeployMod=true   # 编译并直接装进 TerrariaModder\mods\
```

## 目录结构

```
potion-permanent\
├─ Mod.cs                          入口：注册 Harmony 补丁
├─ BuffSustainer.cs                核心逻辑：统计数量、维持增益
├─ PotionPermanentConfig.cs        配置项定义
├─ PotionPermanent.csproj
├─ manifest.json                   模组元数据
├─ Directory.Build.props           本机路径配置
├─ Directory.Build.targets         安装到 TerrariaModder 的逻辑
└─ bin\                            编译产物（可直接复制进 mods\，不入库）
```
