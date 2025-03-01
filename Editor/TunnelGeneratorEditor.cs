using static UnityEngine.GraphicsBuffer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CaveGenerator))]
public class TunnelGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draws the default inspector UI

        CaveGenerator generator = (CaveGenerator)target;

        if (GUILayout.Button("Regenerate Cave"))
        {
            generator.Generate();
        }
    }
}
