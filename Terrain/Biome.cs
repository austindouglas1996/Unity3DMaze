using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Biome
{
    public string name;

    [Header("Height Range")]
    [Range(-50f, 50f)] public float minHeight = 0f;  // Lower bound of the biome
    [Range(-50f, 300f)] public float maxHeight = 10f; // Upper bound of the biome

    [Header("Noise levels")]
    [Range(0.0001f, 0.01f)] public float macroNoiseScale = 0.001f; // Large terrain shapes (mountains, valleys)
    [Range(0.001f, 0.05f)] public float midNoiseScale = 0.01f; // Medium terrain features
    [Range(0.001f, 0.1f)] public float localNoiseScale = 0.02f; // Small terrain bumps (hills, rough terrain)
    [Range(0.001f, 0.1f)] public float riverNoiseScale = 0.02f; // Small terrain bumps (hills, rough terrain)

    [Header("Noise Influence")]
    [Range(0f, 20f)] public float macroInfluence = 10f; // Strength of large terrain features
    [Range(0f, 10f)] public float midInfluence = 5f; // Strength of mid-scale features
    [Range(0f, 3f)] public float localInfluence = 1f; // Strength of local terrain details

    [Header("Temperature & Humidity")]
    public float minTemp;
    public float maxTemp;
    public float minHumidity;
    public float maxHumidity;

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
