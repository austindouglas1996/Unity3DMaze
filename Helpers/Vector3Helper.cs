using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Vector3Helper
{
    /// <summary>
    /// Convert a <see cref="Vector3"/> to a <see cref="Vector3Int"/> instance.
    /// </summary>
    /// <param name="vector"></param>
    /// <returns></returns>
    public static Vector3Int RoundToInt(this Vector3 vector)
    {
        return new Vector3Int(
            Mathf.RoundToInt(vector.x),
            Mathf.RoundToInt(vector.y),
            Mathf.RoundToInt(vector.z));
    }

    public static Vector3 ToVector3(this Vector3Int vector)
    {
        return new Vector3(
            vector.x,
            vector.y,
            vector.z);
    }

    public static Vector3 Multiply(this Vector3 v1, float scalar)
    {
        return new Vector3(v1.x * scalar, v1.y * scalar, v1.z * scalar);
    }
}
