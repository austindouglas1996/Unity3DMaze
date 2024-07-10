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
    /// Returns an instance of <see cref="CellNeighborGroup"/> that includes neighbor cells.
    /// </summary>
    /// <param name="cellPos"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    public CellNeighborGroup Neighbors(Vector3Int cellPos, int distance = 1)
    {
        return this.Neighbors(this[cellPos], distance);
    }

    /// <summary>
    /// Returns an instance of <see cref="CellNeighborGroup"/> that includes neighbor cells.
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    public CellNeighborGroup Neighbors(Cell cell, int distance = 1)
    {
        return new CellNeighborGroup(this, cell, distance);
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



    public Cell Add(Vector3Int pos, CellType type, RoomMono room = null)
    {
        Cell cell = this.Cells.FirstOrDefault(cell => cell.Position == pos);

        if (cell == null)
        {
            cell = new Cell(); 
            this.Cells.Add(cell);
        }

        cell.Position = pos;
        cell.Type = type;
        cell.Room = room;

        return cell;
    }

    public void Clear()
    {
        this.Cells.Clear();
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
                Cell newCell = this.Add(currentPosition + new Vector3Int(0, 2, 0), type, room);
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