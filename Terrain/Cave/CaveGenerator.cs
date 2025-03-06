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

    private List<GameObject> Trash = new List<GameObject>();

    void Start()
    {
        this.Generate();
    }

    public void UpdateTerrainMesh(Vector3 chunkPos, Vector3 localPos, bool adding = true)
    {
        CaveChunk chunk = null;
        foreach (Transform child in this.transform)
        {
            chunk = child.GetComponent<CaveChunk>();
            if (chunk != null)
            {
                if (chunk.ChunkPos == chunkPos)
                {
                    chunk = child.GetComponent<CaveChunk>();
                    break;
                }
            }
        }

        if (chunk == null)
        {
            Debug.Log("Failed to find chunk.");
            return;
        }

        if (adding)
            chunk.AddTerrain(localPos, 1f, 1f);
        else
            chunk.RemoveTerrain(localPos, 1f, 1f);

        chunk.GenerateTerrain();
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

                    int worldX = ch.ChunkPos.x * width + x;
                    int worldY = ch.ChunkPos.y * height + y;
                    int worldZ = ch.ChunkPos.z * depth + z;

                    ch.GetComponent<MeshRenderer>().material = caveMaterial;
                    ch.generator = this;
                    ch.GenerateTerrain();
                }
            }
        }
    }

    public float[,,] GenerateDensityMap(Vector3Int chunkPos)
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
                    noiseValue *= Mathf.Abs(Perlin.Fbm(worldX * 0.02f, worldY * 0.02f, worldZ * 0.02f, octaves));

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

                    noiseValue = HandleNoiseForFloor(new Vector3(worldX, worldY, worldZ), noiseValue);
                    noiseValue = HandleNoiseForRoof(new Vector3(worldX, worldY,worldZ), noiseValue);

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

        EncloseDensityMap(ref densityMap);

        return densityMap;
    }

    private void EncloseDensityMap(ref float[,,] densityMap)
    {
        int sizeX = densityMap.GetLength(0);
        int sizeY = densityMap.GetLength(1);
        int sizeZ = densityMap.GetLength(2);

        // Set front and back faces (Z-direction)
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                densityMap[x, y, 0] = 0.5f;
                densityMap[x, y, sizeZ - 1] = 0.5f;
            }
        }

        // Set top and bottom faces (Y-direction)
        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                densityMap[x, 0, z] = 0.5f;
                densityMap[x, sizeY - 1, z] = 0.5f;
            }
        }

        // Set left and right faces (X-direction)
        for (int y = 0; y < sizeY; y++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                densityMap[0, y, z] = 0.5f;
                densityMap[sizeX - 1, y, z] = 0.5f;
            }
        }
    }

    private float HandleNoiseForFloor(Vector3 worldPos, float noiseValueCurrent)
    {
        float worldX = worldPos.x;
        float worldY = worldPos.y;
        float worldZ = worldPos.z;

        // Add bumpy floor at the bottom of the chunk
        float floorHeight = Perlin.Fbm(worldX * 0.1f, 0, worldZ * 0.1f, octaves);
        floorHeight = (floorHeight + 0.5f) * 2f;

        // If the current voxel is below the floor height, make it solid
        if (worldY < floorHeight)
        {
            // Apply additional noise to the floor for variation
            float floorNoise = Perlin.Fbm(worldX * 0.05f, worldY * 0.05f, worldZ * 0.05f, octaves);
            floorNoise = (floorNoise + 0.5f); // Normalize to [0, 1]

            // Blend the floor noise with the base noise
            noiseValueCurrent = Mathf.Lerp(noiseValueCurrent, 1f, floorNoise);
        }

        return noiseValueCurrent;
    }

    private float HandleNoiseForRoof(Vector3 worldPos, float noiseValueCurrent)
    {
        float worldX = worldPos.x;
        float worldY = worldPos.y;
        float worldZ = worldPos.z;

        // Add bumpy ceiling at the top of the chunk
        float ceilingHeight = Perlin.Fbm(worldX * 0.1f, worldY * 0.1f, worldZ * 0.1f, octaves);
        ceilingHeight = (ceilingHeight + 0.5f) * 7f;  // Scale height variation

        // If the current voxel is above the ceiling height, make it solid
        if (worldY > (height - ceilingHeight))
        {
            // Apply additional noise to the ceiling for variation
            float ceilingNoise = Perlin.Fbm(worldX * 0.05f, worldY * 0.05f, worldZ * 0.05f, octaves);
            ceilingNoise = (ceilingNoise + 0.5f); // Normalize to [0, 1]

            // Blend the ceiling noise with the base noise
            noiseValueCurrent = Mathf.Lerp(noiseValueCurrent, 1f, ceilingNoise);
        }

        return noiseValueCurrent;
    }
}