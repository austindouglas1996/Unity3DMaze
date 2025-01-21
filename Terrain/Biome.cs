using UnityEngine;

[System.Serializable]
public class Biome
{
    public string name;

    [Header("Height Range")]
    [Range(-50f, 50f)] public float minHeight = 0f;  // Lower bound of the biome
    [Range(-50f, 300f)] public float maxHeight = 10f; // Upper bound of the biome

    [Header("Macro Noise (Large Features)")]
    [Range(0.0001f, 0.01f)] public float macroNoiseScale = 0.001f; // Large terrain shapes (mountains, valleys)
    [Range(-10f, 10f)] public float macroInfluence = 1f; // Strength of large terrain features

    [Header("Mid Noise (Hills, Regional Changes)")]
    [Range(0.001f, 0.05f)] public float midNoiseScale = 0.01f; // Medium terrain features
    [Range(0f, 5f)] public float midInfluence = 1.5f; // Strength of mid-scale features

    [Header("Local Noise (Small Features)")]
    [Range(0.001f, 0.1f)] public float localNoiseScale = 0.02f; // Small terrain bumps (hills, rough terrain)
    [Range(0f, 5f)] public float localInfluence = 1f; // Strength of local terrain details

    [Header("Temperature")]
    public float minTemp;       // Minimum temperature for this biome
    public float maxTemp;       // Maximum temperature for this biome
    public float minHumidity;   // Minimum humidity for this biome
    public float maxHumidity;   // Maximum humidity for this biome

    [Header("Color and Display")]
    public Color terrainColor;

    [Header("Biome settings")]
    [Range(0, 1)] public float treeScale;
    [Range(0, 1)] public float grassScale;
    [Range(0, 1)] public float flowerScale;
    [Range(0, 1)] public float rockScale;

    [Range(0f, 1f)] public float blendFactor = 0.5f;
    [Range(0f, 1f)] public float strength;
}