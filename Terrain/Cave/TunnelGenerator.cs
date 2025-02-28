using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TunnelGenerator : MonoBehaviour
{
    [System.Serializable]
    public struct Tunnel
    {
        public float Radius;
        public int Count;
    }

    public int width = 32;
    public int height = 32;
    public int depth = 32;
    public float threshold = 0.5f;
    public Material caveMaterial;
    public List<Tunnel> tunnels = new List<Tunnel>();

    private MeshFilter meshFilter;
    private float[,,] densityMap;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        GetComponent<MeshRenderer>().material = caveMaterial; // Assign material
        this.Generate();
    }

    public void Generate()
    {
        GenerateDensityMap();
        meshFilter.mesh = MarchingCubes.GenerateMesh(densityMap, width, height, depth, threshold);
    }   
    private void GenerateDensityMap()
    {
        densityMap = new float[width + 1, height + 1, depth + 1];

        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y <= height; y++)
            {
                for (int z = 0; z <= depth; z++)
                {
                    densityMap[x, y, z] = 1f; // Start with solid walls everywhere
                }
            }
        }

        List<Vector3> lastTunnelNodes = null;
        for (int i = 0; i < tunnels.Count; i++)
        {
            Tunnel tunnel = tunnels[i];
            var tunnelNodes = GenerateNodes(tunnel.Count);

            // Add a random node from the previous collection to make sure they connect?
            if (i != 0)
            {
                tunnelNodes.Add(lastTunnelNodes.Random());
            }
            lastTunnelNodes = tunnelNodes;

            CarveTunnels(tunnelNodes, tunnel.Radius);
        }
    }
    private void CarveTunnels(List<Vector3> nodes, float tunnelRadius)
    {
        for (int i = 0; i < nodes.Count - 1; i++) // Loop through nodes sequentially
        {
            Vector3 start = nodes[i];
            Vector3 end = nodes[i + 1];

            Vector3 direction = (end - start).normalized;
            float distance = Vector3.Distance(start, end);

            for (float t = 0; t < distance; t += 0.5f)
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
            float x = Random.Range(width /4, width - (width / 4));
            float y = Random.Range(height /4, height - (height / 4));
            float z = Random.Range(depth /4, depth - (depth / 4));

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