using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.XR;
using Random = UnityEngine.Random;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PolygonTerrain : MonoBehaviour
{
    private Mesh Mesh;
    private Vector2[] Uvs;
    public Vector3[] Vertices;
    public int[] Triangles;
    private Color[] Colors;

    public int Width;
    public int Height;

    public void Generate(GameWorld gameWorld, Chunk chunk)
    {


        GenerateTerrain();
    }

    private void GenerateTerrain()
    {
        Mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = Mesh;

        // Calculate the number of vertices and triangles
        int vertexCount = (Width + 1) * (Height + 1);
        int triangleCount = Width * Height * 6;

        // Initialize arrays
        Uvs = new Vector2[vertexCount];
        Vertices = new Vector3[vertexCount];
        Colors = new Color[vertexCount];
        Triangles = new int[triangleCount];

        CalculateVertices();
        AssignUV();
        GenerateTriangles();
        UpdateMesh();
    }

    private void CalculateVertices()
    {
        /*
        var noise = 1f;// Noise.GenerateNoiseMap(Width, Height, 0.3f);

        // Calculate vertices
        int sIndex = 0;
        for (int z = 0; z <= Height; z++)
        {
            for (int x = 0; x <= Width; x++)
            {
                Vertices[sIndex] = new Vector3(x * World.CellSize, 1f, z * World.CellSize);

                var worldPos = World.GridToWorldPosition(Chunk.X, Chunk.Z, x, z);
                Colors[sIndex] = Color.Lerp(Color.black, Color.white, noise[x, z]);

                sIndex++;
            }
        }
        */
    }

    private void AssignUV()
    {
        // Assign UVs (normalized to [0,1])
        for (int z = 0; z <= Height; z++)
        {
            for (int x = 0; x <= Width; x++)
            {
                int index = z * (Width + 1) + x;
                Uvs[index] = new Vector2((float)x / Width, (float)z / Height);
            }
        }
    }

    private void GenerateTriangles()
    {
        int triIndex = 0;
        for (int z = 0; z < Height; z++)
        {
            for (int x = 0; x < Width; x++)
            {
                int start = z * (Width + 1) + x;

                // First triangle (top-left to bottom-right)
                Triangles[triIndex++] = start;
                Triangles[triIndex++] = start + Width + 1;
                Triangles[triIndex++] = start + 1;

                // Second triangle (bottom-left to top-right)
                Triangles[triIndex++] = start + 1;
                Triangles[triIndex++] = start + Width + 1;
                Triangles[triIndex++] = start + Width + 2;
            }
        }
    }

    private void UpdateMesh()
    {
        Mesh.Clear();
        Mesh.vertices = Vertices;
        Mesh.triangles = Triangles;
        Mesh.colors = Colors;
        Mesh.uv = Uvs;
        Mesh.RecalculateNormals();
    }
}
