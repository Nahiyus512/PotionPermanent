using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using TerrariaModder.Core;
using TerrariaModder.Core.IO;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;

namespace PotionPermanent;

/// <summary>
/// 增益控制器：左键点击左上角任意增益图标打开面板。
/// 面板里列出当前生效的增益，悬浮显示说明，左键开关，Alt+左键收藏（排到最前）。
/// 关闭的增益会被立刻移除，并在之后持续压制（包含便携工作站等其它来源给的增益）。
/// </summary>
public static class BuffController
{
    private const string PanelId = "potion-permanent-buff-controller";
    private const string OwnerModId = "potion-permanent";
    private const string StoreFileName = "buff-controller.json";

    private const int IconsPerRow = 8;
    private const int IconSize = 36;
    private const int IconGap = 6;
    private const int EdgePadding = 10;
    private const int FooterHeight = 30;
    private const int ResetButtonWidth = 88;
    private const int ResetButtonHeight = 24;
    private const int VanillaIconSize = 32;

    private static readonly List<int> _favourites = new List<int>();
    private static readonly List<int> _disabled = new List<int>();
    private static readonly List<int> _entries = new List<int>();
    private static readonly List<int> _activeOrder = new List<int>();
    private static readonly HashSet<int> _activeSet = new HashSet<int>();

    // DraggablePanel 找不到本模组图标时会退回框架默认图标，这里用一个占位对象把它顶掉
    private static readonly object NoIcon = new object();

    private static readonly Dictionary<int, Texture2D> _textureCache = new Dictionary<int, Texture2D>();

    private static DraggablePanel _panel;
    private static string _storePath;
    private static int _lastPanelX = -1;
    private static int _lastPanelY = -1;
    private static Array _buffTextures;
    private static PropertyInfo _assetValueProperty;
    private static FieldInfo _assetValueField;

    public static bool IsOpen => _panel != null && _panel.IsOpen;

    public static bool IsDisabled(int buffType)
    {
        return _disabled.Count > 0 && _disabled.Contains(buffType);
    }

    public static void Initialize(string modFolder)
    {
        try
        {
            _storePath = string.IsNullOrEmpty(modFolder) ? null : Path.Combine(modFolder, StoreFileName);
            Load();

            _panel = new DraggablePanel(PanelId, BuffControllerText.PanelTitle, 350, 220)
            {
                Padding = EdgePadding,
                ClipContent = true,
                CloseOnEscape = true,
                ShowCloseButton = true,
                Draggable = true
            };
            _panel.RegisterDrawCallback(DrawPanel);
        }
        catch (Exception ex)
        {
            _panel = null;
            Mod.LogDebug("Buff controller init failed: " + ex.Message);
        }
    }

    public static void Shutdown()
    {
        ClosePanel();
        if (_panel != null)
        {
            try
            {
                _panel.UnregisterDrawCallback();
            }
            catch
            {
            }
        }

        _panel = null;
        _entries.Clear();
        _activeOrder.Clear();
        _activeSet.Clear();
    }

    public static void TogglePanel()
    {
        if (_panel == null)
        {
            return;
        }

        if (_panel.IsOpen)
        {
            _panel.Close();
            return;
        }

        // 记住上次拖到的位置，下次打开还在原处
        if (_lastPanelX >= 0)
        {
            _panel.Open(_lastPanelX, _lastPanelY);
        }
        else
        {
            _panel.Open();
        }
    }

    public static void ClosePanel()
    {
        if (_panel != null && _panel.IsOpen)
        {
            _panel.Close();
        }
    }

    /// <summary>左上角每个增益图标画完之后的回调，用来接管左键点击。</summary>
    public static void OnBuffIconDrawn(int buffSlotOnPlayer, int x, int y)
    {
        if (_panel == null || !Mod.ControllerEnabled || Main.gameMenu)
        {
            return;
        }

        if (buffSlotOnPlayer < 0 || IgnoreMouseInterface())
        {
            return;
        }

        Player player = LocalPlayer();
        if (player == null || buffSlotOnPlayer >= player.buffType.Length)
        {
            return;
        }

        int buffType = player.buffType[buffSlotOnPlayer];
        if (buffType <= 0)
        {
            return;
        }

        int width = VanillaIconSize;
        int height = VanillaIconSize;
        Texture2D texture = GetBuffTexture(buffType);
        if (texture != null)
        {
            width = texture.Width;
            height = texture.Height;
        }

        if (width <= 0 || height <= 0)
        {
            width = VanillaIconSize;
            height = VanillaIconSize;
        }

        if (!UIRenderer.IsMouseOver(x, y, width, height))
        {
            return;
        }

        if (UIRenderer.MouseLeftClick)
        {
            UIRenderer.ConsumeClick();
            TogglePanel();
        }
    }

