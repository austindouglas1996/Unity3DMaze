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

    private Chunk Chunk;
    private GameWorld World;

    /// <summary>
    /// Retrieve the center point of this terrain.
    /// </summary>
    /// <returns></returns>
    public Vector3Int GetCenter()
    {
        return new Vector3Int(Width /2 , 0, Height /2);
    }

    /// <summary>
    /// Returns whether the position is within an edge range.
    /// </summary>
    /// <param name="localX"></param>
    /// <param name="localZ"></param>
    /// <returns></returns>
    public bool IsEdge(int localX, int localZ, int chunkSize = 20, int offset = 0)
    {
        return (localX <= offset ||
                localX >= chunkSize - 1 - offset ||
                localZ <= offset ||
                localZ >= chunkSize - 1 - offset);
    }

    public void Generate(GameWorld gameWorld, Chunk chunk)
    {
        Chunk = chunk;
        World = gameWorld;

        this.Width = World.ChunkCellsWidth;
        this.Height = World.ChunkCellsHeight;

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
        // Store previous row heights for better smoothing
        float[,] heightMap = new float[Width + 1, Height + 1];

        // First Pass: Generate initial heights
        for (int z = 0; z <= Height; z++)
        {
            for (int x = 0; x <= Width; x++)
            {
                heightMap[x, z] = Chunk.GetHeightInChunk(x, z);
            }
        }

        // Second Pass: Apply smoothing
        int sIndex = 0;
        for (int z = 0; z <= Height; z++)
        {
            for (int x = 0; x <= Width; x++)
            {
                float currentY = heightMap[x, z];

                // Assign smoothed height
                Vertices[sIndex] = new Vector3(x * World.CellSize, currentY, z * World.CellSize);

                // Apply edge heat visualization
                if (IsEdge(x, z) && World.ShowEdgeHeat)
                {
                    Colors[sIndex] = Color.red;
                }
                else
                {
                    var worldPos = World.GridToWorldPosition(Chunk.X, Chunk.Z, x, z);
                    Colors[sIndex] = World.GetBiome(worldPos.x, worldPos.z).terrainColor;
                }

                sIndex++;
            }
        }
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
