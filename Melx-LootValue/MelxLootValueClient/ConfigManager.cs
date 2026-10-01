using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using BepInEx;
using System;
using SPT.Common.Utils;

namespace MelxLootValueClient;

public class ConfigManager
{
    private static string _filePath;

    static ConfigManager()
    {
        string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        _filePath = Path.Combine(assemblyFolder, "config.json");
    }

    public static void SaveConfig(ConfigData data)
    {
        try
        {
            string jsonString = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(_filePath, jsonString);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[Melx-LootValue] Failed saving Config JSON: {ex.Message}");
        }
    }

    public static ConfigData LoadConfig() {
        try
        {
            if (!File.Exists(_filePath))
            {
                ConfigData defaults = new ConfigData();
                SaveConfig(defaults);
                return defaults;
            }

            string jsonString = File.ReadAllText(_filePath);
            return JsonConvert.DeserializeObject<ConfigData>(jsonString);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[Melx-LootValue] Failed reading Config JSON: {ex.Message}");
            return new ConfigData(); // Fallback to prevent mod crashes
        }
    }
}