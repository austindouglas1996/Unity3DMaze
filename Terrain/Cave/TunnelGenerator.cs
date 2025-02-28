using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TunnelGenerator : MonoBehaviour
{
    public int width = 32;
    public int height = 32;
    public int depth = 32;
    public float threshold = 0.5f;
    public Material caveMaterial;

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
        densityMap = new float[width, height, depth];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    densityMap[x, y, z] = 1f; // Store density
                }
            }
        }

        for (int x = width / 4; x < (3 * width) / 4; x++)
        {
            for (int y = height / 4; y < (3 * height) / 4; y++)
            {
                for (int z = depth / 4; z < (3 * depth) / 4; z++)
                {
                    densityMap[x, y, z] = 0f; // Air pocket
                }
            }
        }
    }
}