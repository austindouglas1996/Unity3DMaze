using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CaveGenerator : MonoBehaviour
{
    [Header("Chunk")]
    public Vector3Int ChunkSize = new Vector3Int(32, 32, 32);
    public Vector3Int ChunkToRender = new Vector3Int(2, 2, 2);

    [Header("Noise")]
    public float threshold = 0.5f;
    public int octaves = 12;
    public float noise = 0.1f;
    public float floorHeight = 0.2f;

    [Header("Rendering")]
    public Material caveMaterial;
    public CaveChunk ChunkPrefab;

    public Vector3 WorldCenter;
    public Dictionary<Vector3Int, CaveChunk> Chunks = new Dictionary<Vector3Int, CaveChunk>();

    void Start()
    {
        WorldCenter = ChunkSize;
        this.Generate();
    }

    public void Generate()
    {
        foreach (var chunk in Chunks.Values)
        {
            Destroy(chunk.gameObject);
        }

        Chunks.Clear();

        for (int x = 0; x < ChunkToRender.x; x++)
        {
            for (int y = 0; y < ChunkToRender.y; y++)
            {
                for (int z = 0; z < ChunkToRender.z; z++)
                {
                    // Instantiate chunk
                    CaveChunk ch = Instantiate(ChunkPrefab,new Vector3(x * ChunkSize.x, y * ChunkSize.y, z * ChunkSize.z),Quaternion.identity,this.transform);
                    ch.Generate(this, new Vector3Int(x, y, z), this.ChunkSize);
                    ch.GetComponent<MeshRenderer>().material = caveMaterial;

                    this.Chunks.Add(new Vector3Int(x, y, z), ch);

                    ch.GenerateTerrain();
                }
            }
        }
    }
}