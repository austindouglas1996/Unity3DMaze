using UnityEngine;
using System.Collections;
using static UnityEngine.Mesh;
using UnityEngine.Rendering;
using System.Linq;
using DistantLands.Cozy;
using Unity.VisualScripting;
using System.Collections.Generic;
using VHierarchy.Libs;

public enum DrawMode { NoiseMap, ColourMap, Mesh, FalloffMap };

public class MapGenerator : MonoBehaviour
{
    [Header("Drawing Options")]
    public DrawMode drawMode;
    public bool colorBlend; 
    public bool autoUpdate;

    [Header("Map Options")]
    public Vector2 MapChunks = new Vector2(2, 2);
    public int MapChunkSize = 439;
    public int Seed = 2543;
    public Vector2 GlobalOffset;
    public bool useFallOff;
    public TerrainChunk ChunkPrefab;

    [Header("Noise Options")]
    public Noise.NormalizeMode normalizeMode;
    [Range(1f, 500f)] public float NoiseScale;
    [Range(1f, 25f)]  public int octaves;
    [Range(0.1f, 1f)] public float persistance;
    [Range(1f, 5f)]   public float lacunarity;
    public float meshHeightMultiplier = 16f;
    public AnimationCurve meshHeightCurve;

    private float[,] fallOffMap;
    public TerrainType[] Regions;

    private void Start()
    {
    }

    private void OnValidate()
    {
        fallOffMap = FalloffGenerator.GenerateFalloffMap(MapChunkSize);
    }

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(Vector2.zero);

        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        else if (drawMode == DrawMode.ColourMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colourMap, MapChunkSize, MapChunkSize));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            this.GenerateMap();
        }
        else if (drawMode == DrawMode.FalloffMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(MapChunkSize)));
        }
    }

    public void GenerateMap()
    {
        foreach (var chunk in this.GetComponentsInChildren<TerrainChunk>())
        {
            chunk.gameObject.DestroyImmediate();
        }

        for (int x = 0; x < MapChunks.x; x++)
        {
            for (int y = 0; y < MapChunks.y; y++)
            {
                GameObject newChunk = Instantiate(new GameObject(), this.transform);
                TerrainChunk chunk = newChunk.AddComponent<TerrainChunk>();
                chunk.Generate(this, new Vector2(x, y), this.MapChunkSize);
            }
        }
    }

    public MapData GenerateMapData(Vector2 center)
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(MapChunkSize + 2, MapChunkSize + 2, Seed, NoiseScale, octaves, persistance, lacunarity, center, normalizeMode);
        Color[] colourMap = new Color[MapChunkSize * MapChunkSize];

        for (int y = 0; y < MapChunkSize; y++)
        {
            for (int x = 0; x < MapChunkSize; x++)
            {
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < Regions.Length - 1; i++)
                {
                    if (currentHeight >= Regions[i].Height)
                    {
                        float t = Mathf.InverseLerp(Regions[i].Height, Regions[i + 1].Height, currentHeight);
                        colourMap[y * MapChunkSize + x] = colorBlend ? Color.Lerp(Regions[i].Colour, Regions[i + 1].Colour, t) : Regions[i].Colour;
                    }
                    else
                        break;
                }
            }
        }

        return new MapData(noiseMap, colourMap);
    }
}
