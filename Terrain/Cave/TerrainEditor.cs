using UnityEngine;

public class TerrainEditor : MonoBehaviour
{
    public float brushRadius = 5f;
    public float brushIntensity = 0.1f;
    public float isolevel = 0.5f;
    public CaveGenerator generator;

    void Update()
    {
        if (Input.GetMouseButton(0)) // Left click to add terrain
        {
            TryModifyTerrain(true);
        }
        else if (Input.GetMouseButton(1)) // Right click to remove terrain
        {
            TryModifyTerrain(false);
        }
    }

    private void TryModifyTerrain(bool adding)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int layerMask = 1 << LayerMask.NameToLayer("Default");

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
        {
            Vector3 chunkPos = GetClosestChunk(this.transform.position);
            Vector3 chunkWorldPos = GetClosestChunkInWorld(hit.point);
            Vector3 localPos = hit.point - chunkWorldPos;

            generator.UpdateTerrainMesh(GetClosestChunk(chunkWorldPos), localPos, adding);
        }
    }

    private Vector3 GetClosestChunk(Vector3 pos)
    {
        return new Vector3(
            Mathf.Round(pos.x / generator.width),
            Mathf.Round(pos.y / generator.height),
            Mathf.Round(pos.z / generator.depth));
    }

    private Vector3 GetClosestChunkInWorld(Vector3 worldPos)
    {
        // Calculate the chunk's origin in world space
        return new Vector3(
            Mathf.Floor(worldPos.x / generator.width) * generator.width,
            Mathf.Floor(worldPos.y / generator.height) * generator.height,
            Mathf.Floor(worldPos.z / generator.depth) * generator.depth);
    }
}