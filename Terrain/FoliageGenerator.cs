using System.Collections.Generic;
using UnityEngine;
using static TerrainChunk;

public class FoliageGenerator : MonoBehaviour
{
    private List<MeshBatchDrawer> grassBatches = new List<MeshBatchDrawer>();
    private List<MeshBatchDrawer> flowerBatches = new List<MeshBatchDrawer>();
    private List<MeshBatchDrawer> rockBatches = new List<MeshBatchDrawer>();

    public float maxGrassHeight = 2.3f;
    public float grassDensity = 10f;

    private void Start()
    {
    }

    private void OnValidate()
    {
        foreach (var batch in grassBatches)
            batch.UpdateFollowerPosition();
    }

    private void Update()
    {
        foreach (var batch in grassBatches)
            batch.Update();

        foreach (var batch in flowerBatches)
            batch.Update();

        foreach (var batch in rockBatches)
            batch.Update();
    }

    public void ApplyMap(MapGenerator generator, TerrainThreadData chunkData)
    {
        grassBatches.Clear();
        flowerBatches.Clear();
        rockBatches.Clear();

        foreach (var grass in generator.ResourceStore.GrassPrefabs)
            grassBatches.Add(new MeshBatchDrawer(grass, Camera.main));

        foreach (var grass in generator.ResourceStore.FlowersPrefabs)
            flowerBatches.Add(new MeshBatchDrawer(grass, Camera.main));

        foreach (var grass in generator.ResourceStore.RocksPrefabs)
            rockBatches.Add(new MeshBatchDrawer(grass, Camera.main));

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

            bool spawnedFlowerOrRock = false;

            for (int j = 0; j < 4; j++)
            {
                Vector3 position = RandomPointInTriangle(vertexA, vertexB, vertexC) + triangleNormal * 0.01f;
                Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

                Vector3 scale = Vector3.one * Random.Range(0.2f, 4f);
                grassBatches.Random().Add(position, rotation, scale);

                float flowerChance = 0.25f;
                float rockChance = 0.001f;

                // If we haven't already spawned a flower or rock in this loop
                if (!spawnedFlowerOrRock)
                {
                    float roll = Random.value; // Random range between 0 and 1

                    if (roll < rockChance)
                    {
                        Vector3 rockScale = Vector3.one * Random.Range(0.2f, 25f);
                        rockBatches.Random().Add(position, rotation, rockScale);
                        spawnedFlowerOrRock = true; // Ensure no more flowers or rocks in this 4-loop
                    }
                    if (roll < flowerChance + rockChance) // Flower spawn, only if rock didn't spawn
                    {
                        Vector3 flowerScale = Vector3.one * Random.Range(0.2f, 4f);
                        flowerBatches.Random().Add(position, rotation, flowerScale);
                        spawnedFlowerOrRock = true;
                    }
                }
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