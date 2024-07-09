using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEditor.PlayerSettings;

/// <summary>
/// Represents a grid to help identify possible empty slots.
/// </summary>
public class MazeGrid
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MazeGrid"/> class.
    /// </summary>
    /// <param name="mazeGenerator"></param>
    public MazeGrid()
    {
    }

    /// <summary>
    /// Gets a list of filled cells.
    /// </summary>
    public List<Cell> Cells = new List<Cell>();

    /// <summary>
    /// Returns the default cell.
    /// </summary>
    public Cell Cell(Vector3Int position)
    {
        return new Cell() { Position = position, Type = CellType.None };
    }

    /// <summary>
    /// Gets or sets the cell at a position.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public Cell this[int x, int y, int z]
    {
        get
        {
            return this[new Vector3Int(x, y, z)];
        }
        set
        {
            this[new Vector3Int(x, y, z)] = value;
        }
    }

    /// <summary>
    /// Gets or sets the value of a cell.
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public Cell this[Vector3Int pos]
    {
        get
        {
            Cell c = Cells.FirstOrDefault(r => r.Position == pos);
            if (c == null)
            {
                c = Cell(pos);
            }

            return c;
        }
        set
        {
            int index = Cells.IndexOf(this[value.Position]);
            this[value.Position] = value;
        }
    }

    /// <summary>
    /// Returns the neighbor in the direction of a cell by a certain distance. Distance is multiplication of tile size.
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="direction"></param>
    /// <returns></returns>
    public Cell Neighbor(Cell cell, SpatialOrientation direction, int distance = 1)
    {
        switch (direction)
        {
            case SpatialOrientation.Up:
                return this[cell.Position.x + (4 * distance), cell.Position.y, cell.Position.z];
            case SpatialOrientation.Right:
                return this[cell.Position.x, cell.Position.y, cell.Position.z - (4 * distance)];
            case SpatialOrientation.Down:
                return this[cell.Position.x - (4 * distance), cell.Position.y, cell.Position.z];
            case SpatialOrientation.Left:
                return this[cell.Position.x, cell.Position.y, cell.Position.z + (4 * distance)];
            case SpatialOrientation.UpRight:
                return this[cell.Position.x + (4 * distance), cell.Position.y, cell.Position.z - (4 * distance)];
            case SpatialOrientation.UpLeft:
                return this[cell.Position.x + (4 * distance), cell.Position.y, cell.Position.z + (4 * distance)];
            case SpatialOrientation.DownRight:
                return this[cell.Position.x - (4 * distance), cell.Position.y, cell.Position.z - (4 * distance)];
            case SpatialOrientation.DownLeft:
                return this[cell.Position.x - (4 * distance), cell.Position.y, cell.Position.z + (4 * distance)];
            default:
                throw new System.NotSupportedException();
        }
    }

    /// <summary>
    /// Returns the up,right,down,and left neighbors of a cell by a certain distance. Distance is multiplication of tile size.
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    public List<Cell> DirectNeighbors(Cell cell, int distance = 1)
    {
        List<Cell> neighbors = new List<Cell>();
        neighbors.Add(Neighbor(cell, SpatialOrientation.Up, distance));
        neighbors.Add(Neighbor(cell, SpatialOrientation.Right, distance));
        neighbors.Add(Neighbor(cell, SpatialOrientation.Down, distance));
        neighbors.Add(Neighbor(cell, SpatialOrientation.Left, distance));

        return neighbors;
    }

    /// <summary>
    /// Returns the up,right,down,and left neighbors of a cell by a certain distance. Distance is multiplication of tile size.
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    public CellDirectionalGroup AllNeighbors(Cell cell, int distance = 1)
    {
        List<Cell> neighbors = new List<Cell>();
        neighbors.Add(Neighbor(cell, SpatialOrientation.Up, distance));
        neighbors.Add(Neighbor(cell, SpatialOrientation.Right, distance));
        neighbors.Add(Neighbor(cell, SpatialOrientation.Down, distance));
        neighbors.Add(Neighbor(cell, SpatialOrientation.Left, distance));
        neighbors.Add(Neighbor(cell, SpatialOrientation.UpRight, distance));
        neighbors.Add(Neighbor(cell, SpatialOrientation.UpLeft, distance));
        neighbors.Add(Neighbor(cell, SpatialOrientation.DownRight, distance));
        neighbors.Add(Neighbor(cell, SpatialOrientation.DownLeft, distance));

        return new CellDirectionalGroup(neighbors);
    }

    /// <summary>
    /// Find the closest cell to a position.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public Cell Find(Vector3 position, Vector3 tolerance)
    {
        if (tolerance == null) tolerance = new Vector3(0.1f, 0.1f, 0.1f);

        float tileSize = 4f;

        foreach (Cell cell in Cells)
        {
            // Calculate the half size of the tile
            float halfTileSize = tileSize / 2;

            // Check if the position is within the bounds of the tile, considering a small tolerance
            if (Mathf.Abs(cell.Position.x - position.x) < halfTileSize + tolerance.x &&
                Mathf.Abs(cell.Position.y - position.y) < halfTileSize + tolerance.y &&
                Mathf.Abs(cell.Position.z - position.z) < halfTileSize + tolerance.z)
            {
                return cell;
            }
        }

        return Cell(position.RoundToInt());
    }

    /// <summary>
    /// Find a child cell by a certain position with an offset. The default offset is 2 as most tiles have a offset of 2.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="position"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public Transform FindChildByPosition(Transform parent, Vector3 position, int offset = 2)
    {
        foreach (Transform child in parent)
        {
            Vector3 roundedChildPosition = child.position.RoundToInt();

            // Check if the child's position is within the offset range
            if (Mathf.Abs(roundedChildPosition.x - position.x) <= offset &&
                Mathf.Abs(roundedChildPosition.y - position.y) <= offset &&
                Mathf.Abs(roundedChildPosition.z - position.z) <= offset)
            {
                return child;
            }
        }
        return null;
    }

    /// <summary>
    /// Find the closest cell of a certain type by the current cell.
    /// </summary>
    /// <param name="currentCell"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public Cell FindClosest(Cell currentCell, CellType type, bool requireSameRoom = true)
    {
        Cell closestCell = null;
        int closestDistance = int.MaxValue;

        foreach (Cell cell in Cells)
        {
            if (cell.Type != type || requireSameRoom && cell.Room != currentCell.Room) continue;

            // Check if this tile is farther than our recorded cell.
            int distance = CalculateDistance(cell.Position, cell.Position);
            if (distance > closestDistance) continue;

            // We found it!
            closestCell = cell;
        }

        return closestCell;
    }










    /// <summary>
    /// Calculate the distance between two <see cref="Vector3Int"/>.
    /// </summary>
    /// <param name="C1"></param>
    /// <param name="C2"></param>
    /// <returns></returns>
    private static int CalculateDistance(Vector3Int C1, Vector3Int C2)
    {
        // Calculate the absolute difference in x, y, and z coordinates (Manhattan distance)
        int x = Mathf.Abs(C1.x - C2.x);
        int y = Mathf.Abs(C1.y - C2.y);
        int z = Mathf.Abs(C1.z - C2.z);

        return x + y + z;
    }


    public Cell Add(Vector3Int pos, CellType type, RoomMono room = null)
    {
        Cell newCell = this[pos];
        newCell.Type = type;
        newCell.Room = room;

        this.Cells.Add(newCell);

        return newCell;
    }

    public bool Remove(Vector3Int pos)
    {
        Cell cell = this[pos];
        return this.Cells.Remove(cell);
    }


    /// <summary>
    /// Add a new room bounds to the grid. The only bounds that will be added is those that contain the name "Floor".
    /// </summary>
    /// <param name="bounds"></param>
    /// <param name="position"></param>
    /// <param name="type"></param>
    public List<Cell> AddBounds(RoomMono room, CellType type)
    {
        if (room == null)
            throw new System.ArgumentException("Room is null.");

        List<Cell> newBounds = new List<Cell>();

        // Get all floor objects within the room
        Transform[] floorObjects = room.GetComponentsInChildren<Transform>(true);
        foreach (Transform floorObject in floorObjects)
        {
            if (floorObject.gameObject.layer == LayerMask.NameToLayer("Floor"))
            {
                Vector3Int currentPosition = floorObject.position.RoundToInt();
                Cell newCell = new Cell() { Type = type, Position = currentPosition + new Vector3Int(0, 2, 0), Room = room };

                Cells.Add(newCell);
                newBounds.Add(newCell);
            }
        }

        if (newBounds.Count() == 0)
            throw new ArgumentException("Failed to find floor objects.");

        // Set the room bounds.
        room.GridBounds = newBounds;

        return newBounds;
    }

    /// <summary>
    /// Add a new <see cref="Bounds"/> to the world grid.
    /// </summary>
    /// <param name="bounds">The size of the </param>
    /// <param name="position"></param>
    /// <param name="type"></param>
    public List<Cell> AddBounds(Bounds bounds, Vector3Int position, CellType type)
    {
        List<Cell> newBounds = new List<Cell>();

        // Calculate the minimum and maximum corners of the bounds in world space
        Vector3Int minCorner = (bounds.center - bounds.extents).RoundToInt();
        Vector3Int maxCorner = (bounds.center + bounds.extents).RoundToInt();

        // Loop through X,Y,Z. Each tile is 4f. Adjust the step size accordingly
        for (int x = minCorner.x + 2; x < maxCorner.x; x += 4)
        {
            for (int y = minCorner.y + 2; y < maxCorner.y; y += 4)
            {
                for (int z = minCorner.z + 2; z < maxCorner.z; z += 4)
                {
                    Vector3Int currentPosition = new Vector3Int(x, y, z);
                    Cell newCell = new Cell() { Type = type, Position = currentPosition };

                    Cells.Add(newCell);
                    newBounds.Add(newCell);
                }
            }
        }

        return newBounds;
    }

    /// <summary>
    /// Remove a set of bounds from the grid.
    /// </summary>
    /// <param name="bounds"></param>
    /// <param name="position"></param>
    public void RemoveBounds(Bounds bounds, Vector3Int position)
    {
        // Calculate the minimum and maximum corners of the bounds in world space
        Vector3Int minCorner = (bounds.center - bounds.extents).RoundToInt();
        Vector3Int maxCorner = (bounds.center + bounds.extents).RoundToInt();

        // Iterate through all positions within the bounds
        for (int x = minCorner.x; x < maxCorner.x; x++)
        {
            for (int y = minCorner.y; y < maxCorner.y; y++)
            {
                for (int z = minCorner.z; z < maxCorner.z; z++)
                {
                    Vector3Int currentPosition = new Vector3Int(Mathf.RoundToInt(x) + 2, Mathf.RoundToInt(y) + 2, Mathf.RoundToInt(z) + 2);
                    Cells.Remove(Cells.FirstOrDefault(r => r.Position == currentPosition));
                }
            }
        }
    }
}