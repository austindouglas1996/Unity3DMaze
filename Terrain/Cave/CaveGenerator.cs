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
        // 1) Clear existing CaveChunk children
        foreach (Transform child in this.transform)
        {
            if (child.GetComponent<CaveChunk>() != null)
            {
                Destroy(child.gameObject);
            }
        }

        // 2) Instantiate chunks
        for (int x = 0; x < ChunkDimension; x++)
        {
            for (int y = 0; y < ChunkDimension; y++)
            {
                for (int z = 0; z < ChunkDimension; z++)
                {
                    // Instantiate chunk
                    CaveChunk ch = Instantiate(
                        ChunkPrefab,
                        new Vector3(x * width, y * height, z * depth),
                        Quaternion.identity,
                        this.transform
                    );
                    ch.ChunkPos = new Vector3Int(x, y, z);

                    // Generate density data
                    float[,,] density = GenerateDensityMap(ch.ChunkPos);

                    // Compute the chunk offset in world space:
                    // (This ensures each chunk’s mesh is positioned at the correct world coordinates)
                    Vector3 chunkOffset = new Vector3(0,0,0);

                    // Generate mesh and assign material
                    ch.GetComponent<MeshFilter>().mesh =
                        MarchingCubes.GenerateMesh(density, width, height, depth, threshold, chunkOffset);

                    ch.GetComponent<MeshRenderer>().material = caveMaterial;
                }
            }
        }
    }


    private float[,,] GenerateDensityMap(Vector3Int chunkPos)
    {
        // We create an array of size [width+1, height+1, depth+1]
        float[,,] densityMap = new float[width + 1, height + 1, depth + 1];

        // We sample the global coordinates from [chunkPos.x * width .. chunkPos.x * width + width], etc.
        // This ensures each chunk includes the boundary row/column shared with the next chunk.
        for (int x = 0; x < width + 1; x++)
        {
            for (int y = 0; y < height + 1; y++)
            {
                for (int z = 0; z < depth + 1; z++)
                {
                    // Convert local chunk sample index --> global sample index
                    int worldX = chunkPos.x * width + x;
                    int worldY = chunkPos.y * height + y;
                    int worldZ = chunkPos.z * depth + z;

                    // Sample your noise function (replace with your own noise / Fbm / domain transforms)
                    densityMap[x, y, z] = Perlin.Fbm(
                        (Offset.x + worldX) * frequency,
                        worldY * frequency,
                        (Offset.y + worldZ) * frequency,
                        octaves
                    );
                }
            }
        }

        return densityMap;
    }

}