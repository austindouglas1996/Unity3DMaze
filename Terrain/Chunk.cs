using UnityEngine;

[RequireComponent(typeof(PolygonTerrain))]
public class Chunk : MonoBehaviour
{
    public int GridX = 0;
    public int GridZ = 0;

    public Biome Biome;
    [SerializeField] public PolygonTerrain Terrain;

    private void Start()
    {
        this.Terrain = this.GetComponent<PolygonTerrain>();
    }

    public void GenerateTerrain(GameWorld world, Biome biome)
    {
        this.Terrain.Generate(world, biome);
    }
}