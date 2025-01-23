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

    [Range(0f,1f)] public float WorldEdgeBlend = 1f;

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

    public static List<Vector3Int> GetNeighborOffsets()
    {
        return new List<Vector3Int>
    {
        new Vector3Int(-1, 0, 0),  // Left
        new Vector3Int(1, 0, 0),   // Right
        new Vector3Int(0, 0, -1),  // Bottom
        new Vector3Int(0, 0, 1),   // Top

        new Vector3Int(-1, 0, -1), // Bottom-left diagonal
        new Vector3Int(1, 0, -1),  // Bottom-right diagonal
        new Vector3Int(-1, 0, 1),  // Top-left diagonal
        new Vector3Int(1, 0, 1)    // Top-right diagonal
    };
    }

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
        Vector3 worldPos = GridToWorldPosition(chunk.X, chunk.Z, localX, localZ);

        float x = (worldPos.x + Seed) * chunk.Biome.localNoiseScale;
        float z = (worldPos.z + Seed) * chunk.Biome.localNoiseScale;

        // Generate multi-layered Perlin noise using WORLD coordinates
        float noise = Mathf.PerlinNoise(x, z);
        noise += Mathf.PerlinNoise(x * 0.5f, z * 0.5f) * 0.5f;
        noise += Mathf.PerlinNoise(x * 0.25f, z * 0.25f);
        noise /= 1.2f; // Normalize

        // Scale height based on biome range
        return chunk.Biome.minHeight + (noise * (chunk.Biome.maxHeight - chunk.Biome.minHeight));
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
                // Step 2.1: Determine biome for this chunk
                Biome chunkBiome = GetBiome(new Vector3Int(x, 0, z));
                if (chunkBiome == null)
                    throw new System.ArgumentNullException("Failed to find a suitable biome.");

                // Step 2.2: Create the chunk but **don't generate terrain yet**
                Chunk newChunk = Instantiate(ChunkPrefab, this.transform);
                newChunk.name = $"Chunk_{x}_{z}";
                newChunk.World = this;
                newChunk.Biome = chunkBiome;
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
            chunk.GenerateGrass();
        }
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
    /// Retrieve the next biome in the most basic way possible.
    /// </summary>
    /// <param name="chunkPosition"></param>
    /// <returns></returns>
    private Biome GetBiome(Vector3Int chunkPosition)
    {
        System.Random rng = new System.Random(chunkPosition.x * 73856093 ^ chunkPosition.z * 19349663 ^ Seed);
        int biomeIndex = rng.Next(0, Biomes.Count); // Select biome based on seed
        return Biomes[biomeIndex];
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