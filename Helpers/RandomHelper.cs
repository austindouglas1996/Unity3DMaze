using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class RandomHelper
{
    /// <summary>
    /// Helper method for using <see cref="Random.Range(int, int)"/> in less lines.
    /// </summary>
    /// <param name="max"></param>
    /// <returns></returns>
    public static bool Chance(int max)
    {
        int randomInt = Random.Range(0, 100);
        return randomInt <= max;
    }
}
