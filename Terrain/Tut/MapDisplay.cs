using UnityEngine;
using System.Collections;

public class MapDisplay : MonoBehaviour
{
    public Renderer textureRender;

    public void DrawTexture(Texture2D texture)
    {
        textureRender.sharedMaterial.mainTexture = texture;
        textureRender.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }

    public void DrawMesh(MapChunk chunk, MeshData meshData, Texture2D texture)
    {
        chunk.MeshFilter.sharedMesh = meshData.CreateMesh();
        chunk.MeshRenderer.sharedMaterial.mainTexture = texture;
    }
}