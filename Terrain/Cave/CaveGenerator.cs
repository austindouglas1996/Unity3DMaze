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
    public int width = 32;
    public int height = 32;
    public int depth = 32;
    public float threshold = 0.5f;
    public float frequency = 0.05f;
    public int octaves = 12;
    public Material caveMaterial;

    public float verticalScale = 0.1f;
    public float horizontalScale = 0.1f;
    public float depthScale = 0.1f;

    public CaveChunk ChunkPrefab;
    public int ChunkDimension = 2;
    public Vector2 Offset = new Vector2(0, 0);

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

                    // Generate density data
                    float[,,] density = GenerateDensityMap(ch.ChunkPos);

                    ch.GetComponent<MeshFilter>().mesh =
                        MarchingCubes.GenerateMesh(density, width, height, depth, threshold, new Vector3(0, 0, 0));
                    ch.GetComponent<MeshRenderer>().material = caveMaterial;
                }
            }
        }
    }

    private float[,,] GenerateDensityMap(Vector3Int chunkPos)
    {
        // We create an array of size [width+1, height+1, depth+1]
        // the extra is needed for fixing the seam on chunks.
        float[,,] densityMap = new float[width + 1, height + 1, depth + 1];

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

        CarveTunnels(densityMap, GenerateNodes(2), 6f);
        CarveTunnels(densityMap, GenerateNodes(4), 4f);
        CarveTunnels(densityMap, GenerateNodes(4), 2f);

        return densityMap;
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

        // **Step 2: Generate Path Nodes (Curved/Segmented Paths)**
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

        // Add final primary node (last one)
        finalNodes.Add(primaryNodes[primaryNodes.Count - 1]);

        return finalNodes;
    }
}