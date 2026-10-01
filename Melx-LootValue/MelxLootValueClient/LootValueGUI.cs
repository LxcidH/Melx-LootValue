using EFT.ActiveHeadphones;
using EFT.Settings;
using UnityEngine;

namespace MelxLootValueClient;

public class LootValueGUI : MonoBehaviour
{
    private bool showMenu = false;
    private Rect windowRect = new Rect(20, 20, 310, 525);
    private Color lowLevelColor = Color.white;
    private Color medLevelColor = Color.yellow;
    private Color highLevelColor = Color.red;

    private Texture2D colorPreviewTexture;
    ConfigData activeConfig;

    private string medThresholdStr = "10000";
    private string highThresholdStr = "100000";

    private void Awake()
    {
        colorPreviewTexture = new Texture2D(1, 1);
        activeConfig = ConfigManager.LoadConfig();
    }

    private void Start()
    {
        lowLevelColor = Plugin.Instance.LowLevelColor;
        medLevelColor = Plugin.Instance.MedLevelColor;
        highLevelColor = Plugin.Instance.HighLevelColor;

        medThresholdStr = Plugin.Instance.MedThreshold.ToString();
        highThresholdStr = Plugin.Instance.HighThreshold.ToString();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            showMenu = !showMenu;
            Cursor.visible = showMenu;
            Cursor.lockState = showMenu ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }

    private void OnGUI() 
    {
        if (!showMenu) return;
        windowRect = GUI.Window(0, windowRect, DrawWindow, "Melx-LootValue Config");
    }

    private void DrawWindow(int windowID)
    {
        GUILayout.Label("Price Threshold Settings:");
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("Medium Tier Starts At (₽):", GUILayout.Width(160));
        medThresholdStr = GUILayout.TextField(medThresholdStr, GUILayout.Width(100));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("High Tier Starts At (₽):", GUILayout.Width(160));
        highThresholdStr = GUILayout.TextField(highThresholdStr, GUILayout.Width(100));
        GUILayout.EndHorizontal();

        if (int.TryParse(medThresholdStr, out int instantMed)) Plugin.Instance.MedThreshold = instantMed;
        if (int.TryParse(highThresholdStr, out int instantHigh)) Plugin.Instance.HighThreshold = instantHigh;

        GUILayout.Space(15);

        lowLevelColor = DrawColorPicker($"Low Color (Under {FormatString(medThresholdStr)}):", lowLevelColor);
        Plugin.Instance.LowLevelColor = lowLevelColor;

        medLevelColor = DrawColorPicker($"Med Color ({FormatString(medThresholdStr)} - {FormatString(highThresholdStr)}):", medLevelColor);
        Plugin.Instance.MedLevelColor = medLevelColor;

        highLevelColor = DrawColorPicker($"High Color ({FormatString(highThresholdStr)}+):", highLevelColor);
        Plugin.Instance.HighLevelColor = highLevelColor;

        GUILayout.Space(15);
        GUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Load Config"))
        {
            activeConfig = ConfigManager.LoadConfig();

            lowLevelColor = new Color(activeConfig.LowR, activeConfig.LowG, activeConfig.LowB, 1f);
            medLevelColor = new Color(activeConfig.MedR, activeConfig.MedG, activeConfig.MedB, 1f);
            highLevelColor = new Color(activeConfig.HighR, activeConfig.HighG, activeConfig.HighB, 1f);

            medThresholdStr = activeConfig.MedThreshold.ToString();
            highThresholdStr = activeConfig.HighThreshold.ToString();
        }
        
        if (GUILayout.Button("Save Config"))
        {
            int.TryParse(medThresholdStr, out int finalMed);
            int.TryParse(highThresholdStr, out int finalHigh);

            activeConfig.LowR = lowLevelColor.r; activeConfig.LowG = lowLevelColor.g; activeConfig.LowB = lowLevelColor.b;
            activeConfig.MedR = medLevelColor.r; activeConfig.MedG = medLevelColor.g; activeConfig.MedB = medLevelColor.b;
            activeConfig.HighR = highLevelColor.r; activeConfig.HighG = highLevelColor.g; activeConfig.HighB = highLevelColor.b;
            
            activeConfig.MedThreshold = finalMed;
            activeConfig.HighThreshold = finalHigh;
            
            ConfigManager.SaveConfig(activeConfig);      
            Plugin.Log.LogInfo("[Melx-LootValue] Settings saved permanently to JSON file.");
        }

        GUILayout.EndHorizontal();
        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }

    private Color DrawColorPicker(string label, Color currentColor)
    {
        GUILayout.Label(label, GUILayout.ExpandWidth(true));

        int r = (int)(currentColor.r * 255f);
        int g = (int)(currentColor.g * 255f);
        int b = (int)(currentColor.b * 255f);

        GUILayout.BeginHorizontal();
        GUILayout.Label($"R: {r}", GUILayout.Width(45));
        float nextR = GUILayout.HorizontalSlider(currentColor.r, 0f, 1f);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"G: {g}", GUILayout.Width(45));
        float nextG = GUILayout.HorizontalSlider(currentColor.g, 0f, 1f);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"B: {b}", GUILayout.Width(45));
        float nextB = GUILayout.HorizontalSlider(currentColor.b, 0f, 1f);
        GUILayout.EndHorizontal();

        Color selectedColor = new Color(nextR, nextG, nextB, 1f);

        colorPreviewTexture.SetPixel(0, 0, selectedColor);
        colorPreviewTexture.Apply();

        Rect colorRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
        GUI.DrawTexture(colorRect, colorPreviewTexture, ScaleMode.StretchToFill);

        return selectedColor;
    }

    private string FormatString(string input)
    {
        if (int.TryParse(input, out int value)) return value.ToString("N0") + "₽";
        return input;
    }
}
