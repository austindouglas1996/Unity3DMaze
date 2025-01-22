using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PolygonTerrain))]
public class Chunk : MonoBehaviour
{
    public int X = 0;
    public int Z = 0;

    public Biome Biome;
    public GameWorld World;
    public PolygonTerrain Terrain;

    [Header("Grass")]
    private List<Matrix4x4> grassInstances = new List<Matrix4x4>();
    private Mesh grassMesh;
    private Material grassMaterial;

    /// <summary>
    /// Called when the component is initalized.
    /// </summary>
    private void Start()
    {
        this.Terrain = this.GetComponent<PolygonTerrain>();
    }

    /// <summary>
    /// Update and show certain entities based on distance.
    /// </summary>
    private void Update()
    {
        if (Vector3.Distance(Camera.main.transform.position, transform.position) < 260f)
        {
            //Graphics.DrawMeshInstanced(grassMesh, 0, grassMaterial, grassInstances);
        }
    }

    /// <summary>
    /// Get the height of a specific point in a chunk.
    /// </summary>
    /// <param name="biome"></param>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public float GetHeightInChunk(int localX, int localZ)
    {
        Vector3 worldPos = World.GridToWorldPosition(X, Z, localX, localZ);
        float currentHeight = World.GetVertexHeight(this, localX, localZ);

        var (neighborChunk, distanceFactor) = GetClosestNeighbor(localX, localZ);
        if (neighborChunk != null)
        {
            Vector3Int neighborPos = GetMappedLocalPosition(neighborChunk, localX, localZ);
            float neighborHeight = World.GetVertexHeight(neighborChunk, neighborPos.x, neighborPos.z);

            return Mathf.Lerp(currentHeight, neighborHeight, distanceFactor * World.WorldEdgeBlend);
        }

        return currentHeight;
    }

    /// <summary>
    /// Generate the terrain in this chunk.
    /// </summary>
    /// <exception cref="System.Exception"></exception>
    public void GenerateTerrain()
    {
        if (Biome == null || World == null)
        {
            if (Biome == null && World == null)
                throw new System.Exception("Biome and world is null");
            if (Biome == null)
                throw new System.Exception("Biome is null");
            if (World == null)
                throw new System.Exception("World is null");
        }

        this.Terrain.Generate(World, this);

        GenerateGrass();
        PlaceGenericPrefabByPosition(World.TreePrefabs, Biome.treeScale);
        PlaceGenericPrefabByVertex(World.FlowerPrefabs, Biome.flowerScale, 2f);
        PlaceGenericPrefabByVertex(World.RockPrefabs, Biome.rockScale, 2f);
        PlaceGenericPrefabByVertex(World.GrassPrefabs, Biome.grassScale, 1f);
    }

    /// <summary>
    /// Generate Mesh grass? Idk, ChatGBT made this as I was struggling way too much with how
    /// to render objects of this much value.
    /// </summary>
    private void GenerateGrass()
    {
        GameObject grassPrefab = World.GrassPrefabs[0];
        if (grassPrefab == null) return; // Ensure prefab exists

        LODGroup lodGroup = grassPrefab.GetComponent<LODGroup>();
        if (lodGroup == null) return; // Ensure there's an LOD group

        // Extract the first LOD Mesh and Material
        LOD[] lods = lodGroup.GetLODs();
        if (lods.Length > 0 && lods[0].renderers.Length > 0)
        {
            MeshFilter meshFilter = lods[0].renderers[0].GetComponent<MeshFilter>();
            Renderer renderer = lods[0].renderers[0];

            if (meshFilter != null)
                grassMesh = meshFilter.sharedMesh;

            if (renderer != null)
            {
                grassMaterial = renderer.sharedMaterial;
                grassMaterial.enableInstancing = true; // ✅ Enable GPU Instancing
            }
        }

        if (grassMesh == null || grassMaterial == null) return; // Ensure assets are assigned

        int grassDensity = Mathf.FloorToInt(Biome.grassScale * 55000); // Adjust density dynamically

        for (int i = 0; i < grassDensity; i++)
        {
            float worldX = transform.position.x + Random.Range(0, World.ChunkCellsWidth * 4f);
            float worldZ = transform.position.z + Random.Range(0, World.ChunkCellsHeight * 4f);
            float y = 0;// GetHeightInChunk((int)worldX, (int)worldZ); // Get terrain height

            Matrix4x4 matrix = Matrix4x4.TRS(
                new Vector3(worldX, y, worldZ), // Position
                Quaternion.Euler(0, Random.Range(0, 360), 0), // Random rotation
                Vector3.one * Random.Range(0.8f, 1.2f) // Random size variation
            );

            grassInstances.Add(matrix);
        }
    }

