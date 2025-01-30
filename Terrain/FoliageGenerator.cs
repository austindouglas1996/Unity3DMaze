using System.Collections.Generic;
using UnityEngine;
using static TerrainChunk;

public class FoliageGenerator : MonoBehaviour
{
    private List<MeshBatchDrawer> batches = new List<MeshBatchDrawer>();

    public float maxGrassHeight = 2.3f;
    public float grassDensity = 10f;

    private void Start()
    {
    }

    private void OnValidate()
    {
        foreach (var batch in batches)
            batch.UpdateFollowerPosition();
    }

    private void Update()
    {
        foreach (var batch in batches)
            batch.Update();
    }

    public void ApplyMap(MapGenerator generator, TerrainThreadData chunkData)
    {
        batches.Clear();

        foreach (var grass in generator.ResourceStore.GrassPrefabs)
            batches.Add(new MeshBatchDrawer(grass, Camera.main));

        foreach (var grass in generator.ResourceStore.FlowersPrefabs)
            batches.Add(new MeshBatchDrawer(grass, Camera.main));

        foreach (var grass in generator.ResourceStore.RocksPrefabs)
            batches.Add(new MeshBatchDrawer(grass, Camera.main));

        ProcessGrassPositions(chunkData.MeshData);
    }

    private void ProcessGrassPositions(MeshData meshData)
    {
        for (int i = 0; i < meshData.triangles.Length; i += 3)
        {
            Vector3 localA = meshData.vertices[meshData.triangles[i]];
            Vector3 localB = meshData.vertices[meshData.triangles[i + 1]];
            Vector3 localC = meshData.vertices[meshData.triangles[i + 2]];

            float averageHeight = (localA.y + localB.y + localC.y) / 3f;
            if (averageHeight < 160f || averageHeight > 200f)
                continue;

            Vector3 vertexA = transform.TransformPoint(localA);
            Vector3 vertexB = transform.TransformPoint(localB);
            Vector3 vertexC = transform.TransformPoint(localC);

            Vector3 triangleNormal = Vector3.Cross(vertexB - vertexA, vertexC - vertexA).normalized;
            Vector3 triangleCenter = (vertexA + vertexB + vertexC) / 3f;

            for (int j = 0; j < 4; j++)
            {
                Vector3 position = RandomPointInTriangle(vertexA, vertexB, vertexC) + triangleNormal * 0.01f;
                Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                Vector3 scale = Vector3.one * Random.Range(0.2f, 4f);

                batches.Random().Add(position, rotation, scale);
            }
        }
    }

    private Vector3 RandomPointInTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        float r1 = Mathf.Sqrt(Random.value);
        float r2 = Random.value;
        return (1 - r1) * a + (r1 * (1 - r2)) * b + (r1 * r2) * c;
    }
}