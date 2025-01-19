using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.XR;
using Random = UnityEngine.Random;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PolygonTerrain : MonoBehaviour
{
    private Mesh mesh;
    private Vector2[] uvs;
    private Vector3[] vertices;
    private int[] triangles;
    private Color[] colors;
    private int width;
    private int height;
    private Biome Biome;
    private GameWorld World;

    public void Generate(GameWorld gameWorld, Biome biome)
    {
        Biome = biome;
        World = gameWorld;

        this.width = World.ChunkCellsWidth;
        this.height = World.ChunkCellsHeight;

        GenerateTerrain();
    }

    private void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
    }

    private void GenerateTerrain()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        // Calculate the number of vertices and triangles
        int vertexCount = (width + 1) * (height + 1);
        int triangleCount = width * height * 6;

        // Initialize arrays
        uvs = new Vector2[vertexCount];
        vertices = new Vector3[vertexCount];
        colors = new Color[vertexCount];
        triangles = new int[triangleCount];

        // Calculate vertices
        int sIndex = 0;
        for (int z = 0; z <= height; z++)
        {
            for (int x = 0; x <= width; x++)
            {
                vertices[sIndex] = new Vector3(x * World.CellSize, World.GetHeightInChunk(this.Biome, x, z), z * World.CellSize);
                colors[sIndex] = Biome.terrainColor;

                sIndex++;
            }
        }

        // Assign UVs (normalized to [0,1])
        for (int z = 0; z <= height; z++)
        {
            for (int x = 0; x <= width; x++)
            {
                int index = z * (width + 1) + x;
                uvs[index] = new Vector2((float)x / width, (float)z / height);
            }
        }

        // Generate triangles
        int triIndex = 0;
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int start = z * (width + 1) + x;

                // First triangle (top-left to bottom-right)
                triangles[triIndex++] = start;
                triangles[triIndex++] = start + width + 1;
                triangles[triIndex++] = start + 1;

                // Second triangle (bottom-left to top-right)
                triangles[triIndex++] = start + 1;
                triangles[triIndex++] = start + width + 1;
                triangles[triIndex++] = start + width + 2;
            }
        }

        UpdateMesh();
        GenerateGrass();
        PlaceTreePrefabs();
        PlaceRocks();
        PlaceFlowers();
    }


    private List<Matrix4x4> grassInstances = new List<Matrix4x4>();
    public Mesh grassMesh;
    public Material grassMaterial;

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
            float worldX = transform.position.x + Random.Range(0, width * 4f);
            float worldZ = transform.position.z + Random.Range(0, height * 4f);
            float y = World.GetHeightInChunk(Biome, (int)worldX, (int)worldZ); // Get terrain height

            Matrix4x4 matrix = Matrix4x4.TRS(
                new Vector3(worldX, y, worldZ), // Position
                Quaternion.Euler(0, Random.Range(0, 360), 0), // Random rotation
                Vector3.one * Random.Range(0.8f, 1.2f) // Random size variation
            );

            grassInstances.Add(matrix);
        }
    }


    private void Update()
    {
        if (grassMesh == null || grassMaterial == null || grassInstances.Count == 0)
        {
            return; // Prevent null errors
        }

        if (Vector3.Distance(Camera.main.transform.position, transform.position) < 60f)
        {
            Graphics.DrawMeshInstanced(grassMesh, 0, grassMaterial, grassInstances);
        }
    }


    private void PlaceTreePrefabs()
    {
        if (this.Biome.treeScale == 0)
            return;

        // Iterate through a grid (not every vertex)
        for (int x = 0; x < width; x += 2) // Adjust step size for better spread
        {
            for (int z = 0; z < height; z += 2)
            {
                float y = World.GetHeightInChunk(Biome, x, z);// GetTerrainHeight(x, z);

                // **Step 1: Generate a Hash-Based Random Value (Tied to Seed)**
                float treeRandomValue = Hash(x, z, (int)World.Seed);

                // **Step 2: Use treeScale to control density dynamically**
                if (treeRandomValue > Biome.treeScale)
                    continue; // Skip if below threshold

                // **Step 3: Apply Small Random Offsets for Natural Placement**
                float offsetX = (float)World.Rand.NextDouble() * World.CellSize - (World.CellSize * 0.5f);
                float offsetZ = (float)World.Rand.NextDouble() * World.CellSize - (World.CellSize * 0.5f);
                Vector3 treePosition = new Vector3(x * World.CellSize + offsetX, y, z * World.CellSize + offsetZ);

                // **Step 4: Seeded Selection of Tree Prefab**
                int treeIndex = World.Rand.Next(World.TreePrefabs.Count);
                GameObject treePrefab = World.TreePrefabs[Mathf.Clamp(treeIndex, 0, World.TreePrefabs.Count - 1)];

                Instantiate(treePrefab, treePosition, Quaternion.Euler(0, World.Rand.Next(0, 360), 0), transform);
            }
        }
    }


    private void PlaceGrassPatches()
    {
        if (this.Biome.grassScale == 0) return;

        // Loop through each triangle in the mesh
        for (int i = 0; i < triangles.Length; i += 3)
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
            float baseHeight = Mathf.PerlinNoise(tx * World.NoiseScale, tz * World.NoiseScale);

            // Randomly place grass within the triangle
            for (int j = 0; j < (Biome.grassScale * 10) * 2; j++)
            {
                // Generate a random point inside the triangle
                Vector3 randomPoint = RandomPointInTriangle(vertexA, vertexB, vertexC);

                // Offset the position slightly to avoid overlapping
                Vector3 position = randomPoint + triangleNormal * 0.01f; // Lift slightly above surface

                // Instantiate a grass prefab
                GameObject grassInstance = Instantiate(
                    World.GrassPrefabs[Random.Range(0, World.GrassPrefabs.Count)],
                    position,
                    Quaternion.Euler(0, Random.Range(0f, 360f), 0), // Randomize rotation
                    transform
                );

                // Randomize the scale for variety
                float randomScale = Random.Range(0.4f, 1.6f);
                grassInstance.transform.localScale = Vector3.one * randomScale;
            }
        }
    }

    private void PlaceRocks()
    {
        if (this.Biome.rockScale == 0)
            return;

        // Loop through each triangle in the mesh
        for (int i = 0; i < triangles.Length; i += 3)
        {
            if (Random.value > 0.2f)
                continue;

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
            float baseHeight = Mathf.PerlinNoise(tx * World.NoiseScale, tz * World.NoiseScale);

            // Randomly place grass within the triangle
            for (int j = 0; j < (Biome.rockScale * 10); j++)
            {
                if (Random.value > 0.4f)
                    continue;

                // Generate a random point inside the triangle
                Vector3 randomPoint = RandomPointInTriangle(vertexA, vertexB, vertexC);

                // Offset the position slightly to avoid overlapping
                Vector3 position = randomPoint + triangleNormal * 0.01f; // Lift slightly above surface

                // Instantiate a grass prefab
                GameObject grassInstance = Instantiate(
                    World.RockPrefabs[Random.Range(0, World.RockPrefabs.Count)],
                    position,
                    Quaternion.Euler(0, Random.Range(0f, 360f), 0), // Randomize rotation
                    transform
                );

                // Randomize the scale for variety
                float randomScale = Random.Range(0.4f, 1.6f);
                grassInstance.transform.localScale = Vector3.one * randomScale;
            }
        }
    }

    private void PlaceFlowers()
    {
        if (this.Biome.flowerScale == 0)
            return;

        // Loop through each triangle in the mesh
        for (int i = 0; i < triangles.Length; i += 3)
        {
            if (Random.value > 0.3f)
                continue;

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
            float baseHeight = Mathf.PerlinNoise(tx * World.NoiseScale, tz * World.NoiseScale);

            // Randomly place grass within the triangle
            for (int j = 0; j < (Biome.flowerScale * 10); j++)
            {
                if (Random.value > 0.3f)
                    continue;

                // Generate a random point inside the triangle
                Vector3 randomPoint = RandomPointInTriangle(vertexA, vertexB, vertexC);

                // Offset the position slightly to avoid overlapping
                Vector3 position = randomPoint + triangleNormal * 0.01f; // Lift slightly above surface

                // Instantiate a grass prefab
                GameObject grassInstance = Instantiate(
                    World.FlowerPrefabs[Random.Range(0, World.FlowerPrefabs.Count)],
                    position,
                    Quaternion.Euler(0, Random.Range(0f, 360f), 0), // Randomize rotation
                    transform
                );

                // Randomize the scale for variety
                float randomScale = Random.Range(0.4f, 1.6f);
                grassInstance.transform.localScale = Vector3.one * randomScale;
            }
        }
    }

    private Vector3 RandomPointInTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        float r1 = Mathf.Sqrt(Random.value);
        float r2 = Random.value;
        return (1 - r1) * a + (r1 * (1 - r2)) * b + (r1 * r2) * c;
    }

    private float Hash(int x, int z, int seed)
    {
        int hash = (x * 73856093) ^ (z * 19349663) ^ (seed * 83492791);
        hash = (hash << 13) ^ hash;
        return (hash & 0x7FFFFFFF) / (float)0x7FFFFFFF; // Normalize to 0-1
    }
}
