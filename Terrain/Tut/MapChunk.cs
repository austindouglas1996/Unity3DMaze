using Unity.VisualScripting;
using UnityEngine;
using VHierarchy.Libs;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MapChunk : MonoBehaviour
{
    public MeshFilter MeshFilter
    {
        get { return GetComponent<MeshFilter>(); }
    }
    public MeshRenderer MeshRenderer
    {
        get { return GetComponent<MeshRenderer>(); }
    }
    private void OnValidate()
    {
    }

    [Range(0, 6)] public int levelOfDetail = 1;
}