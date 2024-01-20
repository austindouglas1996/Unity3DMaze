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
    /// Helps so certain functions can access the maze generator instance.
    /// </summary>
    private MazeController Maze;

    /// <summary>
    /// Initializes a new instance of the <see cref="MazeGrid"/> class.
    /// </summary>
    /// <param name="mazeGenerator"></param>
    public MazeGrid(MazeController mazeGenerator)
    {
        this.Maze = mazeGenerator;
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
    public List<Cell> Neighbors(Cell cell, int distance = 1)
    {
        List<Cell> neighbors = new List<Cell>();
        neighbors.Add(Neighbor(cell, SpatialOrientation.Up, distance));
        neighbors.Add(Neighbor(cell, SpatialOrientation.Right, distance));
        neighbors.Add(Neighbor(cell, SpatialOrientation.Down, distance));
        neighbors.Add(Neighbor(cell, SpatialOrientation.Left, distance));
        return neighbors;
    }

    /// <summary>
    /// Find the closest cell to a position.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public Cell Find(Vector3 position, int offset = 4)
    {
        foreach (Cell cell in Cells)
        {
            // Check if the child's position is within the offset range
            if (Mathf.Abs(cell.Position.x - position.x) <= offset &&
                Mathf.Abs(cell.Position.y - position.y) <= offset &&
                Mathf.Abs(cell.Position.z - position.z) <= offset)
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
    /// Path find the way across cells that may include different rooms.
    /// </summary>
    /// <param name="destPosition"></param>
    /// <param name="currPosition"></param>
    /// <param name="bufferDistanceInTiles"></param>
    /// <returns></returns>
    public Stack<Cell> PathFindRooms(Vector3Int destPosition, Vector3Int currPosition, int bufferDistanceInTiles = 15)
    {
        Stack<Cell> cells = new Stack<Cell>();

        // Current and destination spot.
        Cell currentCell = this[destPosition];
        Cell destinationCell = this[currPosition];

        // Determine whether we're travelling across rooms.
        if (currentCell.Room != destinationCell.Room)
        {
            var roomPaths = this.Maze.DoorRegistry.FindBestPath(currentCell.Room, destinationCell.Room);
            if (roomPaths == null)
                return null;

            // Go through the rooms recording positions from door to door.
            Vector3Int externalDestPosition = Vector3Int.one;
            Vector3Int externalCurrPosition = currPosition;
            foreach (Tuple<RoomMono, GameObject> entries in roomPaths)
            {
                // Set destination to room B door.
                externalDestPosition = Find(entries.Item2.transform.position.RoundToInt()).Position;

                // Grab the path for that room and then push into the current collection.
                Stack<Cell> stopCells = PathFindSameRoom(externalDestPosition, externalCurrPosition, bufferDistanceInTiles);
                foreach (Cell cell in stopCells)
                    cells.Push(cell);

                // Change current position to destination.
                externalCurrPosition = externalDestPosition;
            }

            // Change our current position to the last destination as we have reached this location from our past foreach.
            currPosition = externalDestPosition;

            // Now we're in the same room so let's push positions.
            Stack<Cell> sameRoomCells = PathFindSameRoom(destPosition, currPosition, bufferDistanceInTiles);
            if (sameRoomCells == null)
                return null;

            foreach (Cell cell in sameRoomCells)
                cells.Push(cell);

            // Reverse the stack.
            Stack<Cell> tempCell = new Stack<Cell>();
            while (cells.Count != 0)
                tempCell.Push(cells.Pop());

            cells = tempCell;
        }
        else
        {
            cells = PathFindSameRoom(destPosition, currPosition, bufferDistanceInTiles);
        }


        return cells;
    }

    /// <summary>
    /// Path find through cells on how to reach a destination.
    /// </summary>
    /// <param name="destPosition"></param>
    /// <param name="currPosition"></param>
    /// <returns></returns>
    /// <remarks>This function assumes visiting CellType.None is not allowed.</remarks>
    public Stack<Cell> PathFindSameRoom(Vector3Int destPosition, Vector3Int currPosition, int bufferDistanceInTiles = 15)
    {
        Stack<Cell> cells = new Stack<Cell>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

        // Current and destination spot.
        Cell currentCell = this[destPosition];
        Cell destinationCell = this[currPosition];

        // Do not allow us visit empty tiles.
        if (currentCell.Type == CellType.None || destinationCell.Type == CellType.None)
            throw new System.ArgumentException("Start or destination type has no tile.");

        // Do not allow cross room crossing.
        //if (currentCell.Room != destinationCell.Room && destinationCell.Type != CellType.Door || currentCell.Type != CellType.Door)
            //throw new System.NotSupportedException("Both cells go to different rooms. Cross room travelling not allowed.");

        // Set the buffer distance. (Distance / CellSize) * CellSize
        bufferDistanceInTiles = (CalculateDistance(destPosition, currPosition) / 4) * 4;

        while (currentCell != destinationCell && bufferDistanceInTiles > 0)
        {
            cells.Push(currentCell);
            visited.Add(currentCell.Position);

            // Get the next cell.
            Cell nextCell = FindBestPossibleNextCell(currentCell, destinationCell, visited);

            if (nextCell == null)
            {
                // No path can be located to the current destination.
                return null;
            }

            currentCell = nextCell;
            bufferDistanceInTiles--;
        }

        return cells;
    }

    /// <summary>
    /// Add a new room bounds to the grid. The only bounds that will be added is those that contain the name "Floor".
    /// </summary>
    /// <param name="bounds"></param>
    /// <param name="position"></param>
    /// <param name="type"></param>
    public List<Cell> AddBounds(RoomMono room, Bounds bounds, Vector3Int position, CellType type)
    {
        if (room == null)
            throw new System.ArgumentException("Room is null.");

        List<Cell> newBounds = new List<Cell>();

        // Calculate the minimum and maximum corners of the bounds in world space
        Vector3Int minCorner = (bounds.center - bounds.extents).RoundToInt();
        Vector3Int maxCorner = (bounds.center + bounds.extents).RoundToInt();

        // Iterate through all positions within the bounds
        for (int x = minCorner.x; x < maxCorner.x; x += 4)
        {
            for (int y = minCorner.y; y < maxCorner.y; y += 4)
            {
                for (int z = minCorner.z; z < maxCorner.z; z += 4)
                {
                    Vector3Int currentPosition = new Vector3Int(Mathf.RoundToInt(x) + 2, Mathf.RoundToInt(y) + 2, Mathf.RoundToInt(z) + 2);
                    Transform foundChild = FindChildByPosition(room.transform, currentPosition);

                    if (foundChild != null && foundChild.name.Contains("Floor"))
                    {
                        Cell newCell = new Cell() { Type = type, Position = currentPosition, Room = room };

                        Cells.Add(newCell);
                        newBounds.Add(newCell);
                    }
                }
            }
        }

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

        // Loop through X,Y,Z. Each tile is 4f. Each prefab has an offset of 2.
        for (int x = minCorner.x; x < maxCorner.x; x += 4)
        {
            for (int y = minCorner.y; y < maxCorner.y; y += 4)
            {
                for (int z = minCorner.z; z < maxCorner.z; z += 4)
                {
                    Vector3Int currentPosition = new Vector3Int(Mathf.RoundToInt(x) + 2, Mathf.RoundToInt(y) + 2, Mathf.RoundToInt(z) + 2);
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

    /// <summary>
    /// Helper method for <see cref="PathFindSameRoom(Vector3Int, Vector3Int, int)"/> for finding the next cell.
    /// </summary>
    /// <param name="currentCell"></param>
    /// <param name="destinationCell"></param>
    /// <param name="visited"></param>
    /// <returns></returns>
    private Cell FindBestPossibleNextCell(Cell currentCell, Cell destinationCell, HashSet<Vector3Int> visited)
    {
        // Grab the neighbors of the current cell.
        List<Cell> adjacentCells = Neighbors(currentCell);

        // Find the closest unvisited adjacent cell to the destination
        Cell closestCell = null;
        int closestDistance = int.MaxValue;

        foreach (Cell cell in adjacentCells)
        {
            // Don't allow visited cells, or empty cells.
            if (visited.Contains(cell.Position) || cell.Type == CellType.None) continue;

            // Check if this tile is farther than our recorded cell.
            int distance = CalculateDistance(cell.Position, destinationCell.Position);
            if (distance > closestDistance) continue;

            closestCell = cell;
            closestDistance = distance;
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
}