# 药水永续（PotionPermanent）

TerrariaModder 模组：当背包或随身存储中同一效果的药水 / 食物达到指定数量时，其增益效果永久保持。

仓库按语言分成两套：

```
potion-permanent\
├─ en-US\   英文版源码：Mod.cs、PotionPermanentConfig.cs、csproj、manifest.json、README.md
│  └─ bin\  英文版成品：PotionPermanent.dll、manifest.json、README.md
├─ zh-CN\   中文版源码（同上）
│  └─ bin\  中文版成品
├─ BuffSustainer.cs                     
├─ Directory.Build.props / .targets     
└─ README.md
```


## 编译

```powershell
dotnet build zh-CN\PotionPermanent.zh-CN.csproj -c Release                     # 中文版，成品在 zh-CN\bin\
dotnet build en-US\PotionPermanent.en-US.csproj -c Release                     # 英文版，成品在 en-US\bin\
dotnet build zh-CN\PotionPermanent.zh-CN.csproj -c Release -p:DeployMod=true   # 编译并装进 TerrariaModder\mods\
```

需要 Windows、[.NET SDK](https://dotnet.microsoft.com/download)、Terraria 1.4.5，以及已安装的 TerrariaModder。
