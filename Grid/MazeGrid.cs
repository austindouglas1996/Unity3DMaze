using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Retrieve a <see cref="Cell"/> at a certain position.
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public Cell Get(Vector3Int pos)
    {
        return this[pos];
    }

    /// <summary>
    /// Sets a cells position with the type and associated room if any.
    /// </summary>
    /// <param name="pos">Position of the cell.</param>
    /// <param name="type">The type of item contained in this cell.</param>
    /// <param name="room">The associated room if any.</param>
    /// <returns>The newly created cell, or modified cell if the cell already exists.</returns>
    public Cell Set(Vector3Int pos, CellType type, RoomMono room = null)
    {
        // Check if the cell already exists.
        Cell cell = this.Cells.FirstOrDefault(cell => cell.Position == pos);

        // Cell does not exist, create it.
        if (cell == null)
        {
            cell = new Cell();
            this.Cells.Add(cell);
        }

        // Modify properties.
        cell.Position = pos;
        cell.Type = type;
        cell.Room = room;

        return cell;
    }

    /// <summary>
    /// Set a collection of cells type based on a <see cref="Bounds"/> object.
    /// </summary>
    /// <param name="bounds">The size of the </param>
    /// <param name="position"></param>
    /// <param name="type"></param>
    public List<Cell> SetBounds(Bounds bounds, Vector3Int position, CellType type)
    {
        var cells = this.GetBoundsCells(bounds, position);
        foreach (Cell cell in cells)
        {
            cell.Type = type;
        }

        return cells;
    }

    /// <summary>
    /// Loops through a <see cref="RoomMono"/> children to find items on the <see cref="Floor"/> layermask.
    /// Adding them as a grid cell. This function is good for 1-story rooms. If you're trying to map a 
    /// multi-story room <see cref="SetBounds"/>.
    /// </summary>
    /// <param name="room">The room we should set the bounds for.</param>
    /// <param name="type">The type of cell contained. By default set to Room.</param>
    public List<Cell> SetRoomCells(RoomMono room, CellType type = CellType.Room)
    {
        if (room == null)
            throw new System.ArgumentException("Room is null.");

        List<Cell> newBounds = new List<Cell>();

        List<Transform> wallObjects = new List<Transform>();
        Transform[] AllObjects = room.GetComponentsInChildren<Transform>(true);
        foreach (Transform obj in AllObjects)
        {
            if (obj.gameObject.layer == LayerMask.NameToLayer("Wall") || obj.gameObject.layer == LayerMask.NameToLayer("Door"))
            {
                wallObjects.Add(obj);
            }
        }

        // Get all floor objects within the room
        Transform[] floorObjects = room.GetComponentsInChildren<Transform>(true);
        foreach (Transform floorObject in floorObjects)
        {
            if (floorObject.gameObject.layer == LayerMask.NameToLayer("Floor"))
            {
                Vector3Int currentPosition = floorObject.position.RoundToInt();
                Cell newCell = this.Set(currentPosition + new Vector3Int(0, 2, 0), type, room);
                newBounds.Add(newCell);

                newCell.SetWallVisibility(SpatialOrientation.Up, false);
                newCell.SetWallVisibility(SpatialOrientation.Down, false);
                newCell.SetWallVisibility(SpatialOrientation.Right, false);
                newCell.SetWallVisibility(SpatialOrientation.Left, false);

                foreach (Transform wallObject in wallObjects)
                {
                    Vector3 direction = (wallObject.position - floorObject.position);
                    if (Math.Abs(direction.x) > 2 || Math.Abs(direction.y) > 2 || Math.Abs(direction.z) > 2)
                        continue;

                    RoomFixtureMono wallRf = wallObject.GetComponent<RoomFixtureMono>();

                    // This is a door.
                    if (wallObject.gameObject.layer == 6)
                    {
                        continue;
                    }

                    if (direction.x == 2)
                    {
                        newCell.SetWallVisibility(SpatialOrientation.Up, true);
                    }
                    else if (direction.z == 2)
                    {
                        newCell.SetWallVisibility(SpatialOrientation.Left, true);
                    }

                    if (direction.x == -2)
                    {
                        newCell.SetWallVisibility(SpatialOrientation.Down, true);
                    }
                    else if (direction.z == -2)
                    {
                        newCell.SetWallVisibility(SpatialOrientation.Right, true);
                    }
                }
            }
        }

        if (newBounds.Count() == 0)
            throw new ArgumentException("Failed to find floor objects.");

        return newBounds;
    }

    /// <summary>
    /// Clear a <see cref="Cell"/> information. Resetting back to default.
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public bool Clear(Vector3Int pos)
    {
        Cell cell = this[pos];
        return this.Cells.Remove(cell);
    }

    /// <summary>
    /// Remove a set of bounds from the grid.
    /// </summary>
    /// <param name="bounds"></param>
    /// <param name="position"></param>
    public void ClearBounds(Bounds bounds, Vector3Int position)
    {
        foreach (Cell cell in GetBoundsCells(bounds, position))
        {
            this.Clear(cell.Position);
        }
    }

    /// <summary>
    /// Clear all cells based in this grid back to default settings.
    /// </summary>
    public void ClearAll()
    {
        this.Cells.Clear();
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

    /// <summary>
    /// Checks whether a <see cref="Cell"/> is set to <see cref="CellType.None"/> for a new cell to be placed.
    /// </summary>
    /// <param name="cellPos"></param>
    /// <returns></returns>
    public bool IsValid(Vector3Int cellPos)
    {
        return this.IsValid(this[cellPos]);
    }

    /// <summary>
    /// Checks whether a <see cref="Cell"/> is set to <see cref="CellType.None"/> for a new cell to be placed.
    /// </summary>
    /// <param name="cellPos"></param>
    /// <returns></returns>
    public bool IsValid(Cell cellPos)
    {
        return cellPos.Type == CellType.None;
    }

    /// <summary>
    /// Retrieve the appropiate cells from a <see cref="Bounds"/> object.
    /// </summary>
    /// <param name="bounds"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    private List<Cell> GetBoundsCells(Bounds bounds, Vector3Int position)
    {
        List<Cell> boundsList = new List<Cell>();

        // Calculate the minimum and maximum corners of the bounds in world space
        Vector3Int minCorner = (bounds.center - bounds.extents).RoundToInt();
        Vector3Int maxCorner = (bounds.center + bounds.extents).RoundToInt();

        // Loop through X,Y,Z. Each tile is 4f. Adjust the step size accordingly
        for (int x = minCorner.x + 2; x < maxCorner.x; x += 4)
            for (int y = minCorner.y + 2; y < maxCorner.y; y += 4)
                for (int z = minCorner.z + 2; z < maxCorner.z; z += 4)
                    boundsList.Add(this[new Vector3Int(x, y, z)]);

        return boundsList;
    }
}