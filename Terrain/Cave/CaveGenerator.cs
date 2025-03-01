using System.Collections.Generic;
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
                    int worldX = chunkPos.x * width + x;
                    int worldY = chunkPos.y * height + y;
                    int worldZ = chunkPos.z * depth + z;

                    densityMap[x, y, z] = Perlin.Fbm((Offset.x + worldX) * frequency,worldY * frequency,(Offset.y + worldZ) * frequency,octaves);
                }
            }
        }

        return densityMap;
    }
}