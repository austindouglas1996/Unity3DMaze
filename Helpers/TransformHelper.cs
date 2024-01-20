using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class TransformHelper
{
    /// <summary>
    /// Return the BoundingBox <see cref="Bounds"/> variable from a <see cref="Transform"/>. This helper function just helps
    /// eliminate the 100 line text to do something like X.transform.Find("BoundingBox").GetComponent<Renderer>().bounds into one a simple helper.
    /// </summary>
    /// <param name="transform">The transform we'd like to grab the boundingbox from.</param>
    /// <param name="noChildBoundingBox">Does this item not have a child named BoundingBox and instead is maybe a Cube that has a renderer attached?</param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentException"></exception>
    public static Bounds BoundingBox(this Transform transform, bool noChildBoundingBox = false)
    {
        Transform boundingBox = null;

        if (!noChildBoundingBox)
        {
            boundingBox = transform.Find("BoundingBox");

            if (boundingBox == null)
            {
                Debug.LogError(transform.name + " does not have a BoundingBox child attached.");
                throw new System.ArgumentException("Transform must have a BoundingBox child.");
            }
        }
        else if (noChildBoundingBox)
        {
            boundingBox = transform;
        }

        Renderer renderer = null;

        if (boundingBox.TryGetComponent<Renderer>(out renderer)) 
        {
            return renderer.bounds;
        }
        else
        {
            Debug.LogWarning(transform.name + " does not have a renderer attached. Cannot grab renderer.");
            throw new System.ArgumentException("BoundingBox does not have a Renderer attached. Use a 3D cube.");
        }
    }
}
