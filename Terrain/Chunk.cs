using UnityEngine;
]
public class Chunk : MonoBehaviour
{
    [SerializeField] public GameWorld World;
    [SerializeField] public Biome Biome;

    private float GetHeight(int x, int z)
    {
        float temperature = GetTemperature(x, z);
        float humidity = GetHumidity(x, z);

        // Calculate the base height before grabbing the actual Y.
        float baseHeight = Mathf.PerlinNoise((x + seed) * globalNoiseScale, (z + seed) * globalNoiseScale);
        Biome currentBiome = GetBiome(baseHeight, x, z);

        // Generate the actual Y.
        float y = Mathf.PerlinNoise(
            (x + seed) * currentBiome.noiseScale * globalNoiseScale,
            (z + seed) * currentBiome.noiseScale * globalNoiseScale) * currentBiome.heightScale;

        return y;
    }


    private void OnValidate()
    {
        
    }

    private void GenerateTerrain()
    {

    }
}