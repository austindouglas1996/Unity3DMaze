using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PolygonTerrain))]
public class Chunk : MonoBehaviour
{
    public int GridX = 0;
    public int GridZ = 0;

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
        if (Vector3.Distance(Camera.main.transform.position, transform.position) < 60f)
        {
            Graphics.DrawMeshInstanced(grassMesh, 0, grassMaterial, grassInstances);
        }
    }

    /// <summary>
    /// Returns whether the position is within an edge range.
    /// </summary>
    /// <param name="localX"></param>
    /// <param name="localZ"></param>
    /// <returns></returns>
    public bool IsEdge(int localX, int localZ)
    {
        return (localX < 0 ||
                localX >= World.ChunkCellsWidth||
                localZ < 0 ||
                localZ >= World.ChunkCellsHeight);
    }

    /// <summary>
    /// Get the height of a specific point in a chunk.
    /// </summary>
    /// <param name="biome"></param>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public float GetHeightInChunk(int localX, int localZ, Chunk root = null)
    {
        Vector3 worldPos = World.GridToWorldPosition(GridX, GridZ, localX, localZ);
        float currentHeight = World.GetVertexHeight(Biome, worldPos.x, worldPos.z);

        // We do not want to cause a stack overflow.
        if (root == null)
        {
            Vector3Int neighborGridPos = GetNeighborChunkGridPosition(localX, localZ);
            if (neighborGridPos != new Vector3(-1,-1,-1))
            {
                Chunk neighborChunk = World.GetChunk(neighborGridPos.x, neighborGridPos.z);
                if (neighborChunk != null)
                {
                    Vector3Int neighborPos = GetLocalPositionInNeighbor(neighborChunk, localX, localZ);
                    float neighborHeight = neighborChunk.GetHeightInChunk(neighborPos.x, neighborPos.z, this);

                    // Convert both local and neighbor positions into world coordinates
                    Vector3 worldCurrentPos = World.GridToWorldPosition(GridX, GridZ, localX, localZ);
                    Vector3 worldNeighborPos = World.GridToWorldPosition(neighborGridPos.x, neighborGridPos.z, neighborPos.x, neighborPos.z);

                    return Mathf.Lerp(currentHeight, neighborHeight, 20.5f);
                }
            }
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
        PlaceGenericPrefabByVertex(World.FlowerPrefabs, Biome.flowerScale, 10f);
        PlaceGenericPrefabByVertex(World.RockPrefabs, Biome.flowerScale, 10f);
        PlaceGenericPrefabByVertex(World.GrassPrefabs, Biome.flowerScale, 3f);
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
                float y = 0;// GetHeightInChunk(x, z);

                float genericRandomValue = Hash(x, z, (int)World.Seed);
                if (genericRandomValue > scale)
                    continue;

                float offsetX = (float)World.Rand.NextDouble() * World.CellSize - (World.CellSize * 0.5f);
                float offsetZ = (float)World.Rand.NextDouble() * World.CellSize - (World.CellSize * 0.5f);
                Vector3 genericPosition = new Vector3(x * World.CellSize + offsetX, y, z * World.CellSize + offsetZ);

                // **Step 4: Seeded Selection of Tree Prefab**
                int treeIndex = World.Rand.Next(World.TreePrefabs.Count);
                GameObject genericPrefab = genericPrefabs[Mathf.Clamp(treeIndex, 0, genericPrefabs.Count - 1)];

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
        Vector3[] vertices = Terrain.Vertices;
        int[] triangles = Terrain.Triangles;

        // Loop through each triangle in the mesh
        for (int i = 0; i < Terrain.Vertices.Length; i += 3)
        {
            // Get the vertices of the triangle
            Vector3 vertexA = transform.TransformPoint(vertices[triangles[i]]);
            Vector3 vertexB = transform.TransformPoint(vertices[triangles[i + 1]]);
            Vector3 vertexC = transform.TransformPoint(vertices[triangles[i + 2]]);

            // Calculate the normal of the triangle
            Vector3 triangleNormal = Vector3.Cross(vertexB - vertexA, vertexC - vertexA).normalized;
            Vector3 triangleCenter = (vertexA + vertexB + vertexC) / 3f;

            // **Convert world position to terrain grid coordinates**
            int tx = Mathf.RoundToInt(triangleCenter.x / World.CellSize);
            int tz = Mathf.RoundToInt(triangleCenter.z / World.CellSize);

            // **Find biome at this triangle's location**
            float temperature = Mathf.PerlinNoise(tx * World.TemperatureNoiseScale, tz * World.TemperatureNoiseScale);
            float humidity = Mathf.PerlinNoise(tx * World.HumidityNoiseScale, tz * World.HumidityNoiseScale);
            float baseHeight = Mathf.PerlinNoise(tx * Biome.noiseScale, tz * Biome.noiseScale);

            // Randomly place grass within the triangle
            for (int j = 0; j < (scale * multiplier) * 2; j++)
            {
                // Offset the position slightly to avoid overlapping
                Vector3 position = RandomPointInTriangle(vertexA, vertexB, vertexC) + triangleNormal * 0.01f; // Lift slightly above surface

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
    private Vector3Int GetNeighborChunkGridPosition(int localX, int localZ)
    {
        if (!IsEdge(localX, localZ))
            return new Vector3Int(-1, -1, -1); // Not an edge, no neighbor needed

        int neighborGridX = GridX;
        int neighborGridZ = GridZ;

        // **X Movement (Up/Down in Grid)**
        // If we're near the top edge, move up in the grid (decreasing GridX)
        if (localX < 0)
            neighborGridX -= 1;

        // If we're near the bottom edge, move down in the grid (increasing GridX)
        if (localX >= World.ChunkCellsWidth - 0)
            neighborGridX += 1;

        // **Z Movement (Left/Right in Grid)**
        // If we're near the left edge, move left in the grid (decreasing GridZ)
        if (localZ < 0)
            neighborGridZ -= 1;

        // If we're near the right edge, move right in the grid (increasing GridZ)
        if (localZ >= World.ChunkCellsHeight - 0)
            neighborGridZ += 1;

        return new Vector3Int(neighborGridX, 0, neighborGridZ);
    }



    /// <summary>
    /// Retrieve the neighor position of an edge.
    /// </summary>
    /// <param name="localX"></param>
    /// <param name="localZ"></param>
    /// <returns></returns>
    private Vector3Int GetLocalPositionInNeighbor(Chunk neighbor, int localX, int localZ)
    {
        var neighborPos = GetChunkOffset(GetNeighborChunkGridPosition(localX, localZ), new Vector3Int(GridX, 0, GridZ));
        
        int x = localX;
        int z = localZ;

        switch(neighborPos.x)
        {
            case -1:
                x = World.ChunkCellsHeight;
                break;
            case 1:
                x = 0;
                break;
        }

        switch(neighborPos.z)
        {
            case -1:
                z = World.ChunkCellsWidth;
                break;
            case 1:
                z = 0;
                break;
        }

        return new Vector3Int(x,0,z);
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

    public static Vector3Int GetChunkOffset(Vector3Int targetChunk, Vector3Int referenceChunk)
    {
        return new Vector3Int(
            targetChunk.x - referenceChunk.x,
            targetChunk.y - referenceChunk.y,
            targetChunk.z - referenceChunk.z
        );
    }
}