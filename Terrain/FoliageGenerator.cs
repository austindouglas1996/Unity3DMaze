using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using static TerrainChunk;

public class FoliageGenerator : MonoBehaviour
{
    private MeshBatchDrawer foliageDrawer;

    public float maxGrassHeight = 2.3f;
    public float grassDensity = 10f;

    private MapGenerator _generator;

    private void Update()
    {
        if (foliageDrawer != null)
            foliageDrawer.Update();
    }

    public void ApplyMap(MapGenerator generator, TerrainThreadData chunkData)
    {
        this._generator = generator;

        foliageDrawer = new MeshBatchDrawer(Camera.main);

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
                    foliageDrawer.Add(this._generator.ResourceStore.RocksPrefabs.Random(), position, rotation, rockScale);
                }
            }

            if (averageHeight > 160f)
            {
                Vector3 scale = Vector3.one * Random.Range(0.2f, 4f);
                foliageDrawer.Add(this._generator.ResourceStore.GrassPrefabs.Random(), position, rotation, scale);

                if (roll < flowerChance + rockChance) // Flower spawn, only if rock didn't spawn
                {
                    Vector3 flowerScale = Vector3.one * Random.Range(0.2f, 4f);
                    foliageDrawer.Add(this._generator.ResourceStore.FlowersPrefabs.Random(), position, rotation, flowerScale);
                }
            }
        }
    }
}