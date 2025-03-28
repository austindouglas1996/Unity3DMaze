using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CaveChunk : MonoBehaviour
{
    public Vector3Int ChunkPos = new Vector3Int(0, 0, 0);
    public float[,,] DensityMap;
    public bool forceUpdate = false;
    public CaveGenerator generator;

    private void Update()
    {
        if (forceUpdate)
        {
            forceUpdate = false;
            DensityMap = generator.UpdateDensityMap(ChunkPos);

            GenerateTerrain();
        }
    }

    public void GenerateTerrain()
    {
        if (DensityMap == null)
        {
            DensityMap = generator.GenerateInitialDensityMap(ChunkPos, new Vector3(generator.width, generator.height, generator.depth), generator.width);
        }

        GetComponent<MeshFilter>().mesh = MarchingCubes.GenerateMesh(DensityMap, generator.width, generator.height, generator.depth, generator.threshold, new Vector3(0, 0, 0));

        UpdateCollider();
    }

    public void ModifyTerrainWorld(Vector3 worldCenter, float radius, float intensity, bool add)
    {
        Vector3 chunkWorldOrigin = new Vector3(
            ChunkPos.x * generator.width,
            ChunkPos.y * generator.height,
            ChunkPos.z * generator.depth
        );

        for (int x = 0; x <= generator.width; x++)
        {
            for (int y = 0; y <= generator.height; y++)
            {
                for (int z = 0; z <= generator.depth; z++)
                {
                    Vector3 voxelWorldPos = chunkWorldOrigin + new Vector3(x, y, z);
                    float dist = Vector3.Distance(voxelWorldPos, worldCenter);
                    if (dist > radius) continue;

                    float falloff = 1 - (dist / radius);
                    float mod = intensity * falloff;

                    if (add)
                        DensityMap[x, y, z] += mod;
                    else
                        DensityMap[x, y, z] -= mod;

                    DensityMap[x, y, z] = Mathf.Clamp(DensityMap[x, y, z], 0f, 1f);
                }
            }
        }

        // Trigger regeneration
        this.GenerateTerrain();
    }


    private void UpdateCollider()
    {
        if (GetComponent<MeshCollider>() != null)
        {
            Destroy(GetComponent<MeshCollider>());
        }

        this.gameObject.AddComponent<MeshCollider>();
    }
}