using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Analytics;
using Random = UnityEngine.Random;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PolygonTerrain : MonoBehaviour
{
    [System.Serializable]
    public enum DrawMode
    {
        Mesh,
        HeightHeatMap
    }

    [Header("Actions")]
    public bool GenerateAgain = false;
    public DrawMode drawMode;
    public bool Submode = false;
    public bool BlendColors = true;
    public bool ShowTrees = true;
    public bool ShowFlowers = true;
    public bool ShowGrass = true;
    public bool ShowRocks = true;

    [Header("World")]
    public int seed = 1;
    private int lastSeed = 1;

    [Header("Cell Settings")]
    public int width = 10; // Number of quads along the X-axis
    public int height = 10; // Number of quads along the Z-axis
    public float cellSize = 1f; // Size of each quad

    [Header("Biome generation")]
    /// <summary>
    /// Controls the global height scale for initial generation.
    /// </summary>
    [Range(0f,1f)] public float globalNoiseScale = 0.3f;

    /// <summary>
    /// Temperature scale.
    /// </summary>
    [Range(0f, 1f)] public float tempNoiseScale = 0.3f;

    /// <summary>
    /// Humity scale.
    /// </summary>
    [Range(0f, 1f)] public float humNoiseScale = 0.3f;

    [Header("Water Settings")]
    public float waterLevel = -2f;
    public float riverThreshold = 0.3f; // Lower values create deeper rivers
    public float riverDepth = 4.0f;      // How deep the river should carve into the terrain
    public float riverNoiseScale = 0.1f; // Determines how "wiggly" the rivers are
    public float riverBankSlope = 4.0f; // Gradual slope into river
    public float maxRiverWidth = 8;

    [Header("Biomes")]
    public Biome[] biomes;

    [Header("Prefabs")]
    public List<GameObject> grassPrefabs;
    public List<GameObject> grass1Prefabs;
    public List<GameObject> flowerPrefabs;
    public List<GameObject> treePrefabs;
    public List<GameObject> rockPrefabs;

    private System.Random rand;

    private Mesh mesh;
    private Vector2[] uvs;
    private Vector3[] vertices;
    private int[] triangles;
    private Color[] colors;
    List<Vector2Int> riverPath = new List<Vector2Int>();

    private void Start()
    {
        rand = new System.Random((int)seed);
        GenerateTerrain();
    }

    private void FixedUpdate()
    {
        if (GenerateAgain || lastSeed != seed)
        {
            lastSeed = seed;
            GenerateAgain = false;

            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            GenerateTerrain();
        }
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

        if (drawMode == DrawMode.Mesh)
            CreateShape();
        else if (drawMode == DrawMode.HeightHeatMap)
            CreateHeightHeatMap();

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

        if (ShowTrees) PlaceTreePrefabs();
        if (ShowRocks) PlaceRocks();
        if (ShowGrass) PlaceGrassPatches();
        if (ShowFlowers) PlaceFlowers();
    }

    private void CreateHeightHeatMap()
    {
        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;
        float[,] heightMap = new float[width + 1, height + 1];

        for (int z = 0; z <= height; z++)
        {
            for (int x = 0; x <= width; x++)
            {
                float y = GetHeight(x, z);

                heightMap[x, z] = y;
                minHeight = Mathf.Min(minHeight, y);
                maxHeight = Mathf.Max(maxHeight, y);
            }
        }

        // Smooth the terrain to prevent sudden spikes
        for (int z = 1; z < height; z++)
        {
            for (int x = 1; x < width; x++)
            {
                float smoothedHeight = (heightMap[x - 1, z] +
                                        heightMap[x + 1, z] +
                                        heightMap[x, z - 1] +
                                        heightMap[x, z + 1] +
                                        heightMap[x, z]) / 5f;

                heightMap[x, z] = Mathf.Lerp(heightMap[x, z], smoothedHeight, 0.5f);
            }
        }

        // Generate flat vertices and apply color mapping
        for (int z = 0; z <= height; z++)
        {
            for (int x = 0; x <= width; x++)
            {
                int index = z * (width + 1) + x;

                // Flat plane with Y = 0
                vertices[index] = new Vector3(x * cellSize, Submode ? heightMap[x,z] : 0, z * cellSize);

                // Normalize height for color mapping
                float normalizedHeight = Mathf.InverseLerp(minHeight, maxHeight, heightMap[x, z]);
                colors[index] = Color.Lerp(Color.cyan, Color.red, normalizedHeight); // Gradient from light blue to red
            }
        }
    }


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

    private float GetTemperature(int x, int z)
    {
        //return Mathf.PerlinNoise(x * tempNoiseScale, z * tempNoiseScale);
        return Mathf.PerlinNoise((x + seed) * tempNoiseScale, (z + seed) * tempNoiseScale);
    }

    private float GetHumidity(int x, int z)
    {
        //return Mathf.PerlinNoise(x * humNoiseScale, z * humNoiseScale);
        return Mathf.PerlinNoise((x + seed) * humNoiseScale, (z + seed) * humNoiseScale);
    }

    private float ProcessRiverInfluence(int x, float y, int z)
    {
        // Determine river settings
        float distanceToRiver = DistanceToRiver(x, z, riverPath);
        float riverWidth = Mathf.Lerp(2, maxRiverWidth, distanceToRiver / maxRiverWidth);

        // **More aggressive slope influence**
        float slopeInfluence = riverWidth + riverBankSlope * 2; // Increase slope reach

        // **Apply a steep but gradual slope into the riverbed**
        if (distanceToRiver < slopeInfluence)
        {
            // Calculate how deep into the slope we are
            float slopeFactor = Mathf.InverseLerp(slopeInfluence, 0, distanceToRiver);

            // Use power to create a more aggressive slope
            slopeFactor = Mathf.Pow(slopeFactor, 1.5f);

            // **Blend from biome base height to riverbed aggressively**
            float targetHeight = Mathf.Lerp(GetBiome(y,x,z).baseHeight, waterLevel - riverDepth, slopeFactor);
            y = Mathf.Lerp(y, targetHeight, slopeFactor);
        }

        return y;
    }




    private void CreateShape()
    {
        // Generate the river path.
        //riverPath = GenerateRiverPath();

        // Calculate vertices
        int index = 0;
        for (int z = 0; z <= height; z++)
        {
            for (int x = 0; x <= width; x++)
            {
                float y = GetHeight(x, z);
                Biome currentBiome = GetBiome(y, x, z);
                //y = ProcessRiverInfluence(x, y, z);

                vertices[index] = new Vector3(x * cellSize, y, z * cellSize);

                // Color the triangles.
                if (BlendColors)
                    colors[index] = BlendBiomeColors(currentBiome, x, z);
                else
                    colors[index] = currentBiome.terrainColor;

                index++;
            }
        }
    }

    private Biome GetBiome(float height, int x, int z)
    {
        float temperature = GetTemperature(x, z);
        float humidity = GetHumidity(x, z);

        if (IsPointInRiver(x, z, riverPath))
        {
            return biomes.FirstOrDefault(r => r.name == "River");
        }

        Biome bestBiome = biomes[0]; // Default fallback
        float bestScore = float.MaxValue;

        foreach (Biome biome in biomes)
        {
            // **Compute how well this biome matches**
            float heightDiff = Mathf.Abs(height - Mathf.Lerp(biome.minHeight, biome.maxHeight, 0.5f));
            float tempDiff = Mathf.Abs(temperature - Mathf.Lerp(biome.minTemp, biome.maxTemp, 0.5f));
            float humidityDiff = Mathf.Abs(humidity - Mathf.Lerp(biome.minHumidity, biome.maxHumidity, 0.5f));

            float biomeScore = heightDiff + tempDiff + humidityDiff; // Lower score = better match

            if (biomeScore < bestScore)
            {
                bestScore = biomeScore;
                bestBiome = biome;
            }
        }

        return bestBiome;
    }

    private Color BlendBiomeColors(Biome currentBiome, int x, int z)
    {
        float height = GetHeight(x, z);
        float temperature = GetTemperature(x, z);
        float humidity = GetHumidity(x, z);

        // **1️⃣ If this point is a river, return the river biome color**
        if (IsPointInRiver(x, z, riverPath))
        {
            Biome riverBiome = biomes.FirstOrDefault(r => r.name == "River") ?? currentBiome;
            return riverBiome.terrainColor;
        }

        // **2️⃣ Blend colors with nearby biomes**
        Color blendedColor = Color.black;
        float weightSum = 0f;

        foreach (Biome biome in biomes)
        {
            // **Check if the biome is a "neighbor" (close in properties)**
            bool isCloseInHeight = height >= biome.minHeight - 1 && height <= biome.maxHeight + 1;
            bool isCloseInTemp = temperature >= biome.minTemp - 0.1f && temperature <= biome.maxTemp + 0.1f;
            bool isCloseInHumidity = humidity >= biome.minHumidity - 0.1f && humidity <= biome.maxHumidity + 0.1f;

            if (isCloseInHeight && isCloseInTemp && isCloseInHumidity)
            {
                // **Calculate weights based on how close this biome is**
                float heightWeight = 1f - Mathf.Abs(height - Mathf.Lerp(biome.minHeight, biome.maxHeight, 0.5f));
                float tempWeight = 1f - Mathf.Abs(temperature - Mathf.Lerp(biome.minTemp, biome.maxTemp, 0.5f));
                float humWeight = 1f - Mathf.Abs(humidity - Mathf.Lerp(biome.minHumidity, biome.maxHumidity, 0.5f));

                float biomeWeight = heightWeight * tempWeight * humWeight;

                // **Blend biome colors based on weight**
                blendedColor += biome.terrainColor * biomeWeight;
                weightSum += biomeWeight;
            }
        }

        // **3️⃣ Normalize the blended color or fallback to the current biome color**
        return weightSum > 0 ? blendedColor / weightSum : currentBiome.terrainColor;
    }



    List<Vector2Int> GenerateRiverPath()
    {
        List<Vector2Int> riverPath = new List<Vector2Int>();

        // **Step 1: Choose a Starting Point (Top or Side)**
        int startX = Mathf.FloorToInt(Mathf.PerlinNoise(seed * 0.1f, 0) * width);
        int startZ = height; // Start from top edge

        Vector2Int riverPos = new Vector2Int(startX, startZ);
        riverPath.Add(riverPos);

        // **Step 2: Random Walk Downward**
        for (int i = 0; i < height; i++)
        {
            float directionNoise = Mathf.PerlinNoise((seed + i) * 0.1f, riverPos.y * 0.1f);
            int direction = (directionNoise < 0.33f) ? -1 : (directionNoise > 0.66f) ? 1 : 0;

            riverPos += Vector2Int.down;
            riverPos.x = Mathf.Clamp(riverPos.x + direction, 0, width - 1);

            riverPath.Add(riverPos);
        }

        return riverPath;
    }

    float DistanceToRiver(int x, int z, List<Vector2Int> riverPath)
    {
        float minDistance = float.MaxValue;

        foreach (Vector2Int riverPoint in riverPath)
        {
            float distance = Vector2Int.Distance(new Vector2Int(x, z), riverPoint);
            if (distance < minDistance)
            {
                minDistance = distance;
            }
        }

        return minDistance;
    }

    bool IsPointInRiver(int x, int z, List<Vector2Int> riverPath)
    {
        float distanceToRiver = DistanceToRiver(x, z, riverPath);

        // **Calculate dynamic river width based on depth**
        float riverWidth = Mathf.Lerp(2, maxRiverWidth, distanceToRiver / maxRiverWidth);

        // **Check if this point is inside the calculated river width**
        return distanceToRiver < riverWidth;
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








    private void PlaceTreePrefabs()
    {
        if (!ShowTrees)
            return;

        // Iterate through a grid (not every vertex)
        for (int x = 0; x < width; x += 2) // Adjust step size for better spread
        {
            for (int z = 0; z < height; z += 2)
            {
                float y = GetHeight(x, z);// GetTerrainHeight(x, z);

                float temperature = GetTemperature(x, z);
                float humidity = GetHumidity(x, z);

                Biome currentBiome = GetBiome(y, x, z);
                if (currentBiome.treeScale <= 0f)
                    continue; // Skip if biome has no trees

                // **Step 1: Generate a Hash-Based Random Value (Tied to Seed)**
                float treeRandomValue = Hash(x, z, (int)seed);

                // **Step 2: Use treeScale to control density dynamically**
                if (treeRandomValue > currentBiome.treeScale)
                    continue; // Skip if below threshold

                // **Step 3: Apply Small Random Offsets for Natural Placement**
                float offsetX = (float)rand.NextDouble() * cellSize - (cellSize * 0.5f);
                float offsetZ = (float)rand.NextDouble() * cellSize - (cellSize * 0.5f);
                Vector3 treePosition = new Vector3(x * cellSize + offsetX, y, z * cellSize + offsetZ);

                // **Step 4: Seeded Selection of Tree Prefab**
                int treeIndex = rand.Next(treePrefabs.Count);
                GameObject treePrefab = treePrefabs[Mathf.Clamp(treeIndex, 0, treePrefabs.Count - 1)];

                Instantiate(treePrefab, treePosition, Quaternion.Euler(0, rand.Next(0, 360), 0), transform);
            }
        }
    }

    private void PlaceGrassPatches()
    {
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
            int tx = Mathf.RoundToInt(triangleCenter.x / cellSize);
            int tz = Mathf.RoundToInt(triangleCenter.z / cellSize);

            // **Find biome at this triangle's location**
            float temperature = Mathf.PerlinNoise(tx * tempNoiseScale, tz * tempNoiseScale);
            float humidity = Mathf.PerlinNoise(tx * humNoiseScale, tz * humNoiseScale);
            float baseHeight = Mathf.PerlinNoise(tx * globalNoiseScale, tz * globalNoiseScale);

            Biome triangleBiome = GetBiome(baseHeight, tx, tz);
            if (triangleBiome.grassScale == 0)
                continue;

            // Randomly place grass within the triangle
            for (int j = 0; j < (triangleBiome.grassScale * 10) * 2; j++)
            {
                // Generate a random point inside the triangle
                Vector3 randomPoint = RandomPointInTriangle(vertexA, vertexB, vertexC);

                // Offset the position slightly to avoid overlapping
                Vector3 position = randomPoint + triangleNormal * 0.01f; // Lift slightly above surface

                // Instantiate a grass prefab
                GameObject grassInstance = Instantiate(
                    grass1Prefabs[Random.Range(0, grass1Prefabs.Count)],
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
            int tx = Mathf.RoundToInt(triangleCenter.x / cellSize);
            int tz = Mathf.RoundToInt(triangleCenter.z / cellSize);

            // **Find biome at this triangle's location**
            float temperature = Mathf.PerlinNoise(tx * tempNoiseScale, tz * tempNoiseScale);
            float humidity = Mathf.PerlinNoise(tx * humNoiseScale, tz * humNoiseScale);
            float baseHeight = Mathf.PerlinNoise(tx * globalNoiseScale, tz * globalNoiseScale);

            Biome triangleBiome = GetBiome(baseHeight, tx, tz);
            if (triangleBiome.rockScale == 0)
                continue;

            // Randomly place grass within the triangle
            for (int j = 0; j < (triangleBiome.rockScale * 10); j++)
            {
                if (Random.value > 0.4f)
                    continue;

                // Generate a random point inside the triangle
                Vector3 randomPoint = RandomPointInTriangle(vertexA, vertexB, vertexC);

                // Offset the position slightly to avoid overlapping
                Vector3 position = randomPoint + triangleNormal * 0.01f; // Lift slightly above surface

                // Instantiate a grass prefab
                GameObject grassInstance = Instantiate(
                    rockPrefabs[Random.Range(0, rockPrefabs.Count)],
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
            int tx = Mathf.RoundToInt(triangleCenter.x / cellSize);
            int tz = Mathf.RoundToInt(triangleCenter.z / cellSize);

            // **Find biome at this triangle's location**
            float temperature = Mathf.PerlinNoise(tx * tempNoiseScale, tz * tempNoiseScale);
            float humidity = Mathf.PerlinNoise(tx * humNoiseScale, tz * humNoiseScale);
            float baseHeight = Mathf.PerlinNoise(tx * globalNoiseScale, tz * globalNoiseScale);

            Biome triangleBiome = GetBiome(baseHeight, tx, tz);
            if (triangleBiome.flowerScale == 0)
                continue;

            // Randomly place grass within the triangle
            for (int j = 0; j < (triangleBiome.flowerScale * 10); j++)
            {
                if (Random.value > 0.3f)
                    continue;

                // Generate a random point inside the triangle
                Vector3 randomPoint = RandomPointInTriangle(vertexA, vertexB, vertexC);

                // Offset the position slightly to avoid overlapping
                Vector3 position = randomPoint + triangleNormal * 0.01f; // Lift slightly above surface

                // Instantiate a grass prefab
                GameObject grassInstance = Instantiate(
                    flowerPrefabs[Random.Range(0, flowerPrefabs.Count)],
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
}
