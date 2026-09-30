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

                // Explicitly cast double to int to resolve CS0266
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

        // Color coding rule based on total price threshold
        string colorHex = totalPrice switch
        {
            >= 100000 => "#FF4444", // Red (100k+)
            >= 10000  => "#FFD700", // Yellow (10k - 100k)
            _         => "#FFFFFF"  // White (0 - 10k)
        };

        string priceStr = $"<color={colorHex}>{totalPrice:N0} ₽</color>";

        // Append per-slot calculation if item takes up more than 1 slot
        if (slots > 1)
        {
            string perSlotColorHex = pricePerSlot switch
            {
                >= 100000 => "#FF4444",
                >= 10000  => "#FFD700",
                _         => "#FFFFFF"
            };

            priceStr += $" (<color={perSlotColorHex}>{pricePerSlot:N0} ₽/s</color>)";
        }

        if (!string.IsNullOrEmpty(traderName))
        {
            priceStr += $" ({traderName})";
        }

        return priceStr;
    }
}