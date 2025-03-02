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

    private float[,,] GenerateDensityMap(Vector3Int chunkPos)
    {
        // Create a density map with an extra layer of padding for marching cubes
        float[,,] densityMap = new float[width + 1, height + 1, depth + 1];

        //float minNoise = float.MaxValue;
        //float maxNoise = float.MinValue;

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

                    // Sample 3D Perlin noise at the world coordinates
                    float noiseValue = Perlin.Fbm(worldX * noise, worldY * noise, worldZ * noise, octaves);

                    // Track min and max noise values
                    //if (noiseValue < minNoise) minNoise = noiseValue;
                    //if (noiseValue > maxNoise) maxNoise = noiseValue;

                    /*
                     * Since our world is infinite we cannot use a min/max noise scale.
                     * One chunk may get a min/max of 0.6 while another gets 0.8 causing tears
                     * so to get around this and knowing Perlin.Fbm returns -0.5 to 0.5 when we
                     * want 0 to 1 (for consitency) we will just add 0.5.
                     */
                    noiseValue = (noiseValue + 0.5f);

                    // Store the noise value in the density map
                    densityMap[x, y, z] = noiseValue;
                }
            }
        }

        /*
        // Normalize the noise values to the range [0, 1]
        // Normally our perlin does -0.5 to 0.5
        for (int x = 0; x < width + 1; x++)
        {
            for (int y = 0; y < height + 1; y++)
            {
                for (int z = 0; z < depth + 1; z++)
                {
                    densityMap[x, y, z] = (densityMap[x, y, z] - minNoise) / (maxNoise - minNoise);
                }
            }
        }
        */

        return densityMap;
    }
}