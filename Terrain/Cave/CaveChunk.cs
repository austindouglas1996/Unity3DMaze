using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CaveChunk : MonoBehaviour
{
    public Vector3Int ChunkPos = new Vector3Int(0, 0, 0);
    public float[,,] DensityMap;
    public CaveGenerator generator;

    public void AddTerrain(Vector3 position, float radius, float intensity)
    {
        ModifyTerrain(position, radius, intensity, true);
    }

    public void RemoveTerrain(Vector3 position, float radius, float intensity)
    {
        ModifyTerrain(position, radius, intensity, false);
    }

    public void GenerateTerrain()
    {
        if (DensityMap == null)
        {
            DensityMap = generator.GenerateDensityMap(ChunkPos);
        }

        GetComponent<MeshFilter>().mesh = MarchingCubes.GenerateMesh(DensityMap, generator.width, generator.height, generator.depth, generator.threshold, new Vector3(0, 0, 0));

        UpdateCollider();
    }

    private void ModifyTerrain(Vector3 position, float radius, float intensity, bool addTerrain)
    {
        bool madeChanges = false;

        if (DensityMap == null)
        {
            // This only happens in editor mode when reloading from a code change.
            DensityMap = generator.GenerateDensityMap(ChunkPos);
        }

        int mapWidth = DensityMap.GetLength(0);
        int mapHeight = DensityMap.GetLength(1);
        int mapDepth = DensityMap.GetLength(2);

        for (int z = 0; z < mapDepth; z++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    // Calculate distance from the modification point
                    float distance = Vector3.Distance(new Vector3(x, y, z), position);

                    if (distance <= radius)
                    {
                        // Modify the noise value based on distance and intensity
                        float falloff = 1 - (distance / radius); // Smooth falloff
                        float modification = intensity * falloff;

                        if (addTerrain)
                        {
                            DensityMap[x, y, z] += modification;
                            madeChanges = true;
                        }
                        else
                        {
                            DensityMap[x, y, z] -= modification;
                            madeChanges = true;
                        }

                        // Clamp the noise value to avoid extreme values
                        DensityMap[x, y, z] = Mathf.Clamp(DensityMap[x, y, z], 0, 1f);
                    }
                }
            }
        }

        if (!madeChanges)
            Debug.Log("Made no changes to the densityMap.");
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