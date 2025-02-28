using static UnityEngine.GraphicsBuffer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TunnelGenerator))]
public class TunnelGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draws the default inspector UI

        TunnelGenerator generator = (TunnelGenerator)target;

        if (GUILayout.Button("Regenerate Cave"))
        {
            generator.Generate();
        }
    }
}
