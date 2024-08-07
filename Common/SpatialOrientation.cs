using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Defines possible spatial orientations for objects within the game world.
/// This enum is used to specify the direction an object is facing or pointing,
/// which is crucial for:
/// 
/// - Determining connections between rooms or objects (e.g., a door facing a hallway).
/// - Ensuring correct visual alignment of props and interactive elements.
/// - Potentially guiding navigation and pathfinding behaviors.
/// </summary>
[Serializable, Flags]
public enum SpatialOrientation
{
    [Tooltip("Nothing. Used as a default option.")]
    None = 0, 

    [Tooltip("Facing in the negative X direction (left).")]
    Left = 1 << 0, // 1

    [Tooltip("Facing in the positive Z direction (up).")]
    Up = 1 << 1, // 2

    [Tooltip("Facing in the positive X direction (right).")]
    Right = 1 << 2, // 4

    [Tooltip("Facing in the negative Z direction (down).")]
    Down = 1 << 3, // 8

    [Tooltip("Facing diagonally up and right.")]
    UpRight = Up | Right, // 2 | 4 = 6

    [Tooltip("Facing diagonally up and left.")]
    UpLeft = Up | Left, // 2 | 1 = 3

    [Tooltip("Facing diagonally down and right.")]
    DownRight = Down | Right, // 8 | 4 = 12

    [Tooltip("Facing diagonally down and left.")]
    DownLeft = Down | Left // 8 | 1 = 9
}


/// <summary>
/// Helper methods for handling <see cref="SpatialOrientation"/>.
/// </summary>
public static class SpatialOrientationHelper
{
    /// <summary>
    /// Returns the opposite spatial orientation of the given direction.
    /// </summary>
    /// <param name="direction">The original spatial orientation to be reversed.</param>
    /// <returns>The opposite spatial orientation.</returns>
    public static SpatialOrientation Reverse(this SpatialOrientation direction)
    {
        switch (direction)
        {
            case SpatialOrientation.Up:
                return SpatialOrientation.Down;
            case SpatialOrientation.Right:
                return SpatialOrientation.Left;
            case SpatialOrientation.Down:
                return SpatialOrientation.Up;
            case SpatialOrientation.Left:
                return SpatialOrientation.Right;
            default:
                throw new System.NotSupportedException("SpatialDirection: " + direction.ToSafeString() + " is not supported for reverse.");
        }
    }
}