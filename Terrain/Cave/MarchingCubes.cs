using System.Collections.Generic;
using UnityEngine;

public static class MarchingCubes
{
    public static Mesh GenerateMesh(float[,,] densityMap, int width, int height, int depth, float threshold)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int x = 0; x < width -1; x++)
        {
            for (int y = 0; y < height-1; y++)
            {
                for (int z = 0; z < depth-1; z++)
                {
                    float[] cubeCorners = new float[8];
                    Vector3[] cubePositions = new Vector3[8];

                    for (int i = 0; i < 8; i++)
                    {
                        Vector3 pos = new Vector3(x, y, z) + MarchingCubesTables.CornerOffsets[i];
                        cubeCorners[i] = densityMap[(int)pos.x, (int)pos.y, (int)pos.z];
                        cubePositions[i] = pos;
                    }

                    int cubeIndex = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        if (cubeCorners[i] > threshold)
                            cubeIndex |= 1 << i;
                    }

                    if (MarchingCubesTables.TriangleTable[cubeIndex, 0] == -1)
                        continue;

                    for (int i = 0; MarchingCubesTables.TriangleTable[cubeIndex, i] != -1; i += 3)
                    {
                        int a0 = MarchingCubesTables.TriangleTable[cubeIndex, i];
                        int b0 = MarchingCubesTables.TriangleTable[cubeIndex, i + 1];
                        int c0 = MarchingCubesTables.TriangleTable[cubeIndex, i + 2];

                        Vector3 vert1 = InterpolateEdge(densityMap, threshold, cubePositions[MarchingCubesTables.EdgeConnections[a0, 0]],
                                                        cubePositions[MarchingCubesTables.EdgeConnections[a0, 1]]);
                        Vector3 vert2 = InterpolateEdge(densityMap, threshold, cubePositions[MarchingCubesTables.EdgeConnections[b0, 0]],
                                                        cubePositions[MarchingCubesTables.EdgeConnections[b0, 1]]);
                        Vector3 vert3 = InterpolateEdge(densityMap, threshold, cubePositions[MarchingCubesTables.EdgeConnections[c0, 0]],
                                                        cubePositions[MarchingCubesTables.EdgeConnections[c0, 1]]);

                        int vertIndex = vertices.Count;
                        vertices.Add(vert1);
                        vertices.Add(vert2);
                        vertices.Add(vert3);

                        // Fix normals by ensuring correct triangle order
                        triangles.Add(vertIndex);
                        triangles.Add(vertIndex + 1);
                        triangles.Add(vertIndex + 2);
                    }
                }
            }

            Debug.Log($"Marching Cubes processing: Width={width}, Height={height}, Depth={depth}");
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Vector3 InterpolateEdge(float[,,] densityMap, float threshold, Vector3 v1, Vector3 v2)
    {
        float d1 = densityMap[(int)v1.x, (int)v1.y, (int)v1.z];
        float d2 = densityMap[(int)v2.x, (int)v2.y, (int)v2.z];

        if (Mathf.Approximately(d1, d2)) return (v1 + v2) * 0.5f; // Avoid division by zero

        float t = (threshold - d1) / (d2 - d1);
        return v1 + t * (v2 - v1);
    }
}