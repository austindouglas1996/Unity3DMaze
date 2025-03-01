using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CaveGenerator : MonoBehaviour
{
    [Header("Chunk")]
    public int width = 32;
    public int height = 32;
    public int depth = 32;

    [Header("Noise")]
    public int Seed = 42;
    public float threshold = 0.5f;
    public float frequency = 0.05f;
    public int octaves = 12;
    public float verticalScale = 0.1f;
    public float horizontalScale = 0.1f;
    public float depthScale = 0.1f;
    public Vector2 Offset = new Vector2(0, 0);

    [Header("Rendering")]
    public Material caveMaterial;
    public CaveChunk ChunkPrefab;
    public int ChunkDimension = 2;

    private System.Random rand;
    private MeshFilter meshFilter;
    private List<GameObject> Trash = new List<GameObject>();

    void Start()
    {
        this.Generate();
    }

    public void Generate()
    {
        // Clear existing CaveChunk children
        foreach (Transform child in this.transform)
        {
            if (child.GetComponent<CaveChunk>() != null)
            {
                Destroy(child.gameObject);
            }
        }

        foreach (var item in this.Trash)
        {
            Destroy(item);
        }

        for (int x = 0; x < ChunkDimension; x++)
        {
            for (int y = 0; y < ChunkDimension; y++)
            {
                for (int z = 0; z < ChunkDimension; z++)
                {
                    // Instantiate chunk
                    CaveChunk ch = Instantiate(ChunkPrefab,new Vector3(x * width, y * height, z * depth),Quaternion.identity,this.transform);
                    ch.ChunkPos = new Vector3Int(x, y, z);

                    float[,,] densityMap = GenerateDensityMap(ch.ChunkPos);

                    ch.GetComponent<MeshFilter>().mesh =
                        MarchingCubes.GenerateMesh(densityMap, width, height, depth, threshold, new Vector3(0,0,0));
                    ch.GetComponent<MeshRenderer>().material = caveMaterial;
                }
            }
        }
    }

    private float[,,] GenerateDensityMap(Vector3Int chunkPos)
    {
        float[,,] densityMap = new float[width + 1, height + 1, depth +1];

        for (int x = 0; x < width + 1; x++)
        {
            for (int y = 0; y < height + 1; y++)
            {
                for (int z = 0; z < depth + 1; z++)
                {
                    densityMap[x, y, z] = 1;
                }
            }
        }

        DrawNodes(densityMap, chunkPos);

        return densityMap;
    }

    private float[,,] GetNoiseMap(float[,,] densityMap, Vector3Int chunkPos)
    {
        float minNoise = float.MaxValue;
        float maxNoise = float.MinValue;
        float[,,] noiseMap = new float[width + 1, height + 1, depth + 1];

        for (int x = 0; x < width + 1; x++)
        {
            for (int y = 0; y < height + 1; y++)
            {
                for (int z = 0; z < depth + 1; z++)
                {
                    // Convert local chunk coordinates to world coordinates
                    int worldX = chunkPos.x * width + x;
                    int worldY = chunkPos.y * height + y;
                    int worldZ = chunkPos.z * depth + z;

                    // Sample 3D Perlin noise at the world coordinates
                    float noiseValue = Perlin.Fbm(worldX * 0.1f, worldY * 0.1f, worldZ * 0.1f, 4);

                    // Track min and max noise values
                    if (noiseValue < minNoise) minNoise = noiseValue;
                    if (noiseValue > maxNoise) maxNoise = noiseValue;

                    // If the noise value is above the threshold, add the point to the list
                    Vector3Int pos = new Vector3Int(x, y, z);
                    noiseMap[x, y, z] = noiseValue;
                }
            }
        }

        for (int x = 0; x < width + 1; x++)
        {
            for (int y = 0; y < height + 1; y++)
            {
                for (int z = 0; z < depth + 1; z++)
                {
                    noiseMap[x, y, z] = (noiseMap[x, y, z] - minNoise) / (maxNoise - minNoise);
                }
            }
        }

        return noiseMap;
    }

    private List<Vector3Int> GetNodesAboveThreshold(float threshold, Vector3Int chunkPos)
    {
        List<Vector3Int> pointsAboveThreshold = new List<Vector3Int>();

        // Add forced border nodes for seamless chunk connections
        List<Vector3Int> borderNodes = new List<Vector3Int>();

        // -X and +X faces
        //borderNodes.Add(new Vector3Int(0, height / 2, depth / 2));         // -X edge
        borderNodes.Add(new Vector3Int(width - 1, height / 2, depth / 2)); // +X edge

        // -Y and +Y faces
        //borderNodes.Add(new Vector3Int(width / 2, 0, depth / 2));         // -Y edge
        //borderNodes.Add(new Vector3Int(width / 2, height - 1, depth / 2)); // +Y edge

        // -Z and +Z faces
        borderNodes.Add(new Vector3Int(width / 2, height / 2, 0));         // -Z edge
        borderNodes.Add(new Vector3Int(width / 2, height / 2, depth - 1)); // +Z edge

        // Merge border nodes into main list (avoiding duplicates)
        foreach (Vector3Int node in borderNodes)
        {
            if (!pointsAboveThreshold.Contains(node))
                pointsAboveThreshold.Add(node);
        }

        return pointsAboveThreshold;
    }

    private void DrawNodes(float[,,] densityMap, Vector3Int chunkPos)
    {
        var roots = GetNodesAboveThreshold(0.90f, chunkPos);

        var rootHeart = roots.Random();
        Vector3Int rootHeartPos = new Vector3Int(
            rootHeart.x + chunkPos.x * width,
            rootHeart.y + chunkPos.y * height,
            rootHeart.z + chunkPos.z * depth);

        foreach (var rootItem in roots)
        {
            bool isHeart = rootItem == rootHeart;

            Vector3Int worldPos = new Vector3Int(
                rootItem.x + chunkPos.x * width,
                rootItem.y + chunkPos.y * height,
                rootItem.z + chunkPos.z * depth
            );

            DrawCube(worldPos, Vector3.one * 1f, isHeart ? Color.black : Color.red, "Root");

            // Don't connect to ourselve.
            if (isHeart)
                continue;

            // Find a way home.
            CarveTunnelBetween(densityMap, worldPos, rootHeartPos, 4, chunkPos,32);
        }
    }

    private void DrawCube(Vector3 pos, Vector3 size, Color color, string name)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = pos;
        cube.transform.localScale = size;
        cube.name = name;

        Renderer cubeRenderer = cube.GetComponent<Renderer>();
        if (cubeRenderer != null)
        {
            cubeRenderer.material.color = color;
        }

        this.Trash.Add(cube);
    }

    public float stepSize = 4f;
    private void CarveTunnelBetween(
        float[,,] densityMap,
        Vector3 worldStart,
        Vector3 worldEnd,
        int tunnelRadius,
        Vector3Int chunkPos,
        int chunkSize
    )
    {
        Vector3 direction = (worldEnd - worldStart).normalized;
        float distance = Vector3.Distance(worldStart, worldEnd);

        float stepSize = 0.1f; // Smaller steps for smoother results

        // 1) Main carving loop
        for (float d = 0f; d <= distance; d += stepSize)
        {
            Vector3 worldPoint = worldStart + direction * d;

            // 2) Define the tunnel's influence on the density field
            for (int x = -tunnelRadius; x <= tunnelRadius; x++)
            {
                for (int y = -tunnelRadius; y <= tunnelRadius; y++)
                {
                    for (int z = -tunnelRadius; z <= tunnelRadius; z++)
                    {
                        Vector3Int voxelPos = new Vector3Int(
                            Mathf.RoundToInt(worldPoint.x) + x - chunkPos.x * chunkSize,
                            Mathf.RoundToInt(worldPoint.y) + y - chunkPos.y * chunkSize,
                            Mathf.RoundToInt(worldPoint.z) + z - chunkPos.z * chunkSize
                        );

                        // Skip out-of-bounds voxels
                        if (voxelPos.x < 0 || voxelPos.x >= densityMap.GetLength(0) ||
                            voxelPos.y < 0 || voxelPos.y >= densityMap.GetLength(1) ||
                            voxelPos.z < 0 || voxelPos.z >= densityMap.GetLength(2))
                        {
                            continue;
                        }

                        // Compute distance from the tunnel center
                        float distToCenter = Vector3.Distance(voxelPos + chunkPos * chunkSize, worldPoint);

                        // 3) Smoothly blend the tunnel's influence into the density field
                        if (distToCenter < tunnelRadius)
                        {
                            // Use a smooth falloff function (e.g., cosine or polynomial)
                            float falloff = Mathf.SmoothStep(1f, 0f, distToCenter / tunnelRadius);
                            densityMap[voxelPos.x, voxelPos.y, voxelPos.z] -= falloff;
                        }
                    }
                }
            }
        }
    }




}