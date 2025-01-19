using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class GameWorld : MonoBehaviour
{
    [Header("World")]
    public int Seed;

    [Header("Chunk Settings")]
    public int GenerateChunksWidth = 5; 
    public int GenerateChunksHeight = 5;

    [Header("Cell Settings")]
    [Range(5, 90)] public int ChunkCellsWidth = 20; // How many cells per chunk in width.
    [Range(5, 90)] public int ChunkCellsHeight = 20; // How many cells per chunk in height.
    [Range(1, 12)] public int CellSize = 4; // How large each cell is.

    [Header("Biome generation")]
    /// <summary>
    /// Global noise scale.
    /// </summary>
    [Range(0f, 1f)] public float GlobalNoiseScale = 0.3f;

    /// <summary>
    /// Temperature scale.
    /// </summary>
    [Range(0f, 1f)] public float TemperatureNoiseScale = 0.3f;

    /// <summary>
    /// Humity scale.
    /// </summary>
    [Range(0f, 1f)] public float HumidityNoiseScale = 0.3f;

    /// <summary>
    /// A list of biomes to use.
    /// </summary>
    public List<Biome> Biomes;

    [Header("Prefabs")]
    public Chunk ChunkPrefab;
    public List<GameObject> GrassPrefabs;
    public List<GameObject> FlowerPrefabs;
    public List<GameObject> TreePrefabs;
    public List<GameObject> RockPrefabs;

    private Dictionary<Vector2Int, Chunk> Chunks = new Dictionary<Vector2Int, Chunk>();

    /// <summary>
    /// A public rand to keep things consistent with the chosen seed.
    /// </summary>
    public System.Random Rand;

    /// <summary>
    /// Initialize components.
    /// </summary>
    private void Start()
    {
        Rand = new System.Random((int)Seed);
    }

    /// <summary>
    /// Method called every time one of the properties is modified.
    /// </summary>
    private void OnValidate()
    {       
        if (!Application.isPlaying) return;
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        this.GenerateChunks();
    }

    /// <summary>
    /// Retrieve a chunk at a certain position.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public Chunk GetChunk(int x, int z)
    {
        if (Chunks.ContainsKey(new Vector2Int(x, z)))
        {
            return Chunks[new Vector2Int(x, z)];
        }

        return null;
    }

    /// <summary>
    /// Get the height of a vertex based on its point in the world.
    /// </summary>
    /// <param name="biome"></param>
    /// <param name="worldX"></param>
    /// <param name="worldZ"></param>
    /// <returns></returns>
    public float GetVertexHeight(Biome biome, float worldX, float worldZ)
    {
        // Generate multi-layered Perlin noise using WORLD coordinates
        float noise = Mathf.PerlinNoise((worldX + Seed) * biome.noiseScale, (worldZ + Seed) * biome.noiseScale);
        noise += Mathf.PerlinNoise((worldX + Seed) * biome.noiseScale * 0.5f, (worldZ + Seed) * biome.noiseScale * 0.5f) * 0.5f;
        noise += Mathf.PerlinNoise((worldX + Seed) * biome.noiseScale * 0.25f, (worldZ + Seed) * biome.noiseScale * 0.25f) * 0.25f;
        noise /= 1.75f; // Normalize

        // Scale height based on biome range
        return biome.minHeight + (noise * (biome.maxHeight - biome.minHeight));
    }

    /// <summary>
    /// Convert a set of grid positions to world positions.
    /// </summary>
    /// <param name="gridX"></param>
    /// <param name="gridZ"></param>
    /// <returns></returns>
    public Vector3 GridToWorldPosition(int gridX, int gridZ)
    {
        float worldX = (gridX * ChunkCellsWidth * CellSize);
        float worldZ = (gridZ * ChunkCellsHeight * CellSize);
        return new Vector3(worldX, 0, worldZ);
    }

    /// <summary>
    /// Convert a set of grid positions to world positions including local positions.
    /// </summary>
    /// <param name="gridX"></param>
    /// <param name="gridZ"></param>
    /// <param name="localX"></param>
    /// <param name="localZ"></param>
    /// <returns></returns>
    public Vector3 GridToWorldPosition(int gridX, int gridZ, int localX, int localZ)
    {
        float worldX = (gridX * ChunkCellsWidth * CellSize) + (localX * CellSize);
        float worldZ = (gridZ * ChunkCellsHeight * CellSize) + (localZ * CellSize);
        return new Vector3(worldX, 0, worldZ);
    }


    /// <summary>
    /// Convert a set of world positions to grid positions.
    /// </summary>
    /// <param name="worldX"></param>
    /// <param name="worldZ"></param>
    /// <returns></returns>
    public Vector2Int WorldToGridPosition(float worldX, float worldZ)
    {
        int gridX = Mathf.FloorToInt(worldX / ChunkCellsWidth);
        int gridZ = Mathf.FloorToInt(worldZ / ChunkCellsHeight);
        return new Vector2Int(gridX, gridZ);
    }

    /// <summary>
    /// Generate the initial chunks for the world.
    /// </summary>
    private void GenerateChunks()
    {
        Chunks.Clear();

        for (int z = 0; z < GenerateChunksHeight; z++)
        {
            for (int x = 0; x < GenerateChunksWidth; x++)
            {
                // Step 1: Determine the biome for this chunk
                Biome chunkBiome = Biomes.Random();//GetBiomeForChunk(x, z, false);
                if (chunkBiome == null)
                    throw new System.ArgumentNullException("Failed to find a suitable biome.");

                Chunk newChunk = Instantiate(ChunkPrefab, this.transform);
                newChunk.name = $"Chunk_{x}_{z}";
                newChunk.World = this;
                newChunk.Biome = chunkBiome;

                float cx = (x * ChunkCellsWidth * CellSize);
                float cz = (z * ChunkCellsHeight * CellSize);

                // Save position for the grid position before setting world position.
                newChunk.GridX = x;
                newChunk.GridZ = z;

                // Step 5: Set chunk's position in world space
                newChunk.transform.position = new Vector3(cx, 0, cz);
                newChunk.transform.SetParent(this.transform);

                // Step 4: Initialize the chunk with position & biome
                newChunk.GenerateTerrain();

                // Add to collection.
                Chunks.Add(new Vector2Int(x,z), newChunk);
            }
        }
    }

    /// <summary>
    /// Grab the best biome to be selected for a given chunk. Also include neighbor influence as an optional parameter.
    /// Neighbor influence will keep biomes consistent with one another.
    /// </summary>
    /// <param name="gridX">The X position in the grid.</param>
    /// <param name="gridZ">The Z position in the grid.</param>
    /// <param name="neighborInfluence"></param>
    /// <returns></returns>
    private Biome GetBiomeForChunk(int gridX, int gridZ, bool neighborInfluence)
    {
        float temp = GetTemperatureForChunk(gridX, gridZ);
        float humi = GetHumidityForChunk(gridX, gridZ);

        Biome bestBiome = Biomes.Random();
        float bestScore = float.MaxValue;

        // Should we grab the neighbor biome influence. 
        List<Biome> neighborInfluences = neighborInfluence ? GetNeighborsBiomes(gridX, gridZ) : new List<Biome>();

        foreach (Biome biome in Biomes)
        {
            float tempDiff = Mathf.Abs(temp - Mathf.Lerp(biome.minTemp, biome.maxTemp, 0.5f));
            float humidityDiff = Mathf.Abs(humi - Mathf.Lerp(biome.minHumidity, biome.maxHumidity, 0.5f));

            float biomeScore = tempDiff + humidityDiff;

            // Include the strength of the current biome.
            biomeScore *= (1f - biome.strength);

            // Find if we should upgrade the biome.
            int matchingNeighbors = neighborInfluences.Count(n => n == biome);
            if (matchingNeighbors >= 4) // If 4+ neighbors match, upgrade biome
            {
                bestBiome = GetStrongerBiome(biome);
                break;
            }

            if (biomeScore < bestScore)
            {
                bestScore = biomeScore;
                bestBiome = biome;
            }
        }

        return bestBiome;
    }

    /// <summary>
    /// Grab the temperature for a chunk in the world.
    /// </summary>
    /// <param name="x">The X position in the grid.</param>
    /// <param name="z">The Z position in the grid.</param>
    /// <returns></returns>
    private float GetTemperatureForChunk(int gridX, int gridZ)
    {
        var worldPos = GridToWorldPosition(gridX, gridZ);

        // Apply Perlin Noise using world coordinates
        return Mathf.PerlinNoise((worldPos.x + Seed) * TemperatureNoiseScale, (worldPos.z + Seed) * TemperatureNoiseScale);
    }

    /// <summary>
    /// Grab the humidity for a chunk in the world.
    /// </summary>
    /// <param name="x">The X position in the grid.</param>
    /// <param name="z">The Z position in the grid.</param>
    /// <returns></returns>
    private float GetHumidityForChunk(int gridX, int gridZ)
    {
        var worldPos = GridToWorldPosition(gridX, gridZ);

        // Apply Perlin Noise using world coordinates
        return Mathf.PerlinNoise((worldPos.x + Seed) * HumidityNoiseScale, (worldPos.z + Seed) * HumidityNoiseScale);
    }

    /// <summary>
    /// Retrieve the next level of the biome.
    /// </summary>
    /// <param name="currentBiome"></param>
    /// <returns></returns>
    private Biome GetStrongerBiome(Biome currentBiome)
    {
        Dictionary<string, string> biomeEvolution = new Dictionary<string, string>()
        {
            { "Plains", "SmallHills" },
            { "SmallHills", "LargeHills" },
            { "LargeHills", "SmallMountains" },
            { "SmallMountains", "MediumMountains" },
            { "MediumMountains", "LargeMountains" },
            { "Grassland", "DenseForest" },
            { "DenseForest", "ThickForest" },
            { "ShallowWater", "DeepWater" },
            { "DeepWater", "Ocean" }
        };

        if (biomeEvolution.ContainsKey(currentBiome.name))
            return Biomes.Find(b => b.name == biomeEvolution[currentBiome.name]);

        return currentBiome; // If no upgrade exists, return itself
    }

    /// <summary>
    /// Get a list of neighbor chunk positions.
    /// </summary>
    /// <param name="x">The X position in the grid.</param>
    /// <param name="z">The Z position in the grid.</param>
    /// <returns></returns>
    private List<(int x, int z)> GetNeighbors(int x, int z)
    {
        List<(int x, int z)> neighbors = new List<(int, int)>
        {
            // Cardinal Directions
            (x - 1, z), // Left
            (x + 1, z), // Right
            (x, z - 1), // Bottom
            (x, z + 1), // Up

            // Diagonal Corners
            (x - 1, z - 1), // Bottom-Left
            (x + 1, z - 1), // Bottom-Right
            (x - 1, z + 1), // Top-Left
            (x + 1, z + 1)  // Top-Right
        };

        return neighbors;
    }

    /// <summary>
    /// Get a list of neighbor chunk biomes.
    /// </summary>
    /// <param name="x">The X position in the grid.</param>
    /// <param name="z">The Z position in the grid.</param>
    /// <returns></returns>
    private List<Biome> GetNeighborsBiomes(int x, int z)
    {
        List<Biome> neighbors = new List<Biome>();
        foreach (var neighbor in GetNeighbors(x,z))
        {
            neighbors.Add(GetBiomeForChunk(neighbor.x, neighbor.z, false));
        }

        return neighbors;
    }
}