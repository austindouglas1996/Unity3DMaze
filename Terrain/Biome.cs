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

    /*
    private List<Matrix4x4> grassInstances = new List<Matrix4x4>();
    private Mesh grassMesh;
    private Material grassMaterial;

    /// <summary>
    /// Generate Mesh grass? Idk, ChatGBT made this as I was struggling way too much with how
    /// to render objects of this much value.
    /// </summary>
    public void GenerateGrass(GameWorld World, PolygonTerrain Terrain)
    {
        if (grassScale == 0f) return;

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

        // Loop through each triangle in the mesh
        for (int i = 0; i < Terrain.Triangles.Length; i += 3)
        {
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
            float baseHeight = Mathf.PerlinNoise(tx * localNoiseScale, tz * localNoiseScale);

            // Randomly place grass within the triangle
            for (int j = 0; j < 12; j++)
            {
                // Offset the position slightly to avoid overlapping
                Vector3 position = RandomPointInTriangle(vertexA, vertexB, vertexC) + triangleNormal * 0.01f; // Lift slightly above surfacen = 
                Matrix4x4 matrix = Matrix4x4.TRS(position, Quaternion.Euler(0, Random.Range(0, 360), 0), Vector3.one * Random.Range(1f, 2f));
                grassInstances.Add(matrix);
            }
        }
    }

    /// <summary>
    /// Place generic objects based on position in the chunk. Used for objects that should not be 
    /// used excessively. 
    /// </summary>
    /// <param name="genericPrefabs"></param>
    /// <param name="scale"></param>
    private void PlaceGenericPrefabByPosition(List<GameObject> genericPrefabs, float scale, GameWorld World, PolygonTerrain Terrain)
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
    */

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
