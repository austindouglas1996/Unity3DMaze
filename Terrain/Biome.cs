using UnityEngine;

[System.Serializable]
public class Biome
{
    public string name;         // Name of the biome (e.g., Forest, Desert)
    public float minHeight;     // Minimum height for this biome
    public float maxHeight;     // Maximum height for this biome
    [Range(0f, 100f)] public float heightScale = 0.3f;
    [Range(0f, 1f)] public float noiseScale;
    public float minTemp;       // Minimum temperature for this biome
    public float maxTemp;       // Maximum temperature for this biome
    public float minHumidity;   // Minimum humidity for this biome
    public float maxHumidity;   // Maximum humidity for this biome

    [Header("Biome settings")]
    [Range(0, 1)] public float treeScale;
    [Range(0, 1)] public float grassScale;
    [Range(0, 1)] public float flowerScale;
    [Range(0, 1)] public float rockScale;

    public Color terrainColor;  // Color for vertex coloring
}