using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace PotionPermanent;

public static class BuffSustainer
{
    private const int ScanIntervalTicks = 30;

    private static int _tickCounter;
    private static readonly Dictionary<int, int> _counts = new Dictionary<int, int>();
    private static readonly HashSet<int> _sustained = new HashSet<int>();

    public static void Reset()
    {
        _tickCounter = 0;
        _sustained.Clear();
    }

    public static void OnPlayerUpdate(Player player, int index)
    {
        if (player == null || index != Main.myPlayer || Main.gameMenu || player.dead || !Mod.Enabled)
        {
            return;
        }

        _tickCounter++;
        if (_tickCounter % ScanIntervalTicks != 0)
        {
            return;
        }

        _tickCounter = 0;
        try
        {
            ScanAndApply(player);
        }
        catch (Exception ex)
        {
            Mod.LogDebug("Sustain error: " + ex.Message);
        }
    }

    public static void KeepInfinite(Player player)
    {
        if (player == null || _sustained.Count == 0 || player.whoAmI != Main.myPlayer)
        {
            return;
        }

        for (int i = 0; i < player.buffType.Length && i < player.buffTime.Length; i++)
        {
            int buffType = player.buffType[i];
            if (buffType > 0 && _sustained.Contains(buffType) && player.buffTime[i] != 2)
            {
                player.buffTime[i] = 2;
            }
        }
    }

    private static void ScanAndApply(Player player)
    {
        _counts.Clear();
        _sustained.Clear();
        Count(player.inventory);
        Count(player.bank.item);
        Count(player.bank2.item);
        Count(player.bank3.item);
        Count(player.bank4.item);

        // 食物类增益互相排斥：只保留“饱食等级”最高的那一种
        int bestFoodBuff = -1;
        int bestFoodPriority = -1;
        foreach (KeyValuePair<int, int> entry in _counts)
        {
            if (entry.Value >= Mod.Threshold && BuffID.Sets.IsFedState[entry.Key])
            {
                int priority = BuffID.Sets.SortingPriorityFoodBuffs[entry.Key];
                if (priority > bestFoodPriority)
                {
                    bestFoodPriority = priority;
                    bestFoodBuff = entry.Key;
                }
            }
        }

        foreach (KeyValuePair<int, int> entry in _counts)
        {
            int buffType = entry.Key;
            if (entry.Value < Mod.Threshold)
            {
                continue;
            }

            if (BuffID.Sets.IsFedState[buffType] && (buffType != bestFoodBuff || HasHigherTierFoodActive(player, buffType)))
            {
                continue;
            }

            _sustained.Add(buffType);
            int buffIndex = player.FindBuffIndex(buffType);
            if (buffIndex < 0)
            {
                // buffTime = 2 的增益不显示倒计时，和篝火 / 心灯一致
                player.AddBuff(buffType, 2, false);
            }
            else if (player.buffTime[buffIndex] != 2)
            {
                player.buffTime[buffIndex] = 2;
            }
        }
    }

    private static bool HasHigherTierFoodActive(Player player, int buffType)
    {
        int priority = BuffID.Sets.SortingPriorityFoodBuffs[buffType];
        for (int i = 0; i < player.buffType.Length; i++)
        {
            int active = player.buffType[i];
            if (active > 0 && BuffID.Sets.IsFedState[active] && BuffID.Sets.SortingPriorityFoodBuffs[active] > priority)
            {
                return true;
            }
        }

        return false;
    }

    private static void Count(Item[] items)
    {
        if (items == null)
        {
            return;
        }

        foreach (Item item in items)
        {
            if (item == null || item.stack <= 0 || !item.consumable)
            {
                continue;
            }

            int buffType = item.buffType;
            if (buffType <= 0 || item.buffTime <= 0 || Main.debuff[buffType])
            {
                continue;
            }

            _counts.TryGetValue(buffType, out int count);
            _counts[buffType] = count + item.stack;
        }
    }
}
