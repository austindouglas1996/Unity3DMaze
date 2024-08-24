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

    /// <summary>
    /// Returns a random rotation between 0, 90 and 180 degrees.
    /// </summary>
    /// <returns></returns>
    public static Quaternion GetRandomRotation()
    {
        int randomRotation = UnityEngine.Random.Range(0, 2);
        switch (randomRotation)
        {
            case 0:
                return Quaternion.Euler(0, 0, 0);
            case 1:
                return Quaternion.Euler(0, 90, 0);
            case 2:
                return Quaternion.Euler(0, 180, 0);
            default:
                return Quaternion.identity;
        }
    }
}
