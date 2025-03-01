using System.Collections.Generic;
using UnityEngine;

public static class MarchingCubes
{
    public static Mesh GenerateMesh(
        float[,,] densityMap,
        int width,
        int height,
        int depth,
        float threshold,
        Vector3 chunkOffset
    )
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    // Gather corner values/positions
                    float[] cornerVals = new float[8];
                    Vector3[] cornerPos = new Vector3[8];

                    for (int i = 0; i < 8; i++)
                    {
                        Vector3 offset = MarchingCubesTables.CornerOffsets[i];

                        int cx = x + (int)offset.x;
                        int cy = y + (int)offset.y;
                        int cz = z + (int)offset.z;

                        // Sample from densityMap. We assume it's sized (width+1, height+1, depth+1).
                        cornerVals[i] = densityMap[cx, cy, cz];

                        // The "local" position, plus the chunkOffset to position it in world space:
                        cornerPos[i] = new Vector3(cx, cy, cz) + chunkOffset;
                    }

                    // Build the cubeIndex
                    int cubeIndex = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        if (cornerVals[i] > threshold)
                            cubeIndex |= 1 << i;
                    }

                    // If no geometry, continue
                    if (MarchingCubesTables.TriangleTable[cubeIndex, 0] == -1)
                        continue;

                    // Generate triangles from the lookup table
                    for (int t = 0; MarchingCubesTables.TriangleTable[cubeIndex, t] != -1; t += 3)
                    {
                        int edgeIndex0 = MarchingCubesTables.TriangleTable[cubeIndex, t];
                        int edgeIndex1 = MarchingCubesTables.TriangleTable[cubeIndex, t + 1];
                        int edgeIndex2 = MarchingCubesTables.TriangleTable[cubeIndex, t + 2];

                        // Interpolate each triangle corner
                        Vector3 v1 = InterpolateEdge(
                            threshold,
                            cornerPos[MarchingCubesTables.EdgeConnections[edgeIndex0, 0]],
                            cornerPos[MarchingCubesTables.EdgeConnections[edgeIndex0, 1]],
                            cornerVals[MarchingCubesTables.EdgeConnections[edgeIndex0, 0]],
                            cornerVals[MarchingCubesTables.EdgeConnections[edgeIndex0, 1]]
                        );
                        Vector3 v2 = InterpolateEdge(
                            threshold,
                            cornerPos[MarchingCubesTables.EdgeConnections[edgeIndex1, 0]],
                            cornerPos[MarchingCubesTables.EdgeConnections[edgeIndex1, 1]],
                            cornerVals[MarchingCubesTables.EdgeConnections[edgeIndex1, 0]],
                            cornerVals[MarchingCubesTables.EdgeConnections[edgeIndex1, 1]]
                        );
                        Vector3 v3 = InterpolateEdge(
                            threshold,
                            cornerPos[MarchingCubesTables.EdgeConnections[edgeIndex2, 0]],
                            cornerPos[MarchingCubesTables.EdgeConnections[edgeIndex2, 1]],
                            cornerVals[MarchingCubesTables.EdgeConnections[edgeIndex2, 0]],
                            cornerVals[MarchingCubesTables.EdgeConnections[edgeIndex2, 1]]
                        );

                        // Add to lists
                        int baseIndex = vertices.Count;
                        vertices.Add(v1);
                        vertices.Add(v2);
                        vertices.Add(v3);

                        triangles.Add(baseIndex + 0);
                        triangles.Add(baseIndex + 1);
                        triangles.Add(baseIndex + 2);
                    }
                }
            }
        }

        // Build final mesh
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // In case large chunk
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }


    private static Vector3 InterpolateEdge(float threshold,
                                           Vector3 p1, Vector3 p2,
                                           float valP1, float valP2)
    {
        // Avoid dividing by zero
        if (Mathf.Approximately(valP1, valP2))
        {
            return p1;
        }
        float t = (threshold - valP1) / (valP2 - valP1);
        return Vector3.Lerp(p1, p2, t);
    }
}