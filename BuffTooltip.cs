using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;

namespace PotionPermanent;

/// <summary>
/// 增益控制器自己的悬浮提示。
/// 不用框架的 Tooltip：它固定 16 像素一行，而中文字体本身的行高就比 16 大，几行叠在一起会挤成一坨。
/// 这里按字体真实行高（FontAssets.MouseText 的 LineSpacing）算行距，再留一点余量，标题与正文之间也空开。
/// </summary>
internal static class BuffTooltip
{
    private const int MaxTextWidth = 320;
    private const int Padding = 10;
    private const int ExtraLineGap = 6;
    private const int TitleGap = 6;
    private const int MouseOffset = 18;
    private const int FallbackLineHeight = 20;

    private static string _title;
    private static string _body;
    private static int _lineHeight;
    private static int _titleHeight;

    public static void Show(string title, string body)
    {
        _title = title;
        _body = body;
    }

    public static void Show(string body)
    {
        _title = null;
        _body = body;
    }

    public static void Clear()
    {
        _title = null;
        _body = null;
    }

    /// <summary>每帧末尾由 UIRenderer 回调，画完就清空，避免鼠标移开后残留。</summary>
    public static void Draw()
    {
        string title = _title;
        string body = _body;
        Clear();

        if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(body))
        {
            return;
        }

        try
        {
            DrawNow(title, body);
        }
        catch (Exception ex)
        {
            Mod.LogDebug("Tooltip draw failed: " + ex.Message);
        }
    }

    private static void DrawNow(string title, string body)
    {
        int lineHeight = LineHeight();
        int titleHeight = _titleHeight;

        List<string> lines = new List<string>();
        if (!string.IsNullOrEmpty(title))
        {
            lines.Add(title);
        }

        int bodyStart = lines.Count;
        if (!string.IsNullOrEmpty(body))
        {
            foreach (string raw in body.Split('\n'))
            {
                Wrap(raw, MaxTextWidth, lines);
            }
        }

        if (lines.Count == 0)
        {
            return;
        }

        int textWidth = 0;
        foreach (string line in lines)
        {
            textWidth = Math.Max(textWidth, TextUtil.MeasureWidth(line));
        }

        int width = Math.Min(textWidth, MaxTextWidth) + Padding * 2;
        int height = Padding * 2;
        for (int i = 0; i < lines.Count; i++)
        {
            height += (i < bodyStart) ? titleHeight : lineHeight;
        }

        if (bodyStart > 0 && lines.Count > bodyStart)
        {
            height += TitleGap;
        }

        int x = WidgetInput.MouseX + MouseOffset;
        int y = WidgetInput.MouseY + MouseOffset;
        if (x + width > WidgetInput.ScreenWidth - 4)
        {
            x = WidgetInput.MouseX - width - 4;
        }

        if (y + height > WidgetInput.ScreenHeight - 4)
        {
            y = WidgetInput.MouseY - height - 4;
        }

        x = Math.Max(4, x);
        y = Math.Max(4, y);

        UIRenderer.DrawRect(x, y, width, height, UIColors.TooltipBg);
        UIRenderer.DrawRectOutline(x, y, width, height, UIColors.Border);

        int cursorY = y + Padding;
        for (int i = 0; i < lines.Count; i++)
        {
            bool isTitle = i < bodyStart;
            UIRenderer.DrawText(lines[i], x + Padding, cursorY, isTitle ? UIColors.TextTitle : UIColors.Text);
            cursorY += isTitle ? titleHeight : lineHeight;
            if (bodyStart > 0 && i == bodyStart - 1 && lines.Count > bodyStart)
            {
                cursorY += TitleGap;
            }
        }
    }

    private static void Wrap(string text, int maxWidth, List<string> output)
    {
        if (string.IsNullOrEmpty(text))
        {
            output.Add(string.Empty);
            return;
        }

        string remaining = text;
        while (remaining.Length > 0)
        {
            if (TextUtil.MeasureWidth(remaining) <= maxWidth)
            {
                output.Add(remaining);
                return;
            }

            int cut = -1;
            for (int i = remaining.Length - 1; i > 0; i--)
            {
                if (remaining[i] == ' ' && TextUtil.MeasureWidth(remaining.Substring(0, i)) <= maxWidth)
                {
                    cut = i;
                    break;
                }
            }

            if (cut <= 0)
            {
                cut = remaining.Length;
                for (int i = 1; i < remaining.Length; i++)
                {
                    if (TextUtil.MeasureWidth(remaining.Substring(0, i)) > maxWidth)
                    {
                        cut = Math.Max(1, i - 1);
                        break;
                    }
                }
            }

            output.Add(remaining.Substring(0, cut));
            remaining = remaining.Substring(cut).TrimStart();
        }
    }

    private static int LineHeight()
    {
        if (_lineHeight > 0)
        {
            return _lineHeight;
        }

        float spacing = MouseFontLineSpacing();
        if (spacing <= 0f)
        {
            _titleHeight = FallbackLineHeight + 2;
            return FallbackLineHeight + ExtraLineGap;
        }

        _lineHeight = (int)Math.Ceiling(spacing) + ExtraLineGap;
        _titleHeight = _lineHeight + 2;
        return _lineHeight;
    }

    private static float MouseFontLineSpacing()
    {
        try
        {
            Type fontAssets = typeof(Main).Assembly.GetType("Terraria.GameContent.FontAssets");
            object asset = fontAssets?.GetField("MouseText", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            object font = asset?.GetType().GetProperty("Value")?.GetValue(asset);
            PropertyInfo lineSpacing = font?.GetType().GetProperty("LineSpacing");
            if (lineSpacing != null)
            {
                return Convert.ToSingle(lineSpacing.GetValue(font));
            }
        }
        catch
        {
        }

        return 0f;
    }
}