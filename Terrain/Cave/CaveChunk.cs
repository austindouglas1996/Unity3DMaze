using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CaveChunk : MonoBehaviour
{
    public Vector3Int ChunkPos = new Vector3Int(0, 0, 0);
}