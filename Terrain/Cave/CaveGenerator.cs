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
    public float threshold = 0.5f;
    public int octaves = 12;
    public float noise = 0.1f;
    public float floorHeight = 0.2f;

    [Header("Rendering")]
    public Material caveMaterial;
    public CaveChunk ChunkPrefab;
    public int ChunkDimension = 2;
    private MeshFilter meshFilter;

    public Dictionary<Vector3Int, CaveChunk> Chunks = new Dictionary<Vector3Int, CaveChunk>();

    void Start()
    {
        this.Generate();
    }

    public void Generate()
    {
        foreach (var chunk in Chunks.Values)
        {
            Destroy(chunk.gameObject);
        }

        Chunks.Clear();

        for (int x = 0; x < ChunkDimension; x++)
        {
            for (int y = 0; y < ChunkDimension; y++)
            {
                for (int z = 0; z < ChunkDimension; z++)
                {
                    // Instantiate chunk
                    CaveChunk ch = Instantiate(ChunkPrefab,new Vector3(x * width, y * height, z * depth),Quaternion.identity,this.transform);
                    ch.ChunkPos = new Vector3Int(x, y, z);

                    ch.GetComponent<MeshRenderer>().material = caveMaterial;
                    ch.generator = this;

                    this.Chunks.Add(ch.ChunkPos, ch);

                    ch.GenerateTerrain();
                }
            }
        }
    }

    public float[,,] GenerateInitialDensityMap(Vector3Int chunkPos, Vector3 planetCenter, float planetRadius)
    {
        // Create a density map with an extra layer of padding for marching cubes
        float[,,] densityMap = new float[width + 1, height + 1, depth + 1];

        // Generate noise map
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

                    // Distance from center of the planet
                    Vector3 worldPos = new Vector3(worldX, worldY, worldZ);
                    float dist = Vector3.Distance(worldPos, planetCenter);

                    // Give the planet some roughness.
                    float sphericalNoise = Perlin.Fbm(worldX * 0.06f, worldY * 0.06f, worldZ * 0.06f, 5);
                    float bumpyRadius = planetRadius + (sphericalNoise - 0.5f) * 2f;

                    float density = (bumpyRadius - dist) * 0.05f;

                    float caveNoise = Perlin.Fbm(worldX * 0.08f, worldY * 0.08f, worldZ * 0.08f, 4);
                    density += Mathf.Pow(caveNoise, 3f);

                    densityMap[x, y, z] = density;
                }
            }
        }

        return densityMap;
    }
}