using static UnityEngine.GraphicsBuffer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MarchingCubesGenerator))]
public class TunnelGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draws the default inspector UI

        MarchingCubesGenerator generator = (MarchingCubesGenerator)target;

        if (GUILayout.Button("Regenerate Cave"))
        {
            generator.Generate();
        }
    }
}
