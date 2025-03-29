using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CaveChunk : MonoBehaviour
{
    public Vector3Int Coordinates = new Vector3Int(0, 0, 0);
    public Vector3 Position = new Vector3(0, 0, 0);
    public Vector3Int Size = new Vector3Int(0, 0, 0);
    private float[,,] DensityMap;

    public void Generate(CaveGenerator generator, Vector3Int coordinates, Vector3Int size)
    {
        this.Coordinates = coordinates;
        this.Position = new Vector3(coordinates.x * size.x, coordinates.y * size.y, coordinates.z * size.z);
        this.Size = size;

        this.name = $"Chunk X:{coordinates.x} Y:{coordinates.y} Z: {coordinates.z}";

        //DensityMap = MarchingCubes.GenerateRoundMap(size, coordinates, generator.WorldCenter, size.x);
        DensityMap = MarchingCubes.GenerateSquareMap(size, coordinates, generator.noise, generator.octaves);

        this.GenerateTerrain();
    }

    public void UpdateMap(Vector3 hitPoint, float radius, float intensity, bool adding = true)
    {
        MarchingCubes.ModifyMapWithBrush(ref DensityMap, this.Coordinates, hitPoint, radius, intensity, adding);

        this.GenerateTerrain();
    }

    public void GenerateTerrain()
    {
        var cube = new MarchingCube();
        cube.Process(DensityMap, 0.5f, new Vector3(0, 0, 0));

        GetComponent<MeshFilter>().mesh = MeshGenerator.GenerateMarchingCubeMesh(cube);

        UpdateCollider();
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