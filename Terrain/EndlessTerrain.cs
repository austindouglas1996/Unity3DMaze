using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(MapGenerator))]    
public class EndlessTerrain : MonoBehaviour
{
    public Transform Viewer;
    public float ViewerMoveThresholdForUpdate = 50f;
    public Vector2 ChunksToLoad = new Vector2(5, 5);

    private Vector3 lastKnownViewerPosition;

    private MapGenerator mapGenerator;
    private Dictionary<Vector2, TerrainChunk> terrainChunks = new Dictionary<Vector2, TerrainChunk>();
    private List<TerrainChunk> activeChunks = new List<TerrainChunk>();

    public bool forceRegen = false;

    private async void OnValidate()
    {
        if (forceRegen)
        {
            foreach (var chunk in activeChunks)
            {
                await chunk.UpdateTerrainAsync();
            }

            forceRegen = false;
        }
    }

    private void Start()
    {
        this.mapGenerator = GetComponent<MapGenerator>();
        this.UpdateActiveChunks();
    }

    private async void Update()
    {
        float viewerDistance = Vector3.Distance(Viewer.position, lastKnownViewerPosition);
        if (viewerDistance > ViewerMoveThresholdForUpdate)
        {
            lastKnownViewerPosition = Viewer.position;
            await UpdateActiveChunks();
        }
    }

    private async Task UpdateActiveChunks()
    {
        List<TerrainChunk> newChunksVisible = new List<TerrainChunk>();
        Vector2 currentChunkPos = GetClosestChunk(Viewer.position);

        for (int x = -(int)ChunksToLoad.x; x < ChunksToLoad.x; x++)
        {
            for (int y = -(int)ChunksToLoad.y; y < ChunksToLoad.y; y++)
            {
                Vector2 chunkPos = new Vector2(currentChunkPos.x + x, currentChunkPos.y + y);

                if (!terrainChunks.ContainsKey(chunkPos))
                {
                    TerrainChunk newChunk = mapGenerator.GenerateChunkInstance();
                    await newChunk.Generate(this.mapGenerator, chunkPos, mapGenerator.MapChunkSize);
                    terrainChunks.Add(chunkPos, newChunk);
                }

                terrainChunks[chunkPos].SetVisible(true);
                newChunksVisible.Add(terrainChunks[chunkPos]);
            }
        }

        foreach (TerrainChunk chunk in activeChunks.Except(newChunksVisible))
        {
            chunk.SetVisible(false);
        }

        activeChunks = newChunksVisible;
    }

    /// <summary>
    /// Returns the closest terrain chunk to be decided as the assumed primary chunk.
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    private Vector2 GetClosestChunk(Vector3 pos)
    {
        return new Vector2(
            Mathf.Round(pos.x / mapGenerator.MapChunkSize),
            Mathf.Round(pos.z / mapGenerator.MapChunkSize));
    }
}