    /// <summary>
    /// Place generic objects based on position in the chunk. Used for objects that should not be 
    /// used excessively. 
    /// </summary>
    /// <param name="genericPrefabs"></param>
    /// <param name="scale"></param>
    private void PlaceGenericPrefabByPosition(List<GameObject> genericPrefabs, float scale)
    {
        for (int x = 0; x < World.ChunkCellsWidth; x += 2)
        {
            for (int z = 0; z < World.ChunkCellsHeight; z += 2)
            {
                // **Convert Local Chunk Position to World Position**
                int worldX = X * World.ChunkCellsWidth + x;
                int worldZ = Z * World.ChunkCellsHeight + z;

                // Get height from world position
                float y = GetHeightInChunk(x, z);

                // **Use World Position for Hashing (Prevents Single Chunk Issue)**
                float genericRandomValue = Hash(worldX, worldZ, (int)World.Seed);
                if (genericRandomValue > scale)
                    continue;

                // **Random Offsets for Natural Placement**
                float offsetX = (float)World.Rand.NextDouble() * World.CellSize - (World.CellSize * 0.5f);
                float offsetZ = (float)World.Rand.NextDouble() * World.CellSize - (World.CellSize * 0.5f);
                Vector3 genericPosition = new Vector3(worldX * World.CellSize + offsetX, y, worldZ * World.CellSize + offsetZ);

                // **Step 4: Seeded Selection of Tree Prefab**
                int genericIndex = World.Rand.Next(genericPrefabs.Count);
                GameObject genericPrefab = genericPrefabs[Mathf.Clamp(genericIndex, 0, genericPrefabs.Count - 1)];

                Instantiate(genericPrefab, genericPosition, Quaternion.Euler(0, World.Rand.Next(0, 360), 0), transform);
            }
        }
    }

    /// <summary>
    /// Place generic objects on vertexes in the chunk. This means 3x the amount of normal spawn
    /// will occur used for tiny detail items.
    /// </summary>
    /// <param name="genericPrefabs"></param>
    /// <param name="scale"></param>
    /// <param name="multiplier"></param>
    private void PlaceGenericPrefabByVertex(List<GameObject> genericPrefabs, float scale, float multiplier)
    {
        // Loop through each triangle in the mesh
        for (int i = 0; i < Terrain.Triangles.Length; i += 3)
        {
            if (Random.value > 0.4)
                continue;

            // Get the vertices of the triangle
            Vector3 vertexA = transform.TransformPoint(Terrain.Vertices[Terrain.Triangles[i]]);
            Vector3 vertexB = transform.TransformPoint(Terrain.Vertices[Terrain.Triangles[i + 1]]);
            Vector3 vertexC = transform.TransformPoint(Terrain.Vertices[Terrain.Triangles[i + 2]]);

            // Calculate the normal of the triangle
            Vector3 triangleNormal = Vector3.Cross(vertexB - vertexA, vertexC - vertexA).normalized;
            Vector3 triangleCenter = (vertexA + vertexB + vertexC) / 3f;

            // **Convert world position to terrain grid coordinates**
            int tx = Mathf.RoundToInt(triangleCenter.x / World.CellSize);
            int tz = Mathf.RoundToInt(triangleCenter.z / World.CellSize);

            // **Find biome at this triangle's location**
            float temperature = Mathf.PerlinNoise(tx * World.TemperatureNoiseScale, tz * World.TemperatureNoiseScale);
            float humidity = Mathf.PerlinNoise(tx * World.HumidityNoiseScale, tz * World.HumidityNoiseScale);
            float baseHeight = Mathf.PerlinNoise(tx * Biome.localNoiseScale, tz * Biome.localNoiseScale);

            // Randomly place grass within the triangle
            for (int j = 0; j < (scale * multiplier); j++)
            {
                // Offset the position slightly to avoid overlapping
                Vector3 position = RandomPointInTriangle(vertexA, vertexB, vertexC) + triangleNormal * 0.01f; // Lift slightly above surfacen = 

                GameObject genericInstance = Instantiate(
                    genericPrefabs[World.Rand.Next(0, genericPrefabs.Count)],
                    position,
                    Quaternion.Euler(0, (float)World.Rand.Next(0, 360), 0), // Randomize rotation
                    transform
                );

                // Randomize the scale for variety
                float randomScale = Random.Range(0.4f, 1.6f);
                genericInstance.transform.localScale = Vector3.one * randomScale;
            }
        }
    }

