using UnityEngine;
using System.Collections;
using static UnityEngine.Mesh;
using UnityEngine.Rendering;
using System.Linq;
using DistantLands.Cozy;

public enum DrawMode { NoiseMap, ColourMap, Mesh };

public class MapGenerator : MonoBehaviour
{
    [Header("Drawing Options")]
    public DrawMode drawMode;
    public bool colorBlend; 
    public bool autoUpdate;

    [Header("Map Options")]
    public Vector2 MapChunks = new Vector2(2, 2);
    public int MapChunkSize = 250;
    public int Seed = 2543;
    public Vector2 GlobalOffset; 
    public TerrainType[] Regions;
    public MapChunk ChunkPrefab;

    [Header("Noise Options")]
    [Range(1f, 250f)] public float NoiseScale;
    [Range(1f, 25f)]  public int octaves;
    [Range(0.1f, 1f)] public float persistance;
    [Range(1f, 5f)]   public float lacunarity;
    public float heightMultiplier = 16f;
    public AnimationCurve meshHeightCurve;

    private GameObject Chunks;

    private void Start()
    {
    }

    private void OnValidate()
    {
        if (Chunks == null)
        {
            Chunks = Instantiate(new GameObject(), this.transform);
            Chunks.name = "Chunks";
        }
    }

    public void GenerateMap()
    {
        while (Chunks.transform.childCount != 0)
        {
            foreach (Transform child in Chunks.transform)
            {
                DestroyImmediate(child.gameObject);
            }
        }

        for (int x = 0; x < MapChunks.x; x++)
        {
            for (int y = 0; y < MapChunks.y; y++)
            {
                GenerateChunk(new Vector2(x,y));
            }
        }
    }

    public MapChunk GenerateChunk(Vector2 offset)
    {
        Vector2 worldPos = new Vector2(offset.x * MapChunkSize, offset.y * MapChunkSize);

        MapChunk newChunk = Instantiate(ChunkPrefab, new Vector3(worldPos.x, 0, worldPos.y), Quaternion.identity, this.Chunks.transform);
        newChunk.name = $"Chunk_{newChunk.transform.position.x}_{newChunk.transform.position.z}";

        float[,] noiseMap = Noise.GenerateNoiseMap(MapChunkSize, MapChunkSize, Seed, NoiseScale, octaves, persistance, lacunarity, worldPos / NoiseScale);
        Color[] colourMap = new Color[MapChunkSize * MapChunkSize];

        for (int y = 0; y < MapChunkSize; y++)
        {
            for (int x = 0; x < MapChunkSize; x++)
            {
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < Regions.Length - 1; i++)
                {
                    if (currentHeight <= Regions[i + 1].Height)
                    {
                        float t = Mathf.InverseLerp(Regions[i].Height, Regions[i + 1].Height, currentHeight);
                        colourMap[y * MapChunkSize + x] = colorBlend ? Color.Lerp(Regions[i].Colour, Regions[i + 1].Colour, t) : Regions[i].Colour;
                        break;
                    }
                }
            }
        }

        MeshData meshData = MeshGenerator.GenerateTerrainMesh(noiseMap, heightMultiplier, meshHeightCurve, 1);
        Texture2D meshTexture = TextureGenerator.TextureFromColourMap(colourMap, MapChunkSize, MapChunkSize);

        newChunk.MeshFilter.sharedMesh = meshData.CreateMesh();
        newChunk.MeshRenderer.material = new Material(Shader.Find("Unlit/Texture"));
        newChunk.MeshRenderer.material.mainTexture = meshTexture;


        return newChunk;
    }
}
