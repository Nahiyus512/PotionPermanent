using System;
using System.Reflection;
using HarmonyLib;
using Terraria;
using TerrariaModder.Core;
using TerrariaModder.Core.Logging;

namespace PotionPermanent;

public class Mod : IMod, IModLifecycle
{
    private static ILogger _log;
    private static PotionPermanentConfig _config;
    private static Harmony _harmony;

    internal static bool Enabled = true;
    internal static int Threshold = 30;
    internal static bool DebugLogging;

    public string Id => "potion-permanent";

    public string Name => "Potion Eternity";

    public string Version => "0.1.1";

    public void Initialize(ModContext context)
    {
        _log = context.Logger;
        _config = context.GetConfig<PotionPermanentConfig>();
        LoadConfig();
        _log.Info("Potion Eternity initializing...");
        try
        {
            _harmony = new Harmony("com.terrariamodder.potionpermanent");
            _log.Info("Patches will be applied when game content is ready");
        }
        catch (Exception ex)
        {
            _log.Error("Init failed: " + ex.Message);
        }
    }

    public void OnConfigChanged()
    {
        LoadConfig();
        _log?.Info("Potion Eternity config reloaded");
    }

    public void OnContentReady(ModContext context)
    {
        if (_harmony != null)
        {
            ApplyPatches();
        }
    }

    public void OnWorldLoad()
    {
        BuffSustainer.Reset();
    }

    public void OnWorldUnload()
    {
        BuffSustainer.Reset();
    }

    public void Unload()
    {
        _harmony?.UnpatchAll("com.terrariamodder.potionpermanent");
        _log?.Info("Potion Eternity unloaded");
    }

    public static void PlayerUpdatePostfix(Player __instance, int i)
    {
        BuffSustainer.OnPlayerUpdate(__instance, i);
    }

    public static void UpdateBuffsPostfix(Player __instance)
    {
        BuffSustainer.KeepInfinite(__instance);
    }

    internal static void LogDebug(string message)
    {
        if (DebugLogging)
        {
            _log?.Debug(message);
        }
    }

    private static void ApplyPatches()
    {
        try
        {
            MethodInfo update = typeof(Player).GetMethod("Update", new[] { typeof(int) });
            MethodInfo updatePostfix = typeof(Mod).GetMethod("PlayerUpdatePostfix", BindingFlags.Static | BindingFlags.Public);
            MethodInfo updateBuffs = typeof(Player).GetMethod("UpdateBuffs", new[] { typeof(int) });
            MethodInfo updateBuffsPostfix = typeof(Mod).GetMethod("UpdateBuffsPostfix", BindingFlags.Static | BindingFlags.Public);

            if (update != null && updatePostfix != null)
            {
                _harmony.Patch(update, postfix: new HarmonyMethod(updatePostfix));
                _log?.Info("Successfully patched Player.Update");
            }
            else
            {
                _log?.Error($"Failed to find patch targets - Update: {update != null}, Postfix: {updatePostfix != null}");
            }

            if (updateBuffs != null && updateBuffsPostfix != null)
            {
                _harmony.Patch(updateBuffs, postfix: new HarmonyMethod(updateBuffsPostfix));
                _log?.Info("Successfully patched Player.UpdateBuffs");
            }
            else
            {
                _log?.Error($"Failed to find patch targets - UpdateBuffs: {updateBuffs != null}, KeepPostfix: {updateBuffsPostfix != null}");
            }
        }
        catch (Exception ex)
        {
            _log?.Error("Patch error: " + ex.Message);
        }
    }

    private static void LoadConfig()
    {
        if (_config == null)
        {
            return;
        }

        Enabled = _config.Enabled;
        Threshold = _config.Threshold;
        DebugLogging = _config.DebugLogging;
    }
}
