using System;
using System.Drawing;

namespace MelxLootValueClient;

[Serializable]
public class ConfigData
{
    // Default White
    public float LowR { get; set; } = 1f;
    public float LowG { get; set; } = 1f;
    public float LowB { get; set; } = 1f;

    // Default Yellow
    public float MedR { get; set; } = 1f;
    public float MedG { get; set; } = 1f;
    public float MedB { get; set; } = 0f;

    // Default Red
    public float HighR { get; set; } = 1f;
    public float HighG { get; set; } = 0f;
    public float HighB { get; set; } = 0f;

    public int MedThreshold { get; set; } = 10000;
    public int HighThreshold { get; set; } = 100000;
}