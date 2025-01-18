using UnityEngine;

public class GameWorld : MonoBehaviour
{
    public int Seed;
    public Biome[] Biomes;

    [Header("Biome generation")]
    /// <summary>
    /// Controls the global height scale for initial generation.
    /// </summary>
    [Range(0f, 1f)] public float NoiseScale = 0.3f;

    /// <summary>
    /// Temperature scale.
    /// </summary>
    [Range(0f, 1f)] public float TempNoiseScale = 0.3f;

    /// <summary>
    /// Humity scale.
    /// </summary>
    [Range(0f, 1f)] public float HumNoiseScale = 0.3f;

    /// <summary>
    /// Grab the temperature for a chunk in the world.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    private float GetTemperatureForChunk(int x, int z)
    {
        //return Mathf.PerlinNoise(x * tempNoiseScale, z * tempNoiseScale);
        return Mathf.PerlinNoise((x + Seed) * TempNoiseScale, (z + Seed) * TempNoiseScale);
    }

    /// <summary>
    /// Grab the humidity for a chunk in the world.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    private float GetHumidityForChunk(int x, int z)
    {
        //return Mathf.PerlinNoise(x * humNoiseScale, z * humNoiseScale);
        return Mathf.PerlinNoise((x + Seed) * HumNoiseScale, (z + Seed) * HumNoiseScale);
    }
}