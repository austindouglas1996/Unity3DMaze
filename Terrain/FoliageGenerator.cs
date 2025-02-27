using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using static MeshData;
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

        foliageDrawer = null;
        foliageDrawer = new MeshBatchDrawer(Camera.main);

        ProcessGrassPositions(chunkData.MeshData);
    }

    private void ProcessGrassPositions(MeshData meshData)
    {
        foreach (TrianglePOS tria in meshData.GetRandomPositionsInTriangles(this.transform, 2, true, "Default"))
        {
            float roll = Random.value;
            float flowerChance = 0.25f;
            float rockChance = 0.002f;

            float averageHeight = tria.Position.y;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, tria.Normal) * Quaternion.Euler(0, Random.Range(0, 360), 0);

            if (averageHeight > 160 || averageHeight < 200)
            {
                if (roll < rockChance)
                {
                    Vector3 rockScale = Vector3.one * Random.Range(0.2f, 25f);
                    foliageDrawer.Add(this._generator.ResourceStore.RocksPrefabs.Random(), tria.Position, rotation, rockScale);
                }
            }

            if (averageHeight > 160 && averageHeight < 200f)
            {
                Vector3 scale = Vector3.one * Random.Range(1.3f, 2f);
                foliageDrawer.Add(this._generator.ResourceStore.GrassPrefabs.Random(), tria.Position, rotation, scale);

                if (roll < flowerChance + rockChance) // Flower spawn, only if rock didn't spawn
                {
                    Vector3 flowerScale = Vector3.one * Random.Range(1.3f, 2.5f);
                    foliageDrawer.Add(this._generator.ResourceStore.FlowersPrefabs.Random(), tria.Position, rotation, flowerScale);
                }
            }
        }
    }
}