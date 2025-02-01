using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using static TerrainChunk;

public class FoliageGenerator : MonoBehaviour
{
    private List<MeshBatchDrawer> grassBatches = new List<MeshBatchDrawer>();
    private List<MeshBatchDrawer> flowerBatches = new List<MeshBatchDrawer>();
    private List<MeshBatchDrawer> rockBatches = new List<MeshBatchDrawer>();

    public float maxGrassHeight = 2.3f;
    public float grassDensity = 10f;

    private MapGenerator _generator;

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
        this._generator = generator;

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
        foreach (var position in meshData.GetRandomPositionsInTriangles(this.transform, 1, true, "Default"))
        {
            float roll = Random.value;
            float flowerChance = 0.25f;
            float rockChance = 0.002f;

            float averageHeight = position.y; 
            Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

            if (averageHeight < 160f || averageHeight > 200f)
            {
                if (roll < rockChance)
                {
                    Vector3 rockScale = Vector3.one * Random.Range(0.2f, 25f);
                    rockBatches.Random().Add(position, rotation, rockScale);
                }
            }

            if (averageHeight > 160f)
            {
                Vector3 scale = Vector3.one * Random.Range(0.2f, 4f);
                grassBatches.Random().Add(position, rotation, scale);

                if (roll < flowerChance + rockChance) // Flower spawn, only if rock didn't spawn
                {
                    Vector3 flowerScale = Vector3.one * Random.Range(0.2f, 4f);
                    flowerBatches.Random().Add(position, rotation, flowerScale);
                }
            }
        }
    }
}