using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
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

    private float[,,] GenerateDensityMap(Vector3 chunkPos)
    {
        // We create an array of size [width+1, height+1, depth+1]
        // the extra is needed for fixing the seam on chunks.
        int globalWidth = (width * ChunkDimension) + 1;
        int globalHeight = (height * ChunkDimension) + 1;
        int globalDepth = (depth * ChunkDimension) + 1;

        float[,,] densityMap = new float[globalWidth, globalHeight, globalDepth];

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

        CarveDetails(densityMap);

        return densityMap;
    }

    private float[,,] GetDensityMapForChunk(float[,,] densityMap, Vector3Int chunkPos)
    {
        int startX = (width * chunkPos.x);
        int startY = (height * chunkPos.y);
        int startZ = (depth * chunkPos.z);

        float[,,] localDensityMap = new float[width + 1, height + 1, depth + 1];
        for (int x = 0; x < width + 1; x++)
        {
            for (int y = 0; y < height + 1; y++)
            {
                for (int z = 0; z < depth + 1; z++)
                {
                    localDensityMap[x, y, z] = densityMap[x + startX, y + startY, z + startZ];
                }
            }
        }

        return localDensityMap;
    }

    private void CarveDetails(float[,,] densityMap)
    {
        CarveTunnels(densityMap, GenerateNodes(2 * ChunkDimension), 6f);
        CarveTunnels(densityMap, GenerateNodes(4 * ChunkDimension), 4f);
        CarveTunnels(densityMap, GenerateNodes(4 * ChunkDimension), 2f);
    }

    private void CarveTunnels(float[,,] densityMap, List<Vector3> nodes, float tunnelRadius)
    {
        for (int i = 0; i < nodes.Count - 1; i++) // Loop through nodes sequentially
        {
            Vector3 start = nodes[i];
            Vector3 end = nodes[i + 1];

            Vector3 direction = (end - start).normalized;
            float distance = Vector3.Distance(start, end);

            for (float t = 0; t < distance; t += 0.1f)
            {
                Vector3 point = start + direction * t;

                // **Only remove tunnel areas, everything else stays solid**
                for (int x = -Mathf.CeilToInt(tunnelRadius); x <= Mathf.CeilToInt(tunnelRadius); x++)
                {
                    for (int y = -Mathf.CeilToInt(tunnelRadius); y <= Mathf.CeilToInt(tunnelRadius); y++)
                    {
                        for (int z = -Mathf.CeilToInt(tunnelRadius); z <= Mathf.CeilToInt(tunnelRadius); z++)
                        {
                            Vector3Int voxelPos = new Vector3Int(
                                Mathf.RoundToInt(point.x) + x,
                                Mathf.RoundToInt(point.y) + y,
                                Mathf.RoundToInt(point.z) + z
                            );

                            if (voxelPos.x >= 0 && voxelPos.x < width &&
                                voxelPos.y >= 0 && voxelPos.y < height &&
                                voxelPos.z >= 0 && voxelPos.z < depth &&
                                Vector3.Distance(voxelPos, point) <= tunnelRadius)
                            {
                                densityMap[voxelPos.x, voxelPos.y, voxelPos.z] = 0; // **Tunnels stay open**
                            }
                        }
                    }
                }
            }
        }
    }

    private List<Vector3> GenerateNodes(int numNodes, float segmentLength = 10f, float maxOffset = 5f)
    {
        List<Vector3> primaryNodes = new List<Vector3>();

        // **Step 1: Generate Primary Nodes**
        for (int i = 0; i < numNodes; i++)
        {
            float x = Random.Range(0, width);
            float y = Random.Range(0, height);
            float z = Random.Range(0,depth);

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
                point.x += Random.Range(-maxOffset, maxOffset);
                point.y += Random.Range(-maxOffset, maxOffset);
                point.z += Random.Range(-maxOffset, maxOffset);

                finalNodes.Add(point);
            }
        }

        // Add final primary node.
        finalNodes.Add(primaryNodes[primaryNodes.Count - 1]);

        return finalNodes;
    }
}