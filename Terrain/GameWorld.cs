using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class GameWorld : MonoBehaviour
{
    [Header("Debug")]
    public bool ShowEdgeStitch = false;
    public bool ShowEdgeHeat = true;

    [Header("World")]
    public int Seed;

    [Header("Chunk Settings")]
    public int GenerateChunksWidth = 5; 
    public int GenerateChunksHeight = 5;

    [Header("Cell Settings")]
    [Range(5, 90)] public int ChunkCellsWidth = 20; 
    [Range(5, 90)] public int ChunkCellsHeight = 20;
    [Range(1, 12)] public int CellSize = 4;

    [Header("Biome generation")]
    [Range(-25f, 0f)] public float WorldMinBaseHeight = -25f;
    [Range(0f, 50f)] public float WorldMaxBaseHeight = 50f;

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
        Rand = new System.Random((int)Seed);

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
    public float GetVertexHeight(Chunk chunk, float localX, float localZ)
    {
        return GetVertexHeight(chunk.X, chunk.Z, localX, localZ);
    }

    /// <summary>
    /// Get the height of a vertex based on its point in the world.
    /// </summary>
    /// <param name="gridX"></param>
    /// <param name="gridZ"></param>
    /// <param name="localX"></param>
    /// <param name="localZ"></param>
    /// <returns></returns>
    public float GetVertexHeight(int gridX, int gridZ, float localX, float localZ)
    {
        Vector3 worldPos = GridToWorldPosition(gridX, gridZ, localX, localZ);
        return GetVertexHeight(worldPos.x, worldPos.z);
    }

    /// <summary>
    /// Get the height of a vertex based on its point in the world.
    /// </summary>
    /// <param name="worldX"></param>
    /// <param name="worldZ"></param>
    /// <returns></returns>
    public float GetVertexHeight(float worldX, float worldZ)
    {
        float baseNoise = GetBaseHeightForVertice(worldX, worldZ);

        // Macro noise should be [-0.5, 0.5], then rescaled to have a small effect
        float macro = (Mathf.PerlinNoise(worldX * 0.55f, worldZ * 0.55f) - 0.5f) * 0.2f;

        // Mid-level terrain bumps (small impact)
        float mid = (Mathf.PerlinNoise(worldX * 0.1f, worldZ * 0.1f) - 0.5f) * 0.05f;

        // Local small terrain details (very small impact)
        float local = (Mathf.PerlinNoise(worldX * 0.25f, worldZ * 0.25f) - 0.5f) * 0.02f;

        // Final height calculation
        float terrainHeight = Mathf.Clamp01(baseNoise + macro + mid + local);

        return terrainHeight;
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
    public Vector3 GridToWorldPosition(int gridX, int gridZ, float localX, float localZ)
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
        // Step 1: Clear previous chunks
        Chunks.Clear();

        // Step 2: Pre-create all chunks before generating terrain
        for (int z = 0; z < GenerateChunksHeight; z++)
        {
            for (int x = 0; x < GenerateChunksWidth; x++)
            {
                // Step 2.2: Create the chunk but **don't generate terrain yet**
                Chunk newChunk = Instantiate(ChunkPrefab, this.transform);
                newChunk.name = $"Chunk_{x}_{z}";
                newChunk.World = this;
                newChunk.X = x;
                newChunk.Z = z;

                // Step 2.3: Set world position
                newChunk.transform.position = new Vector3(x * ChunkCellsWidth * CellSize, 0, z * ChunkCellsHeight * CellSize);
                newChunk.transform.SetParent(this.transform);

                // Step 2.4: Add to collection **before terrain generation**
                Chunks.Add(new Vector2Int(x, z), newChunk);
            }
        }

        // Step 3: Now that all chunks exist, generate their terrain
        foreach (var chunk in Chunks.Values)
        {
            chunk.GenerateTerrain();
        }

        foreach (var chunk in Chunks.Values)
        {
            //chunk.GenerateGrass(this);
        }
    }

    /// <summary>
    /// Returns the base height for a vertice.
    /// </summary>
    /// <param name="worldX"></param>
    /// <param name="worldZ"></param>
    /// <returns></returns>
    private float GetBaseHeightForVertice(float worldX, float worldZ)
    {
        float height = 0f;

        // Large-scale terrain features
        height += Mathf.PerlinNoise((worldX + Seed) * GlobalNoiseScale, (worldZ + Seed) * GlobalNoiseScale) * 0.6f;

        // Mid-scale variations (hills)
        height += Mathf.PerlinNoise((worldX + Seed * 2) * (GlobalNoiseScale * 2f),
                                    (worldZ + Seed * 2) * (GlobalNoiseScale * 2f)) * 0.3f;

        // Small-scale terrain details
        height += Mathf.PerlinNoise((worldX + Seed * 3) * (GlobalNoiseScale * 4f),
                                    (worldZ + Seed * 3) * (GlobalNoiseScale * 4f)) * 0.1f;

        return Mathf.Clamp01(height); // Ensure it's in [0,1]
    }

    /// <summary>
    /// Grab the temperature for a chunk in the world.
    /// </summary>
    /// <param name="x">The X position in the grid.</param>
    /// <param name="z">The Z position in the grid.</param>
    /// <returns></returns>
    private float GetTemperatureForVertice(float worldX, float worldZ)
    {
        // Apply Perlin Noise using world coordinates
        return Mathf.PerlinNoise((worldX + Seed) * TemperatureNoiseScale, (worldZ + Seed) * TemperatureNoiseScale);
    }

    /// <summary>
    /// Grab the humidity for a chunk in the world.
    /// </summary>
    /// <param name="x">The X position in the grid.</param>
    /// <param name="z">The Z position in the grid.</param>
    /// <returns></returns>
    private float GetHumidityForVertice(float worldX, float worldZ)
    {
        // Apply Perlin Noise using world coordinates
        return Mathf.PerlinNoise((worldX + Seed) * HumidityNoiseScale, (worldZ + Seed) * HumidityNoiseScale);
    }

    /// <summary>
    /// Retrieve a biome based on its position and height.
    /// </summary>
    /// <param name="baseHeight"></param>
    /// <param name="worldX"></param>
    /// <param name="worldZ"></param>
    /// <returns></returns>
    public Biome GetBiome(float worldX, float worldZ)
    {
        float baseHeight = GetVertexHeight(worldX, worldZ);
        float temp = GetTemperatureForVertice(worldX, worldZ);
        float humid = GetHumidityForVertice(worldX, worldZ);

        Biome bestBiome = null;
        float bestScore = float.MaxValue; // Lower score is better

        var biomes = Biomes.Where(r => r.minHeight <= baseHeight && r.maxHeight >= baseHeight).ToList();
        foreach (var biome in biomes)
        {
            // Calculate how well this biome matches the given temperature and humidity
            float heightScore = Mathf.Abs((biome.minHeight + biome.maxHeight) * 0.5f - baseHeight);
            float tempScore = Mathf.Abs((biome.minTemp + biome.maxTemp) * 0.5f - temp);
            float humidScore = Mathf.Abs((biome.minHumidity + biome.maxHumidity) * 0.5f - humid);
            float totalScore = heightScore + tempScore + humidScore; // Lower is better

            if (totalScore < bestScore)
            {
                bestScore = totalScore;
                bestBiome = biome;
            }
        }

        return bestBiome ?? Biomes.FirstOrDefault();
    }

    /// <summary>
    /// Get a list of neighbor chunk positions.
    /// </summary>
    /// <param name="x">The X position in the grid.</param>
    /// <param name="z">The Z position in the grid.</param>
    /// <returns></returns>
    private List<(int x, int z)> GetNeighbors(Chunk chunk)
    {
        List<(int x, int z)> neighbors = new List<(int, int)>
        {
            // Cardinal Directions
            (chunk.X - 1, chunk.Z), // Left
            (chunk.X + 1, chunk.Z), // Right
            (chunk.X, chunk.Z - 1), // Bottom
            (chunk.X, chunk.Z + 1), // Up

            // Diagonal Corners
            (chunk.X - 1, chunk.Z - 1), // Bottom-Left
            (chunk.X + 1, chunk.Z - 1), // Bottom-Right
            (chunk.X - 1, chunk.Z + 1), // Top-Left
            (chunk.X + 1, chunk.Z + 1)  // Top-Right
        };

        return neighbors;
    }
}