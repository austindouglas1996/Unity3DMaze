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
}
