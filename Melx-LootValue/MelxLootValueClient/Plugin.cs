using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using SPT.Common.Http;
using UnityEngine; // Required for using the Color type structure

namespace MelxLootValueClient;

[BepInPlugin("com.melx.lootvalue", "Melx-LootValue", "1.1.0")]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; private set; } = null!;
    
    public static ManualLogSource Log { get; private set; } = null!;
    public static Dictionary<string, ItemValueData> PriceCache { get; private set; } = new();

    public Color LowLevelColor { get; set; } = Color.white;
    public Color MedLevelColor { get; set; } = Color.yellow;
    public Color HighLevelColor { get; set; } = Color.red;
    public int MedThreshold { get; set; } = 10000;
    public int HighThreshold { get; set; } = 100000;

    private void Awake()
    {
        Instance = this;
        Log = Logger;

        LoadSavedSettings();

        FetchPricesFromServer();

        var harmony = new Harmony("com.melx.lootvalue");
        harmony.PatchAll();

        gameObject.AddComponent<LootValueGUI>();

        Log.LogInfo("[Melx-LootValue] Client mod loaded successfully.");
    }

    public static void FetchPricesFromServer()
    {
        try
        {
            string json = RequestHandler.GetJson("/melx-lootvalue/prices");

            if (!string.IsNullOrEmpty(json))
            {
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
        private void LoadSavedSettings()
    {
        try
        {
            var savedData = ConfigManager.LoadConfig();
            
            LowLevelColor  = new Color(savedData.LowR, savedData.LowG, savedData.LowB, 1f);
            MedLevelColor  = new Color(savedData.MedR, savedData.MedG, savedData.MedB, 1f);
            HighLevelColor = new Color(savedData.HighR, savedData.HighG, savedData.HighB, 1f);
            MedThreshold = savedData.MedThreshold;
            HighThreshold = savedData.HighThreshold;
        }
        catch (Exception ex)
        {
            Log.LogWarning($"[Melx-LootValue] Could not populate saved color preferences, fallback to standard profiles: {ex.Message}");
        }
    }
}

public class SptResponseWrapper<T>
{
    public int err { get; set; }
    public string errmsg { get; set; }
    public T data { get; set; }
}
