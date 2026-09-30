using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using SPT.Common.Http;

namespace MelxLootValueClient;

[BepInPlugin("com.melx.lootvalue", "Melx-LootValue", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    public static ManualLogSource Log { get; private set; } = null!;
    public static Dictionary<string, ItemValueData> PriceCache { get; private set; } = new();

    private void Awake()
    {
        Log = Logger;

        // 1. Download price cache from custom SPT server endpoint
        FetchPricesFromServer();

        // 2. Register Harmony hooks for UI display
        var harmony = new Harmony("com.melx.lootvalue");
        harmony.PatchAll();

        Log.LogInfo("[Melx-LootValue] Client mod loaded successfully.");
    }

    public static void FetchPricesFromServer()
    {
        try
        {
            // RequestHandler automatically includes localhost host/port and current session ID
            string json = RequestHandler.GetJson("/melx-lootvalue/prices");

            if (!string.IsNullOrEmpty(json))
            {
                // Deserialize using the SPT response envelope wrapper to safely extract 'data'
                var response = JsonConvert.DeserializeObject<SptResponseWrapper<Dictionary<string, ItemValueData>>>(json);

                if (response != null && response.err == 0 && response.data != null)
                {
                    PriceCache = response.data;
                    Log.LogInfo($"[Melx-LootValue] Successfully retrieved {PriceCache.Count} item valuations from server.");
                }
                else
                {
                    Log.LogWarning($"[Melx-LootValue] Server returned error code or empty data. Err: {response?.err}");
                }
            }
            else
            {
                Log.LogWarning("[Melx-LootValue] Endpoint /melx-lootvalue/prices returned empty response.");
            }
        }
        catch (Exception ex)
        {
            Log.LogError($"[Melx-LootValue] Error communicating with server: {ex.Message}");
        }
    }
}

// Helper wrapper classes matching your server-side payload structure
public class SptResponseWrapper<T>
{
    public int err { get; set; }
    public string errmsg { get; set; }
    public T data { get; set; }
}