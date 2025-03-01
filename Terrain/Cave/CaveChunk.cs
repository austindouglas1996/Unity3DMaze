using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CaveChunk : MonoBehaviour
{
    public Vector3Int ChunkPos = new Vector3Int(0, 0, 0);

    public int width = 8;
    public int height = 8;
    public int depth = 8;

    public float[,,] densityMap;

    private bool drawn = false;
    private void Update()
    {
        if (densityMap != null & !drawn)
        {
            drawn = true;
            //VisualizeDensityMap();
        }
    }

    private void VisualizeDensityMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    float density = densityMap[x, y, z]; // Get density value

                    // Interpolate color (1 = white/solid, 0 = black/air)
                    Color cubeColor = Color.Lerp(Color.black, Color.white, density);

                    CreateCube(x, y, z, cubeColor, density); // Pass color instead of material
                }
            }
        }
    }

    private void CreateCube(int x, int y, int z, Color color, float density)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = this.transform.position + new Vector3(x * 1f, y * 1f, z * 1f);
        cube.transform.localScale = Vector3.one * 0.2f;

        // Get the renderer
        Renderer renderer = cube.GetComponent<Renderer>();

        // Create a new material with the Unlit shader (which works with color)
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = color;

        // Assign the material
        renderer.material = mat;

        cube.name = Mathf.Clamp(density, 0f,1f).ToString();
        cube.transform.SetParent(this.transform); // Keep hierarchy clean
    }
}