    /// <summary>
    /// 便携类家具（水蜡烛等）的增益由 SceneMetrics 的蜡烛计数决定，
    /// 而计数是在扫描结束之后才被补上的，导致 ZoneXxxCandle 一直是 false、增益拿不到。
    /// 这里在玩家更新前按计数把标记补上；被关掉的蜡烛则不补，等于真正失效。
    /// </summary>
    public static void OnPlayerUpdatePrefix(Player player)
    {
        if (!Mod.ControllerEnabled || !Mod.RestoreCandleBuffs)
        {
            return;
        }

        if (player == null || player.whoAmI != Main.myPlayer)
        {
            return;
        }

        try
        {
            SceneMetrics metrics = Player.SceneMetrics;
            if (metrics == null)
            {
                return;
            }

            if (metrics.WaterCandleCount > 0 && !IsDisabled(BuffID.WaterCandle))
            {
                metrics.ZoneWaterCandle = true;
            }

            if (metrics.PeaceCandleCount > 0 && !IsDisabled(BuffID.PeaceCandle))
            {
                metrics.ZonePeaceCandle = true;
            }

            if (metrics.ShadowCandleCount > 0 && !IsDisabled(BuffID.ShadowCandle))
            {
                metrics.ZoneShadowCandle = true;
            }
        }
        catch (Exception ex)
        {
            Mod.LogDebug("Candle zone fix error: " + ex.Message);
        }
    }

    /// <summary>
    /// Player.Update 结束时清理被关掉的增益。
    /// 放在最外层，保证晚于原版以及其它模组（便携工作站、时装增益等）补增益的时机。
    /// </summary>
    public static void OnPlayerUpdatePostfix(Player player)
    {
        if (_disabled.Count == 0 || !Mod.ControllerEnabled || Main.gameMenu)
        {
            return;
        }

        if (player == null || player.whoAmI != Main.myPlayer)
        {
            return;
        }

        try
        {
            for (int i = player.buffType.Length - 1; i >= 0; i--)
            {
                int buffType = player.buffType[i];
                if (buffType > 0 && _disabled.Contains(buffType))
                {
                    player.DelBuff(i);
                }
            }
        }
        catch (Exception ex)
        {
            Mod.LogDebug("Buff suppress error: " + ex.Message);
        }
    }

    private static void DrawPanel()
    {
        if (_panel == null)
        {
            return;
        }

        if (!Mod.ControllerEnabled || Main.gameMenu)
        {
            ClosePanel();
            return;
        }

        Player player = LocalPlayer();
        if (player == null || !player.active)
        {
            ClosePanel();
            return;
        }

        BuildEntries(player);

        int rows = Math.Max(1, (_entries.Count + IconsPerRow - 1) / IconsPerRow);
        _panel.Width = IconsPerRow * IconSize + (IconsPerRow - 1) * IconGap + EdgePadding * 2;
        _panel.Height = _panel.HeaderHeight + EdgePadding + rows * (IconSize + IconGap) + FooterHeight;
        _panel.IconTexture = ModIcon();

        if (!_panel.BeginDraw())
        {
            return;
        }

        _lastPanelX = _panel.X;
        _lastPanelY = _panel.Y;

        bool blocked = _panel.BlockInput;
        int originX = _panel.X + EdgePadding;
        int originY = _panel.Y + _panel.HeaderHeight + EdgePadding;

        if (_entries.Count == 0)
        {
            UIRenderer.DrawText(BuffControllerText.NoBuffs, originX, originY + 6, UIColors.TextDim);
        }

        for (int index = 0; index < _entries.Count; index++)
        {
            int column = index % IconsPerRow;
            int row = index / IconsPerRow;
            DrawEntry(_entries[index], originX + column * (IconSize + IconGap), originY + row * (IconSize + IconGap), blocked, player);
        }

        DrawFooter(originY + rows * (IconSize + IconGap), blocked, player);
        _panel.EndDraw();
    }

