using UnityEngine;

public static class DistanceHelper
{
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

    /// <summary>
    /// Determine the best direction to move from one position to another. Helps with
    /// navigating cell positions from left to right, or up and down.
    /// </summary>
    /// <param name="APOS"></param>
    /// <param name="BPOS"></param>
    /// <returns></returns>
    public static Vector3 DetermineDirectionBetweenPointsXZ(MazeGrid grid, Vector3Int APOS, Vector3Int BPOS)
    {
        // Create temporary positions for checking the direction.
        Vector3Int Z = new Vector3Int(APOS.x, APOS.y, APOS.z + (APOS.z < BPOS.z ? 4 : -4));
        Vector3Int X = new Vector3Int(APOS.x + (APOS.x < BPOS.x ? 4 : -4), APOS.y, APOS.z);

        // Check if moving in the Z direction is possible.
        if (!grid.IsValid(X))
        {
            return new Vector3(0, 0, 1); // Move right/left
        }

        // If not, move in the X direction.
        return new Vector3(1, 0, 0); // Move up/down
    }

    /// <summary>
    /// Helps with determining the direction between cells.
    /// </summary>
    /// <param name="curr"></param>
    /// <param name="dest"></param>
    /// <returns></returns>
    public static SpatialOrientation GetSpatialOrientation(Cell curr, Cell dest)
    {
        int dx = dest.Position.x - curr.Position.x;
        int dz = dest.Position.z - curr.Position.z;

        if (dx > 0)
        {
            return SpatialOrientation.Right;  // Moving right (positive X direction)
        }
        else if (dx < 0)
        {
            return SpatialOrientation.Left;   // Moving left (negative X direction)
        }
        else if (dz > 0)
        {
            return SpatialOrientation.Up;     // Moving up (positive Z direction)
        }
        else if (dz < 0)
        {
            return SpatialOrientation.Down;   // Moving down (negative Z direction)
        }

        return SpatialOrientation.None;       // No movement or invalid direction
    }

    /// <summary>
    /// Determines if moving from point a to point b is in a positive direction.
    /// </summary>
    /// <param name="a">Starting point</param>
    /// <param name="b">Ending point</param>
    /// <returns>True if moving in a positive direction, otherwise false for negative</returns>
    public static bool IsPositiveDirection(float a, float b)
    {
        return (b > a);
    }

    /// <summary>
    /// Determines a direction that a <see cref="Transform"/> is facing.
    /// </summary>
    /// <param name="transform"></param>
    /// <returns></returns>
    public static SpatialOrientation GetDirection(this Transform transform)
    {
        Vector3 forward = transform.forward;

        // Compare the forward vector's components to determine the direction
        if (Mathf.Abs(forward.x) > Mathf.Abs(forward.z))
            return forward.x > 0 ? SpatialOrientation.Up : SpatialOrientation.Down;
        else
            return forward.z > 0 ? SpatialOrientation.Left : SpatialOrientation.Right;
    }
}