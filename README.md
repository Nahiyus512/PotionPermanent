# 药水永续（PotionPermanent）

TerrariaModder 模组：当背包或随身存储中同一效果的药水 / 食物达到指定数量时，其增益效果永久保持；
并提供增益控制器——点击左上角增益图标即可开关 / 收藏增益。

仓库按语言分成两套：

```
potion-permanent\
├─ en-US\   英文版源码：Mod.cs、PotionPermanentConfig.cs、BuffControllerText.cs、csproj、manifest.json、README.md
│  └─ bin\  英文版成品：PotionPermanent.dll、manifest.json、README.md
├─ zh-CN\   中文版源码（同上）
│  └─ bin\  中文版成品
├─ BuffSustainer.cs
├─ BuffController.cs                    增益控制器：弹窗、图标开关、收藏、状态存档（与语言无关）
├─ Directory.Build.props / .targets
└─ README.md
```

带界面文字的源码（`Mod.cs`、`PotionPermanentConfig.cs`、`BuffControllerText.cs`）在 `en-US\` 和 `zh-CN\` 里各有一份；
`BuffController.cs` 只放逻辑，界面文字统一从 `BuffControllerText` 取。
增益控制器的开关与收藏存在 `mods\potion-permanent\buff-controller.json`。

## 编译

```powershell
dotnet build zh-CN\PotionPermanent.zh-CN.csproj -c Release                     # 中文版，成品在 zh-CN\bin\
dotnet build en-US\PotionPermanent.en-US.csproj -c Release                     # 英文版，成品在 en-US\bin\
dotnet build zh-CN\PotionPermanent.zh-CN.csproj -c Release -p:DeployMod=true   # 编译并装进 TerrariaModder\mods\
```

需要 Windows、[.NET SDK](https://dotnet.microsoft.com/download)、Terraria 1.4.5，以及已安装的 TerrariaModder。
画界面要用 XNA，程序集从 `$(XnaGacDir)`（默认 `C:\Windows\Microsoft.NET\assembly\GAC_32\`）取，装了 XNA 4.0 可再发行组件即可。