    private static void DrawEntry(int buffType, int x, int y, bool blocked, Player player)
    {
        bool disabled = IsDisabled(buffType);
        bool favourite = _favourites.Contains(buffType);
        bool hovered = !blocked && UIRenderer.IsMouseOver(x, y, IconSize, IconSize);

        UIRenderer.DrawRect(x, y, IconSize, IconSize, hovered ? UIColors.ItemHoverBg : UIColors.ItemBg);

        Texture2D texture = GetBuffTexture(buffType);
        if (texture != null)
        {
            UIRenderer.DrawTexture(texture, x + 2, y + 2, IconSize - 4, IconSize - 4, disabled ? (byte)70 : (byte)255);
        }

        if (disabled)
        {
            UIRenderer.DrawRect(x, y, IconSize, IconSize, new Color4(12, 12, 16, 150));
        }

        Color4 outline = disabled ? UIColors.Error : (favourite ? UIColors.Accent : UIColors.Border);
        UIRenderer.DrawRectOutline(x, y, IconSize, IconSize, outline, (disabled || favourite) ? 2 : 1);

        if (!hovered)
        {
            return;
        }

        Tooltip.Set(BuffName(buffType), TooltipBody(buffType, disabled, favourite));

        if (!WidgetInput.MouseLeftClick)
        {
            return;
        }

        WidgetInput.ConsumeClick();
        if (WidgetInput.IsAltHeld)
        {
            ToggleFavourite(buffType);
        }
        else
        {
            ToggleDisabled(buffType, player);
        }
    }

    private static void DrawFooter(int y, bool blocked, Player player)
    {
        int left = _panel.X + EdgePadding;
        int right = _panel.X + _panel.Width - EdgePadding;
        bool showReset = _disabled.Count > 0;
        int buttonX = right - ResetButtonWidth;
        int hintWidth = Math.Max(40, (showReset ? buttonX - left - 8 : right - left));

        UIRenderer.DrawText(TextUtil.Truncate(BuffControllerText.Hint, hintWidth), left, y + 5, UIColors.TextHint);

        if (!showReset)
        {
            return;
        }

        bool hovered = !blocked && UIRenderer.IsMouseOver(buttonX, y, ResetButtonWidth, ResetButtonHeight);
        UIRenderer.DrawRect(buttonX, y, ResetButtonWidth, ResetButtonHeight, hovered ? UIColors.ButtonHover : UIColors.Button);
        UIRenderer.DrawRectOutline(buttonX, y, ResetButtonWidth, ResetButtonHeight, UIColors.Border);
        UIRenderer.DrawText(BuffControllerText.ResetButton, buttonX + 8, y + 5, UIColors.Text);

        if (!hovered)
        {
            return;
        }

        Tooltip.Set(BuffControllerText.ResetTooltip);
        if (WidgetInput.MouseLeftClick)
        {
            WidgetInput.ConsumeClick();
            EnableAll();
        }
    }

    private static void BuildEntries(Player player)
    {
        _entries.Clear();
        _activeOrder.Clear();
        _activeSet.Clear();

        for (int i = 0; i < player.buffType.Length; i++)
        {
            int buffType = player.buffType[i];
            if (buffType > 0 && _activeSet.Add(buffType))
            {
                _activeOrder.Add(buffType);
            }
        }

        // 收藏的排在最前，其余按增益栏里的顺序，被关掉的排在最后
        foreach (int buffType in _favourites)
        {
            if ((_activeSet.Contains(buffType) || _disabled.Contains(buffType)) && !_entries.Contains(buffType))
            {
                _entries.Add(buffType);
            }
        }

        foreach (int buffType in _activeOrder)
        {
            if (!_entries.Contains(buffType))
            {
                _entries.Add(buffType);
            }
        }

        foreach (int buffType in _disabled)
        {
            if (!_entries.Contains(buffType))
            {
                _entries.Add(buffType);
            }
        }
    }

    private static void ToggleDisabled(int buffType, Player player)
    {
        if (_disabled.Contains(buffType))
        {
            _disabled.Remove(buffType);
        }
        else
        {
            _disabled.Add(buffType);
            RemoveNow(player, buffType);
        }

        Save();
    }

    private static void ToggleFavourite(int buffType)
    {
        if (!_favourites.Remove(buffType))
        {
            _favourites.Add(buffType);
        }

        Save();
    }

    private static void EnableAll()
    {
        if (_disabled.Count == 0)
        {
            return;
        }

        _disabled.Clear();
        Save();
    }

    private static void RemoveNow(Player player, int buffType)
    {
        try
        {
            int index = player.FindBuffIndex(buffType);
            if (index < 0)
            {
                return;
            }

            // 走原版移除流程：处理坐骑下马、隐藏饰品等副作用
            Main.TryRemovingBuff(index, buffType);

            index = player.FindBuffIndex(buffType);
            if (index >= 0)
            {
                player.DelBuff(index);
            }
        }
        catch (Exception ex)
        {
            Mod.LogDebug("Buff remove error: " + ex.Message);
        }
    }

