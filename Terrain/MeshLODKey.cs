using UnityEngine;

internal struct MeshLODKey
{
    public GameObject GameObject { get; }
    public int LODIndex { get; }

    public MeshLODKey(GameObject gameObject, int lodIndex = 0)
    {
        GameObject = gameObject;
        LODIndex = lodIndex;
    }

    public override bool Equals(object obj)
    {
        if (obj is MeshLODKey other)
        {
            return GameObject == other.GameObject && LODIndex == other.LODIndex;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return GameObject.GetHashCode() ^ LODIndex.GetHashCode();
    }
}