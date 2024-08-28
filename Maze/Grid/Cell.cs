using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

/// <summary>
/// Provides information about a cell.
/// </summary>
public class Cell
{
    /// <summary>
    /// Contains information on wall visiblity for a cell.
    /// </summary>
    private bool[] WallVisibility = new bool[4]
    {
        true,true,true,true
    };

    /// <summary>
    /// The type of cell helping with some pathfinding.
    /// </summary>
    public CellType Type { get; set; }

    /// <summary>
    /// Position of the cell.
    /// </summary>
    public Vector3Int Position { get; set; }

    /// <summary>
    /// Room that is associated with this cell, if any.
    /// </summary>
    public RoomMono Room { get; set; }

    /// <summary>
    /// A* Pathfinding. Helps with walking back in the path after reaching a destination.
    /// </summary>
    public Cell Parent { get; set; }

    /// <summary>
    /// A* Pathfinding. The cost from the start node to the current node.
    /// </summary>
    public int G { get; set; } = 0;

    /// <summary>
    /// A* Pathfinding. The heuristic estimate from the cost from the current node to this cell.
    /// </summary>
    public int H { get; set; } = 0;

    /// <summary>
    /// A* Pathfinding. The total estimated cost of the this cell.
    /// </summary>
    public int F => G + H;

    /// <summary>
    /// A* Pathfinding. Used for rebuilding a stairway.
    /// </summary>
    public HashSet<Vector3Int> PreviousSet { get; set; } = new HashSet<Vector3Int>();

    /// <summary>
    /// A* Pathfinding. The default cost of going to this cell.
    /// </summary>
    public int Cost { get; set; } = 1;

    /// <summary>
    /// Check if a <see cref="Cell"/> is a valid position for A* pathfinding to reach.
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="controller"></param>
    /// <param name="current"></param>
    /// <param name="direction"></param>
    /// <returns></returns>
    public bool IsValidMove(MazeGrid grid, MazeController controller, SpatialOrientation direction)
    {
        Cell neighbor = grid.Neighbor(this, direction);

        if (neighbor == null)
            return false;

        if (neighbor.Type == CellType.None)
            return false;

        // Check if both cells belong to a door and if the door is locked.
        if (Type == CellType.Door && neighbor.Type == CellType.Door)
        {
            // Retrieve the door.
            GameObject doorGo = controller.DoorRegistry.Get(Position, 4f);

            // DoorMono is stored inside a child object. Check if it's locked.
            if (doorGo.GetComponentInChildren<DoorMono>().CurrentState == DoorState.Locked)
            {
                return false;
            }
        }

        // Common directions.
        if (direction == SpatialOrientation.Left && neighbor.IsWallVisible(SpatialOrientation.Right)
            || direction == SpatialOrientation.Right && neighbor.IsWallVisible(SpatialOrientation.Left)
            || direction == SpatialOrientation.Up && neighbor.IsWallVisible(SpatialOrientation.Down)
            || direction == SpatialOrientation.Down && neighbor.IsWallVisible(SpatialOrientation.Up))
            return false;

        return true;
    }

