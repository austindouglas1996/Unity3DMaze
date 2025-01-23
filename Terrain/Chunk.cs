using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(PolygonTerrain))]
public class Chunk : MonoBehaviour
{
    public int X = 0;
    public int Z = 0;

    public GameWorld World;
    public PolygonTerrain Terrain;

    /// <summary>
    /// Called when the component is initalized.
    /// </summary>
    private void Start()
    {
        this.Terrain = this.GetComponent<PolygonTerrain>();
        this.AddComponent<MeshCollider>();
    }

    /// <summary>
    /// Update and show certain entities based on distance.
    /// </summary>
    private void Update()
    {
        return;
        if (Vector3.Distance(Camera.main.transform.position, transform.position) < 260f)
        {
            //if (this.Biome.grassScale != 0)
                //Graphics.DrawMeshInstanced(grassMesh, 0, grassMaterial, grassInstances);
        }
    }

    /// <summary>
    /// Get the height of a specific point in a chunk.
    /// </summary>
    /// <param name="biome"></param>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public float GetHeightInChunk(int localX, int localZ)
    {
        Vector3 worldPos = World.GridToWorldPosition(X, Z, localX, localZ);
        float currentHeight = World.GetVertexHeight(this, localX, localZ);

        var (neighborChunk, distanceFactor) = GetClosestNeighbor(localX, localZ);
        if (neighborChunk != null)
        {
            Vector3Int neighborPos = GetMappedLocalPosition(neighborChunk, localX, localZ);
            float neighborHeight = World.GetVertexHeight(neighborChunk, neighborPos.x, neighborPos.z);

            return Mathf.Lerp(currentHeight, neighborHeight, distanceFactor * World.WorldEdgeBlend);
        }

        return currentHeight;
    }

    /// <summary>
    /// Generate the terrain in this chunk.
    /// </summary>
    /// <exception cref="System.Exception"></exception>
    public void GenerateTerrain()
    {
        this.Terrain.Generate(this.World, this);
    }

    /// <summary>
    /// Returns the neighbor grid position.
    /// </summary>
    /// <param name="localX"></param>
    /// <param name="localZ"></param>
    /// <returns></returns>
    private (Chunk, float) GetClosestNeighbor(int localX, int localZ)
    {
        int neighborGridX = X;
        int neighborGridZ = Z;

        // **X Movement (Up/Down in Grid)**
        // If we're near the top edge, move up in the grid (decreasing GridX)
        if (localX < 0)
            neighborGridX -= 1;

        // If we're near the bottom edge, move down in the grid (increasing GridX)
        if (localX >= World.ChunkCellsWidth)
            neighborGridX += 1;

        // **Z Movement (Left/Right in Grid)**
        // If we're near the left edge, move left in the grid (decreasing GridZ)
        if (localZ < 0)
            neighborGridZ -= 1;

        // If we're near the right edge, move right in the grid (increasing GridZ)
        if (localZ >= World.ChunkCellsHeight)
            neighborGridZ += 1;

        // Don't return our chunk.
        if (neighborGridX == X && neighborGridZ == Z)
            return (null, 0f); // throw new System.ArgumentException("Failed to locate neighbor chunk.");

        return (World.GetChunk(neighborGridX, neighborGridZ), 3f);
    }

    /// <summary>
    /// Maps a local position in this chunk to the corresponding position in a neighboring chunk.
    /// </summary>
    /// <param name="b">The neighboring chunk</param>
    /// <param name="aX">Local X coordinate in this chunk</param>
    /// <param name="aZ">Local Z coordinate in this chunk</param>
    /// <returns>Mapped local position in chunk B</returns>
    private Vector3Int GetMappedLocalPosition(Chunk b, int aX, int aZ)
    {
        if (b == this)
            throw new System.ArgumentException("Tried to map local position to local chunk.");

        Vector3Int neighborDiff = new Vector3Int(Mathf.Clamp(b.X - X, -1, 1), 0, Mathf.Clamp(b.Z - Z, -1, 1));

        return new Vector3Int(
            (neighborDiff.x == -1) ? (World.ChunkCellsWidth - 1) : (neighborDiff.x == 1 ? 0 : aX),
            0,
            (neighborDiff.z == -1) ? (World.ChunkCellsHeight - 1) : (neighborDiff.z == 1 ? 0 : aZ)
        );
    }
}