using System.Collections.Generic;
using System.Diagnostics;
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

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    densityMap[x, y, z] = 1;
                }
            }
        }

        CarveDetails(densityMap, chunkPos);

        return densityMap;
    }

    private void CarveDetails(float[,,] densityMap, Vector3Int chunkPos)
    {
        System.Random rand = new System.Random(42 ^ chunkPos.GetHashCode());

        CarveTunnels(densityMap, rand, GenerateNodes(rand,2 * ChunkDimension), chunkPos, 6f);
        CarveTunnels(densityMap, rand, GenerateNodes(rand,4 * ChunkDimension), chunkPos, 4f);
        CarveTunnels(densityMap, rand, GenerateNodes(rand,4 * ChunkDimension), chunkPos, 2f);
    }

    private void CarveTunnels(float[,,] densityMap, System.Random rand, List<Vector3> nodes, Vector3Int chunkPos, float tunnelRadius)
    {
        foreach (Vector3 node in nodes)
        {
            Vector3 worldNode = node + (chunkPos * width); // Convert local node to world space

            for (float t = 0; t < 1; t += 0.1f) // Step along tunnel
            {
                Vector3 point = worldNode; // In an infinite world, we carve based on world space

                int maxRadius = Mathf.CeilToInt(tunnelRadius);
                for (int x = -maxRadius; x <= maxRadius; x++)
                {
                    for (int y = -maxRadius; y <= maxRadius; y++)
                    {
                        for (int z = -maxRadius; z <= maxRadius; z++)
                        {
                            Vector3Int voxelPos = new Vector3Int(
                                Mathf.RoundToInt(point.x) + x - chunkPos.x * width,
                                Mathf.RoundToInt(point.y) + y - chunkPos.y * height,
                                Mathf.RoundToInt(point.z) + z - chunkPos.z * depth
                            );

                            if (voxelPos.x >= 0 && voxelPos.x < width &&
                                voxelPos.y >= 0 && voxelPos.y < height &&
                                voxelPos.z >= 0 && voxelPos.z < depth)
                            {
                                float dist = Vector3.Distance(voxelPos + chunkPos * width, point);
                                if (dist <= tunnelRadius)
                                {
                                    densityMap[voxelPos.x, voxelPos.y, voxelPos.z] = 0;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private List<Vector3> GenerateNodes(System.Random rand, int numNodes, float segmentLength = 10f, float maxOffset = 5f)
    {
        List<Vector3> primaryNodes = new List<Vector3>();

        // **Step 1: Generate Primary Nodes**
        for (int i = 0; i < numNodes; i++)
        {
            float x = (float)rand.NextDouble() * width;
            float y = (float)rand.NextDouble() * height;
            float z = (float)rand.NextDouble() * depth;

            Vector3 node = new Vector3(x, y, z);
            primaryNodes.Add(node);
        }

        List<Vector3> finalNodes = new List<Vector3>();
        for (int i = 0; i < primaryNodes.Count - 1; i++)
        {
            Vector3 start = primaryNodes[i];
            Vector3 end = primaryNodes[i + 1];

            Vector3 direction = (end - start).normalized;
            float distance = Vector3.Distance(start, end);

            // **Break path into segments**
            for (float t = 0; t < distance; t += segmentLength)
            {
                Vector3 point = start + direction * t;

                // **Add random offsets to create turns**
                point.x += (float)(rand.NextDouble() * 2 - 1) * maxOffset;
                point.y += (float)(rand.NextDouble() * 2 - 1) * maxOffset;
                point.z += (float)(rand.NextDouble() * 2 - 1) * maxOffset;


                finalNodes.Add(point);
            }
        }

        // Add final primary node.
        finalNodes.Add(primaryNodes[primaryNodes.Count - 1]);

        return finalNodes;
    }
}