    private static string TooltipBody(int buffType, bool disabled, bool favourite)
    {
        StringBuilder builder = new StringBuilder();
        string description = BuffDescription(buffType);
        if (!string.IsNullOrEmpty(description))
        {
            builder.Append(description);
            builder.Append('\n');
        }

        builder.Append(disabled ? BuffControllerText.StateOff : BuffControllerText.StateOn);
        if (favourite)
        {
            builder.Append("  ");
            builder.Append(BuffControllerText.StateFavourite);
        }

        builder.Append('\n');
        builder.Append(BuffControllerText.HintClick);
        builder.Append('\n');
        builder.Append(BuffControllerText.HintFavourite);
        return builder.ToString();
    }

    private static Player LocalPlayer()
    {
        try
        {
            int index = Main.myPlayer;
            if (index < 0 || index >= Main.player.Length)
            {
                return null;
            }

            return Main.player[index];
        }
        catch
        {
            return null;
        }
    }

    private static bool IgnoreMouseInterface()
    {
        try
        {
            return PlayerInput.IgnoreMouseInterface;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 取增益图标贴图。TextureAssets.Buff 的元素是 ReLogic 的 Asset&lt;Texture2D&gt;，
    /// 本模组没有引用 ReLogic（核心框架同样用反射绕开），所以这里反射取 Value 并缓存。
    /// </summary>
    private static Texture2D GetBuffTexture(int buffType)
    {
        if (buffType <= 0)
        {
            return null;
        }

        if (_textureCache.TryGetValue(buffType, out Texture2D cached))
        {
            return cached;
        }

        try
        {
            if (_buffTextures == null)
            {
                _buffTextures = typeof(TextureAssets).GetField("Buff", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as Array;
            }

            if (_buffTextures == null || buffType >= _buffTextures.Length)
            {
                return null;
            }

            object asset = _buffTextures.GetValue(buffType);
            if (asset == null)
            {
                return null;
            }

            Type assetType = asset.GetType();
            if (_assetValueProperty == null && _assetValueField == null)
            {
                _assetValueProperty = assetType.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
                if (_assetValueProperty == null)
                {
                    _assetValueField = assetType.GetField("Value", BindingFlags.Public | BindingFlags.Instance);
                }
            }

            object raw = _assetValueProperty != null ? _assetValueProperty.GetValue(asset, null) : _assetValueField?.GetValue(asset);
            Texture2D texture = raw as Texture2D;
            if (texture != null)
            {
                _textureCache[buffType] = texture;
            }

            return texture;
        }
        catch
        {
            return null;
        }
    }

    private static string BuffName(int buffType)
    {
        try
        {
            return Lang.GetBuffName(buffType) ?? "";
        }
        catch
        {
            return "";
        }
    }

    private static string BuffDescription(int buffType)
    {
        try
        {
            return Lang.GetBuffDescription(buffType) ?? "";
        }
        catch
        {
            return "";
        }
    }

    private static object ModIcon()
    {
        try
        {
            return PluginLoader.GetMod(OwnerModId)?.IconTexture ?? NoIcon;
        }
        catch
        {
            return NoIcon;
        }
    }

    private static void Load()
    {
        _favourites.Clear();
        _disabled.Clear();

        if (string.IsNullOrEmpty(_storePath) || !File.Exists(_storePath))
        {
            return;
        }

        try
        {
            Dictionary<string, object> data = SidecarJson.Deserialize(SidecarJson.ReadText(_storePath));
            ReadIntList(data, "favourites", _favourites);
            ReadIntList(data, "disabled", _disabled);
        }
        catch (Exception ex)
        {
            Mod.LogDebug("Buff controller load failed: " + ex.Message);
        }
    }

    private static void Save()
    {
        if (string.IsNullOrEmpty(_storePath))
        {
            return;
        }

        try
        {
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                ["version"] = 1,
                ["favourites"] = new List<int>(_favourites),
                ["disabled"] = new List<int>(_disabled)
            };
            AtomicFile.WriteText(_storePath, SidecarJson.Serialize(data), new UTF8Encoding(false));
        }
        catch (Exception ex)
        {
            Mod.LogDebug("Buff controller save failed: " + ex.Message);
        }
    }

    private static void ReadIntList(Dictionary<string, object> data, string key, List<int> target)
    {
        if (data == null || !data.TryGetValue(key, out object value))
        {
            return;
        }

        if (!(value is IEnumerable list) || value is string)
        {
            return;
        }

        foreach (object item in list)
        {
            int buffType = Convert.ToInt32(item ?? 0);
            if (buffType > 0 && !target.Contains(buffType))
            {
                target.Add(buffType);
            }
        }
    }
}
