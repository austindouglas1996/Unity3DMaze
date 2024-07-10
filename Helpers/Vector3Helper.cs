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

    /// <summary>
    /// Returns the distance between two positions.
    /// </summary>
    /// <param name="C1"></param>
    /// <param name="C2"></param>
    /// <returns></returns>
    public static int CalculateDistance(Vector3Int C1, Vector3Int C2)
    {
        // Calculate the absolute difference in x, y, and z coordinates (Manhattan distance)
        int x = Mathf.Abs(C1.x - C2.x);
        int y = Mathf.Abs(C1.y - C2.y);
        int z = Mathf.Abs(C1.z - C2.z);

        return x + y + z;
    }
}
