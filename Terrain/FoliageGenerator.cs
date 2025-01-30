using System.Collections.Generic;
using UnityEngine;
using static TerrainChunk;

public class FoliageGenerator : MonoBehaviour
{
    private const int MAX_BATCH_SIZE = 1023; // Unity's instance batch limit

    public GameObject grassPrefab;
    public float maxGrassHeight = 2.3f;
    public float grassDensity = 10f;

    private Mesh[] grassLODMeshes;
    private Material grassMaterial;
    private List<List<Matrix4x4>>[] grassInstancesLOD;
    private List<Bounds>[] batchBounds; // Store bounding boxes for each batch
    private LODGroup grassLODGroup;
    private Vector3 lastCameraPosition;

    private void Start()
    {
        ExtractGrassPrefab();
        InitializeLODInstances();
    }

    private void OnValidate()
    {
        ExtractGrassPrefab();
        InitializeLODInstances();

        this.lastCameraPosition = Camera.main.transform.position;
    }

    private void Update()
    {
        Vector3 cameraPos = Camera.main.transform.position;
        if (Vector3.Distance(cameraPos, lastCameraPosition) > 25f)
        {
            UpdateLODs();
            this.lastCameraPosition = cameraPos;
        }    

        RenderGrassInstances();
    }

    public void ApplyMap(MapGenerator generator, TerrainThreadData chunkData)
    {
        ProcessGrassPositions(chunkData.MeshData);
    }

    private void ExtractGrassPrefab()
    {
        grassLODGroup = grassPrefab.GetComponent<LODGroup>();
        if (grassLODGroup == null) return;

        LOD[] lods = grassLODGroup.GetLODs();
        grassLODMeshes = new Mesh[lods.Length];

        for (int i = 0; i < lods.Length; i++)
        {
            if (lods[i].renderers.Length > 0)
            {
                MeshFilter meshFilter = lods[i].renderers[0].GetComponent<MeshFilter>();
                if (meshFilter != null)
                    grassLODMeshes[i] = meshFilter.sharedMesh;

                if (i == 0 && lods[i].renderers[0] != null)
                {
                    grassMaterial = lods[i].renderers[0].sharedMaterial;
                    grassMaterial.enableInstancing = true;
                }
            }
        }
    }

    private void InitializeLODInstances()
    {
        grassInstancesLOD = new List<List<Matrix4x4>>[grassLODMeshes.Length];
        batchBounds = new List<Bounds>[grassLODMeshes.Length];

        for (int i = 0; i < grassInstancesLOD.Length; i++)
        {
            grassInstancesLOD[i] = new List<List<Matrix4x4>>();
            grassInstancesLOD[i].Add(new List<Matrix4x4>()); // Initialize first batch

            batchBounds[i] = new List<Bounds>();
            batchBounds[i].Add(new Bounds()); // Initialize bounds for the first batch
        }
    }

    private void UpdateLODs()
    {
        Vector3 cameraPos = Camera.main.transform.position;

        for (int i = 0; i < grassInstancesLOD.Length; i++)
        {
            var batches = grassInstancesLOD[i];
            var boundsList = batchBounds[i]; // Bounds for current LOD level

            for (int j = 0; j < batches.Count; j++)
            {
                if (batches[j].Count == 0) continue; // Skip empty batches

                Bounds batchBound = boundsList[j]; // Get batch bounds
                float distanceToCamera = Vector3.Distance(batchBound.center, cameraPos);
                int newLodIndex = GetLODIndex(distanceToCamera);

                if (newLodIndex != i) // LOD needs to change
                {
                    // Move entire batch to new LOD level
                    grassInstancesLOD[newLodIndex].Add(batches[j]);
                    batchBounds[newLodIndex].Add(batchBound);

                    // Remove from the old LOD level
                    batches.RemoveAt(j);
                    boundsList.RemoveAt(j);
                    j--; // Adjust index after removal
                }
            }
        }
    }

    private void ProcessGrassPositions(MeshData meshData)
    {
        for (int i = 0; i < meshData.triangles.Length; i += 3)
        {
            Vector3 localA = meshData.vertices[meshData.triangles[i]];
            Vector3 localB = meshData.vertices[meshData.triangles[i + 1]];
            Vector3 localC = meshData.vertices[meshData.triangles[i + 2]];

            float averageHeight = (localA.y + localB.y + localC.y) / 3f;
            if (averageHeight < 160f || averageHeight > 200f)
                continue;

            Vector3 vertexA = transform.TransformPoint(localA);
            Vector3 vertexB = transform.TransformPoint(localB);
            Vector3 vertexC = transform.TransformPoint(localC);

            Vector3 triangleNormal = Vector3.Cross(vertexB - vertexA, vertexC - vertexA).normalized;
            Vector3 triangleCenter = (vertexA + vertexB + vertexC) / 3f;

            for (int j = 0; j < 4; j++)
            {
                Vector3 position = RandomPointInTriangle(vertexA, vertexB, vertexC) + triangleNormal * 0.01f;
                Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                Vector3 scale = Vector3.one * Random.Range(0.2f, 4f);

                float distanceToCamera = Vector3.Distance(position, Camera.main.transform.position);
                int lodIndex = GetLODIndex(distanceToCamera);

                // Add to the appropriate LOD batch
                var batchList = grassInstancesLOD[lodIndex];
                var currentBatch = batchList[batchList.Count - 1];

                if (currentBatch.Count >= MAX_BATCH_SIZE)
                {
                    batchList.Add(new List<Matrix4x4>()); // Start a new batch
                    batchBounds[lodIndex].Add(new Bounds(position, Vector3.zero)); // Initialize bounds for the new batch
                    currentBatch = batchList[batchList.Count - 1];
                }

                currentBatch.Add(Matrix4x4.TRS(position, rotation, scale));

                // Update the batch's bounding box
                Bounds bounds = batchBounds[lodIndex][batchList.Count - 1];
                bounds.Encapsulate(position);
                batchBounds[lodIndex][batchList.Count - 1] = bounds;
            }
        }
    }

    private void RenderGrassInstances()
    {
        if (Camera.main == null) return;

        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

        for (int i = 0; i < grassInstancesLOD.Length; i++)
        {
            var batches = grassInstancesLOD[i];
            var boundsList = batchBounds[i];

            for (int j = 0; j < batches.Count; j++)
            {
                if (batches[j].Count == 0) continue;

                // Test the batch's bounding box against the camera's frustum
                if (GeometryUtility.TestPlanesAABB(frustumPlanes, boundsList[j]))
                {
                    Graphics.DrawMeshInstanced(grassLODMeshes[i], 0, grassMaterial, batches[j]);
                }
            }
        }
    }

    private int GetLODIndex(float distance)
    {
        if (distance < 20f) return 0;
        if (distance < 50f) return 1;
        return 2;
    }

    private Vector3 RandomPointInTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        float r1 = Mathf.Sqrt(Random.value);
        float r2 = Random.value;
        return (1 - r1) * a + (r1 * (1 - r2)) * b + (r1 * r2) * c;
    }
}