    /// <summary>
    /// Check if a <see cref="Cell"/> is valid position for a <see cref="HallwayMono"/> to spawn in A+ pathfinding.
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="controller"></param>
    /// <param name="direction"></param>
    /// <returns></returns>
    public bool IsValidHallwaySpawn(MazeGrid grid, MazeController controller, SpatialOrientation direction)
    {
        Cell neighbor = grid.Neighbor(this, direction);

        if (neighbor == null)
            return false;

        List<CellType> validTypes = new List<CellType>()
        {
            CellType.None,
            CellType.Hallway
        };

        if (!validTypes.Contains(neighbor.Type))
            return false;

        // Make sure the stairway is going our direction.
        if (neighbor.Type == CellType.Stairway)
        {
            return false;

            // Common directions.
            if (direction == SpatialOrientation.Left && neighbor.IsWallVisible(SpatialOrientation.Right)
                || direction == SpatialOrientation.Right && neighbor.IsWallVisible(SpatialOrientation.Left)
                || direction == SpatialOrientation.Up && neighbor.IsWallVisible(SpatialOrientation.Down)
                || direction == SpatialOrientation.Down && neighbor.IsWallVisible(SpatialOrientation.Up))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Check if a <see cref="Cell"/> along with its neighbors is a valid position for a <see cref="HallwayStairMap"/> to spawn in A+ pathfinding.
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="controller"></param>
    /// <param name="direction"></param>
    /// <param name="IsUp"></param>
    /// <returns></returns>
    public List<Cell> IsValidStairway(MazeGrid grid, MazeController controller, Cell lookingAt, Cell dest)
    {
        List<Cell> stairwayCells = new List<Cell>();

        // Calculate the direction vector from curr to dest
        SpatialOrientation direction = DistanceHelper.GetSpatialOrientation(this, lookingAt);
        if (direction == SpatialOrientation.None) return null;

        // The vertical difference between the current and destination cells
        int positiveY = DistanceHelper.IsPositiveDirection(this.Position.y, dest.Position.y) ? 4 : -4;

        // Start cell needs to be a hallway.
        Cell entrance = this;//grid.Neighbor(this, direction);

        // Grab the next two cells that are forward from the entrance.
        Cell step1 = grid.Neighbor(entrance, direction);
        Cell step2 = grid.Neighbor(step1, direction);

        // Grab the next two cells that are up/below the first two.
        Cell step3 = grid[new Vector3Int(step1.Position.x, step1.Position.y + positiveY, step1.Position.z)];
        Cell step4 = grid[new Vector3Int(step2.Position.x, step2.Position.y + positiveY, step2.Position.z)];

        // Finally, the destination needs to be a hallway too.
        Cell exit = grid.Neighbor(step4, direction);

        if (entrance.Type != CellType.Hallway && entrance.Type != CellType.None && entrance.Type != CellType.Door)
            return null;

        if (exit.Type != CellType.Hallway && exit.Type != CellType.None)
            return null;

        if (step1.Type != CellType.None || step2.Type != CellType.None || step3.Type != CellType.None || step4.Type != CellType.None)
            return null;

        stairwayCells.Add(entrance);
        stairwayCells.Add(step1);
        stairwayCells.Add(step2);
        stairwayCells.Add(step3);
        stairwayCells.Add(step4);
        stairwayCells.Add(exit);

        return stairwayCells;
    }

    /// <summary>
    /// Retrieve a list of valid neighbors with this cell.
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="controller"></param>
    /// <returns></returns>
    public List<Cell> GetValidNeighbors(MazeGrid grid, MazeController controller)
    {
        var neighbors = grid.Neighbors(this);
        var acceptedNeighbors = new List<Cell>();

        if (IsValidMove(grid, controller, SpatialOrientation.Up))
        {
            acceptedNeighbors.Add(neighbors.Up);
        }
        if (IsValidMove(grid, controller, SpatialOrientation.Down))
        {
            acceptedNeighbors.Add(neighbors.Down);
        }
        if (IsValidMove(grid, controller, SpatialOrientation.Left))
        {
            acceptedNeighbors.Add(neighbors.Left);
        }
        if (IsValidMove(grid, controller, SpatialOrientation.Right))
        {
            acceptedNeighbors.Add(neighbors.Right);
        }
        if (IsValidMove(grid, controller, SpatialOrientation.UpStairsLeft))
        {
            acceptedNeighbors.Add(neighbors.UpStairsLeft);
        }
        if (IsValidMove(grid, controller, SpatialOrientation.UpStairsRight))
        {
            acceptedNeighbors.Add(neighbors.UpStairsRight);
        }
        if (IsValidMove(grid, controller, SpatialOrientation.UpStairsUp))
        {
            acceptedNeighbors.Add(neighbors.UpStairsUp);
        }
        if (IsValidMove(grid, controller, SpatialOrientation.UpStairsDown))
        {
            acceptedNeighbors.Add(neighbors.UpStairsDown);
        }
        if (IsValidMove(grid, controller, SpatialOrientation.DownStairsLeft))
        {
            acceptedNeighbors.Add(neighbors.DownStairsLeft);
        }
        if (IsValidMove(grid, controller, SpatialOrientation.DownStairsRight))
        {
            acceptedNeighbors.Add(neighbors.DownStairsRight);
        }
        if (IsValidMove(grid, controller, SpatialOrientation.DownStairsUp))
        {
            acceptedNeighbors.Add(neighbors.DownStairsUp);
        }
        if (IsValidMove(grid, controller, SpatialOrientation.DownStairsDown))
        {
            acceptedNeighbors.Add(neighbors.DownStairsDown);
        }

        return acceptedNeighbors;
    }

    /// <summary>
    /// Retrieve a list of valid neighbors for hallway spawns.
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="controller"></param>
    /// <returns></returns>
    public List<Cell> GetValidHallwayNeighbors(MazeGrid grid, MazeController controller)
    {
        var neighbors = grid.Neighbors(this);
        var acceptedNeighbors = new List<Cell>();

        if (IsValidHallwaySpawn(grid, controller, SpatialOrientation.Up))
        {
            acceptedNeighbors.Add(neighbors.Up);
        }
        if (IsValidHallwaySpawn(grid, controller, SpatialOrientation.Down))
        {
            acceptedNeighbors.Add(neighbors.Down);
        }
        if (IsValidHallwaySpawn(grid, controller, SpatialOrientation.Left))
        {
            acceptedNeighbors.Add(neighbors.Left);
        }
        if (IsValidHallwaySpawn(grid, controller, SpatialOrientation.Right))
        {
            acceptedNeighbors.Add(neighbors.Right);
        }

        return acceptedNeighbors;
    }

    /// <summary>
    /// Returns whether a wall part of this cell is visible.
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public bool IsWallVisible(SpatialOrientation direction)
    {
        switch (direction)
        {
            case SpatialOrientation.Left:
                return WallVisibility[0];
            case SpatialOrientation.Up:
                return WallVisibility[1];
            case SpatialOrientation.Right:
                return WallVisibility[2];
            case SpatialOrientation.Down:
                return WallVisibility[3];
            default:
                throw new NotSupportedException($"Direction {direction} is not supported.");
        }
    }

    /// <summary>
    /// Set whether a wall part of this cell is visible.
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="newValue"></param>
    public void SetWallVisibility(SpatialOrientation direction, bool newValue)
    {
        switch (direction)
        {
            case SpatialOrientation.Left:
                WallVisibility[0] = newValue;
                break;
            case SpatialOrientation.Up:
                WallVisibility[1] = newValue;
                break;
            case SpatialOrientation.Right:
                WallVisibility[2] = newValue;
                break;
            case SpatialOrientation.Down:
                WallVisibility[3] = newValue;
                break;
            default:
                throw new NotSupportedException($"Direction {direction} is not supported.");
        }
    }

    /// <summary>
    /// A* Pathfinding. Used to help with determining if we have reached our chosen cell.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        Cell other = (Cell)obj;
        return Position.Equals(other.Position);
    }

    /// <summary>
    /// Returns a hashcode of the <see cref="Position"/>
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        return Position.GetHashCode();
    }
}