    /// <summary>
    /// Returns the neighbor grid position.
    /// </summary>
    /// <param name="localX"></param>
    /// <param name="localZ"></param>
    /// <returns></returns>
    private (Chunk, float) GetClosestNeighbor(int localX, int localZ)
    {
        int neighborGridX = X;
        int neighborGridZ = Z;

        int maxDistance = 5;
        float xDistance = 0;
        float zDistance = 0;

        // **X Movement (Up/Down in Grid)**
        // If we're near the top edge, move up in the grid (decreasing GridX)
        if (localX < 0)
            neighborGridX -= 1;

        // If we're near the bottom edge, move down in the grid (increasing GridX)
        if (localX >= World.ChunkCellsWidth)
            neighborGridX += 1;

        // **Z Movement (Left/Right in Grid)**
        // If we're near the left edge, move left in the grid (decreasing GridZ)
        if (localZ < 0)
            neighborGridZ -= 1;

        // If we're near the right edge, move right in the grid (increasing GridZ)
        if (localZ >= World.ChunkCellsHeight)
            neighborGridZ += 1;

        // Don't return our chunk.
        if (neighborGridX == X && neighborGridZ == Z)
            return (null,0f); // throw new System.ArgumentException("Failed to locate neighbor chunk.");

        return (World.GetChunk(neighborGridX, neighborGridZ), 3f);
    }

    /// <summary>
    /// Maps a local position in this chunk to the corresponding position in a neighboring chunk.
    /// </summary>
    /// <param name="b">The neighboring chunk</param>
    /// <param name="aX">Local X coordinate in this chunk</param>
    /// <param name="aZ">Local Z coordinate in this chunk</param>
    /// <returns>Mapped local position in chunk B</returns>
    private Vector3Int GetMappedLocalPosition(Chunk b, int aX, int aZ)
    {
        if (b == this)
            throw new System.ArgumentException("Tried to map local position to local chunk.");

        Vector3Int neighborDiff = new Vector3Int(Mathf.Clamp(b.X - X, -1, 1), 0, Mathf.Clamp(b.Z - Z, -1, 1));

        return new Vector3Int(
            (neighborDiff.x == -1) ? (World.ChunkCellsWidth - 1) : (neighborDiff.x == 1 ? 0 : aX),
            0,
            (neighborDiff.z == -1) ? (World.ChunkCellsHeight - 1) : (neighborDiff.z == 1 ? 0 : aZ)
        );
    }

    /// <summary>
    /// Retrieve a random position in the triangle.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <returns></returns>
    private Vector3 RandomPointInTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        float r1 = Mathf.Sqrt(Random.value);
        float r2 = Random.value;
        return (1 - r1) * a + (r1 * (1 - r2)) * b + (r1 * r2) * c;
    }

    /// <summary>
    /// Create a unique hash based on a seed to help with unique generation.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <param name="seed"></param>
    /// <returns></returns>
    private float Hash(int x, int z, int seed)
    {
        int hash = (x * 73856093) ^ (z * 19349663) ^ (seed * 83492791);
        hash = (hash << 13) ^ hash;
        return (hash & 0x7FFFFFFF) / (float)0x7FFFFFFF; // Normalize to 0-1
    }
}