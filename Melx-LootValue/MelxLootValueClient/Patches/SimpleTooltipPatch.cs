using EFT.UI;
using HarmonyLib;
using UnityEngine;

namespace MelxLootValueClient.Patches;

[HarmonyPatch(typeof(SimpleTooltip), nameof(SimpleTooltip.Show))]
public static class SimpleTooltipPatch
{
    [HarmonyPrefix]
    public static void Prefix(ref string text)
    {
        try
        {
            var currentContext = ItemUiContext.Instance?.CurrentItemContext;
            if (currentContext?.Item == null) return;

            var item = currentContext.Item;
            string templateId = item.TemplateId;

            if (Plugin.PriceCache.TryGetValue(templateId, out var valueData))
            {
                // Calculate total slots occupied (Width * Height)
                var size = item.CalculateCellSize();
                int totalSlots = Mathf.Max(1, size.X * size.Y);

                int fleaPrice = (int)valueData.FleaPrice;
                int traderPrice = (int)valueData.BestTraderPrice;

                int fleaPerSlot = fleaPrice / totalSlots;
                int traderPerSlot = traderPrice / totalSlots;

                string fleaFormatted = FormatPriceWithColor(fleaPrice, fleaPerSlot, totalSlots);
                string traderFormatted = FormatPriceWithColor(traderPrice, traderPerSlot, totalSlots, valueData.BestTraderName);

                text += $"\nFlea: {fleaFormatted}\nTrader: {traderFormatted}";
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"[Melx-LootValue] Tooltip Patch Error: {ex.Message}");
        }
    }

    private static string FormatPriceWithColor(int totalPrice, int pricePerSlot, int slots, string traderName = null)
    {
        if (totalPrice <= 0) return "N/A";

        var config = Plugin.Instance; 

        string lowHex  = $"#{ColorUtility.ToHtmlStringRGB(config.LowLevelColor)}";
        string medHex  = $"#{ColorUtility.ToHtmlStringRGB(config.MedLevelColor)}";
        string highHex = $"#{ColorUtility.ToHtmlStringRGB(config.HighLevelColor)}";

        // --- DYNAMIC THRESHOLD CHECK ---
        string colorHex = totalPrice switch
        {
            _ when totalPrice >= config.HighThreshold => highHex,
            _ when totalPrice >= config.MedThreshold  => medHex,
            _ => lowHex
        };

        string priceStr = $"<color={colorHex}>{totalPrice:N0} ₽</color>";

        if (slots > 1)
        {
            // --- DYNAMIC PER-SLOT CHECK ---
            string perSlotColorHex = pricePerSlot switch
            {
                _ when pricePerSlot >= config.HighThreshold => highHex,
                _ when pricePerSlot >= config.MedThreshold  => medHex,
                _ => lowHex
            };

            priceStr += $" (<color={perSlotColorHex}>{pricePerSlot:N0} ₽/s</color>)";
        }

        if (!string.IsNullOrEmpty(traderName)) priceStr += $" ({traderName})";
        return priceStr;
    }
}