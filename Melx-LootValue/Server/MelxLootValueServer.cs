using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.Common.Models.Logging;
﻿using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace Server;

public record ItemValueData(double FleaPrice, int BestTraderPrice, string BestTraderName);

[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class MelxLootValueServer(
    ISptLogger<MelxLootValueServer> logger,
    TemplateTable templateTable,
    TradersTable tradersTable) : IOnLoad
{
    // Global static cache containing itemId -> valuation data
    public static readonly Dictionary<string, ItemValueData> PriceCache = new();

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        logger.Info("[Melx-LootValue] Calculating item valuations...");

        // Map handbook and flea prices for quick lookup
        var handbookPrices = templateTable.Handbook.Items.ToDictionary(h => h.Id.ToString(), h => h.Price);
        var fleaPrices = templateTable.Prices.ToDictionary(p => p.Key.ToString(), p => p.Value);

        foreach (var (itemId, itemTemplate) in templateTable.Items)
        {
            string itemIdStr = itemId.ToString();
            double baseHandbookPrice = handbookPrices.TryGetValue(itemIdStr, out var hbPrice) ? Convert.ToDouble(hbPrice) : 0.0;
            double fleaPrice = fleaPrices.TryGetValue(itemIdStr, out var fPrice) ? Convert.ToDouble(fPrice) : 0.0;

            int maxTraderPrice = 0;
            string bestTrader = "None";

            // Evaluate all traders to find who buys this item for the most rubles
            foreach (var trader in tradersTable.Values)
            {
                if (trader?.Base == null || trader.Base.ItemsBuy == null) continue;

                if (IsItemBuyableByTrader(itemTemplate.Parent, trader.Base.ItemsBuy.Category))
                {
                    double buyPriceCoef = 0.0;
                    if (trader.Base.LoyaltyLevels != null && trader.Base.LoyaltyLevels.Count > 0)
                    {
                        var firstLoyaltyLevel = trader.Base.LoyaltyLevels.FirstOrDefault();
                        if (firstLoyaltyLevel != null)
                        {
                            buyPriceCoef = firstLoyaltyLevel.BuyPriceCoefficient ?? 0.0;
                        }
                    }

                    double multiplier = buyPriceCoef / 100.0;
                    int calculatedPrice = (int)Math.Round(baseHandbookPrice * multiplier);

                    if (calculatedPrice > maxTraderPrice)
                    {
                        maxTraderPrice = calculatedPrice;
                        bestTrader = !string.IsNullOrEmpty(trader.Base.Nickname) ? trader.Base.Nickname : trader.Base.Name;
                    }
                }
            }

            PriceCache[itemIdStr] = new ItemValueData(fleaPrice, maxTraderPrice, bestTrader);
        }

        // Save a clean backup/export file directly to your mod directory
        try
        {
            var json = JsonSerializer.Serialize(PriceCache, new JsonSerializerOptions { WriteIndented = false });
            string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user", "mods", "Melx-LootValue");
            Directory.CreateDirectory(outputDir);
            File.WriteAllText(Path.Combine(outputDir, "prices.json"), json);
            logger.Success($"[Melx-LootValue] Saved valuation database to disk ({PriceCache.Count} items processed).");
        }
        catch (Exception ex)
        {
            logger.Error($"[Melx-LootValue] Failed to save prices.json: {ex.Message}");
        }

        logger.Success($"[Melx-LootValue] Successfully calculated valuations for {PriceCache.Count} items.");
        return Task.CompletedTask;
    }

    private static bool IsItemBuyableByTrader(MongoId itemCategoryId, IEnumerable<MongoId>? allowedCategories)
    {
        return allowedCategories != null && allowedCategories.Any(c => c.ToString() == itemCategoryId.ToString());
    }
}

[Injectable]
public class MelxLootValueStaticRouter(JsonUtil jsonUtil, MelxLootValueCallback melxLootValueCallback) : StaticRouter(jsonUtil, [
        new RouteAction<EmptyRequestData>(
            "/melx-lootvalue/prices",
            async (
                url,
                info,
                sessionId,
                output,
                cancellationToken
            ) => await melxLootValueCallback.HandleMelxEmptyStaticRoute(url, info, sessionId)
        )
    ])
{ }

[Injectable]
public class MelxLootValueCallback(ISptLogger<MelxLootValueServer> logger, HttpResponseUtil httpResponseUtil)
{
    public ValueTask<string> HandleMelxEmptyStaticRoute(string url, EmptyRequestData requestData, MongoId sessionId)
    {
        logger.Info($"[Melx-LootValue] Request received. Current cache count: {MelxLootValueServer.PriceCache.Count} items.");

        // If the cache is somehow empty, let's log a warning
        if (MelxLootValueServer.PriceCache.Count == 0)
        {
            logger.Warning("[Melx-LootValue] PriceCache is empty when route was called!");
        }

        var response = httpResponseUtil.GetBody(MelxLootValueServer.PriceCache);
        return new ValueTask<string>(response);
    }
}

public record MelxLootValueRequestData : IRequestData
{
}