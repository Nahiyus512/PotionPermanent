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
    internal static bool ControllerEnabled = true;
    internal static bool RestoreCandleBuffs = true;
    internal static string ModFolder;

    public string Id => "potion-permanent";

    public string Name => "Potion Eternity";

    public string Version => "0.2.0";

    public void Initialize(ModContext context)
    {
        _log = context.Logger;
        _config = context.GetConfig<PotionPermanentConfig>();
        ModFolder = context.ModFolder;
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

        BuffController.Initialize(ModFolder);
    }

    public void OnConfigChanged()
    {
        LoadConfig();
        if (!ControllerEnabled)
        {
            BuffController.ClosePanel();
        }

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
        BuffController.ClosePanel();
    }

    public void Unload()
    {
        _harmony?.UnpatchAll("com.terrariamodder.potionpermanent");
        BuffController.Shutdown();
        _log?.Info("Potion Eternity unloaded");
    }

    public static void PlayerUpdatePrefix(Player __instance)
    {
        BuffController.OnPlayerUpdatePrefix(__instance);
    }

    public static void PlayerUpdatePostfix(Player __instance, int i)
    {
        BuffSustainer.OnPlayerUpdate(__instance, i);
        BuffController.OnPlayerUpdatePostfix(__instance);
    }

    public static void UpdateBuffsPostfix(Player __instance)
    {
        BuffSustainer.KeepInfinite(__instance);
    }

    public static void DrawBuffIconPostfix(int buffSlotOnPlayer, int x, int y)
    {
        BuffController.OnBuffIconDrawn(buffSlotOnPlayer, x, y);
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
            MethodInfo updatePrefix = typeof(Mod).GetMethod("PlayerUpdatePrefix", BindingFlags.Static | BindingFlags.Public);
            MethodInfo updatePostfix = typeof(Mod).GetMethod("PlayerUpdatePostfix", BindingFlags.Static | BindingFlags.Public);
            MethodInfo updateBuffs = typeof(Player).GetMethod("UpdateBuffs", new[] { typeof(int) });
            MethodInfo updateBuffsPostfix = typeof(Mod).GetMethod("UpdateBuffsPostfix", BindingFlags.Static | BindingFlags.Public);
            MethodInfo drawBuffIcon = typeof(Main).GetMethod("DrawBuffIcon", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo drawBuffIconPostfix = typeof(Mod).GetMethod("DrawBuffIconPostfix", BindingFlags.Static | BindingFlags.Public);

            if (update != null && updatePrefix != null && updatePostfix != null)
            {
                _harmony.Patch(update, prefix: new HarmonyMethod(updatePrefix), postfix: new HarmonyMethod(updatePostfix));
                _log?.Info("Successfully patched Player.Update");
            }
            else
            {
                _log?.Error($"Failed to find patch targets - Update: {update != null}, Prefix: {updatePrefix != null}, Postfix: {updatePostfix != null}");
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

            if (drawBuffIcon != null && drawBuffIconPostfix != null)
            {
                _harmony.Patch(drawBuffIcon, postfix: new HarmonyMethod(drawBuffIconPostfix));
                _log?.Info("Successfully patched Main.DrawBuffIcon");
            }
            else
            {
                _log?.Error($"Failed to find patch targets - DrawBuffIcon: {drawBuffIcon != null}, Postfix: {drawBuffIconPostfix != null}");
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
        ControllerEnabled = _config.BuffController;
        RestoreCandleBuffs = _config.RestoreCandleBuffs;
    }
}
