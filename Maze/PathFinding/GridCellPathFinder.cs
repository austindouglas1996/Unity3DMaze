using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Uses a <see cref="MazeGrid"/> along with A* alogrithm to help determine the path between two cells throughout the maze.
/// </summary>
public class GridCellPathFinder
{
    public MazeGrid Grid;
    public MazeController Controller;

    /// <summary>
    /// Initialize an instance of <see cref="GridCellPathFinder"/>.
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="controller"></param>
    public GridCellPathFinder(MazeGrid grid, MazeController controller)
    {
        Grid = grid;
        Controller = controller;
    }

    /// <summary>
    /// Use A* pathfinding to find a way from one cell to another.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public List<Cell> FindPath(Cell start, Cell end)
    {
        var openList = new PriorityQueue<Cell>(Comparer<Cell>.Create((a, b) => a.F.CompareTo(b.F)));
        var closedList = new HashSet<Cell>();
        var allNodes = new Dictionary<Vector3Int, Cell>();

        Cell currentCell = null;
        List<Cell> cellPath = null;

        start.G = 0;
        start.H = Heuristic(start, end);
        openList.Enqueue(start);
        allNodes[start.Position] = start;

        while (openList.Count > 0)
        {
            currentCell = openList.Dequeue(); 
            closedList.Add(currentCell);

            // We found the end. Return the path.
            if (currentCell.Equals(end))
            {
                cellPath = ReconstructPath(currentCell);
                break;
            }

            // Retrieve a list of valid neighbors. This will automaticall
            // determine if the neighbor is a valid target from the current cell.
            var neighbors = currentCell.GetValidNeighbors(this.Grid, this.Controller);

            foreach (var neighbor in neighbors)
            {
                if (!allNodes.TryGetValue(neighbor.Position, out var neighborCell))
                {
                    neighborCell = neighbor;
                    allNodes[neighbor.Position] = neighborCell;
                }

                // Have we already seen this cell?
                if (closedList.Contains(neighborCell))
                    continue;

                // The current cost to access this cell.
                var tentativeG = currentCell.G + neighbor.Cost;

                if (!openList.Contains(neighborCell))
                {
                    neighbor.Parent = currentCell;
                    neighbor.G = tentativeG;
                    neighbor.H = Heuristic(neighborCell, end);
                    openList.Enqueue(neighborCell);
                }
                else if (tentativeG < neighbor.G)
                {
                    neighbor.Parent = currentCell;
                    neighbor.G = tentativeG;
                    neighbor.H = Heuristic(neighborCell, end);
                    openList.UpdatePriority(neighborCell);
                }
            }
        }

        ResetNodes(allNodes);
        return cellPath;
    }

    public bool IsInBounds(Vector3Int position)
    {
        return position.x >= -100 && position.x < 400 &&
               position.y >= -100 && position.y < 400 &&
               position.z >= -100 && position.z < 400;
    }



    public List<Cell> FindHallwayPath(Cell start, Cell end)
    {
        var openList = new PriorityQueue<Cell>(Comparer<Cell>.Create((a, b) => a.F.CompareTo(b.F)));
        var closedList = new HashSet<Vector3Int>();
        var allNodes = new Dictionary<Vector3Int, Cell>();

        Cell currentCell = null;
        List<Cell> cellPath = null;

        start.G = 0;
        start.H = Heuristic(start, end);
        start.PreviousSet = new HashSet<Vector3Int>();
        start.PreviousSet.Add(start.Position);
        openList.Enqueue(start);
        allNodes[start.Position] = start;

        while (openList.Count > 0)
        {
            currentCell = openList.Dequeue();
            closedList.Add(currentCell.Position);

            // We found the end. Return the path.
            if (currentCell.Equals(end))
            {
                cellPath = ReconstructPath(currentCell);
                break;
            }

            foreach (var neighbor in currentCell.GetValidHallwayNeighbors(this.Grid, this.Controller))
            {
                if (!allNodes.TryGetValue(neighbor.Position, out var neighborCell))
                {
                    neighborCell = neighbor;
                    allNodes[neighbor.Position] = neighborCell;
                }

                // Skip if the cell is already in the closed list
                if (closedList.Contains(neighborCell.Position))
                    continue;

                // Skip if the position is in the PreviousSet (to prevent backtracking)
                if (currentCell.PreviousSet.Contains(neighborCell.Position))
                    continue;

                // Handle stairways
                if (currentCell.Position.y != end.Position.y)
                {
                    var stairway = currentCell.IsValidStairway(Grid, Controller, neighbor, end);
                    if (stairway != null)
                    {
                        bool invalid = false;
                        foreach (var stair in stairway)
                        {
                            if (stair != currentCell && (currentCell.PreviousSet.Contains(stair.Position) || closedList.Contains(stair.Position)))
                            {
                                invalid = true;
                                break;
                            }
                        }

                        if (invalid)
                            continue;

                        // Process the entire stairway as a single step
                        Cell lastStairCell = stairway.Last();
                        if (!allNodes.TryGetValue(lastStairCell.Position, out var stairNode))
                        {
                            stairNode = lastStairCell;
                            allNodes[lastStairCell.Position] = stairNode;
                        }

                        stairNode.Parent = currentCell;
                        stairNode.G = currentCell.G + GetMovementCost(stairNode);
                        stairNode.H = Heuristic(stairNode, end);

                        // Update PreviousSet for the stairway in one go
                        stairNode.PreviousSet = new HashSet<Vector3Int>(currentCell.PreviousSet);
                        stairNode.PreviousSet.UnionWith(stairway.ConvertAll(cell => cell.Position));

                        // Add or update stairNode in the open list
                        if (!openList.Contains(stairNode))
                        {
                            openList.Enqueue(stairNode);
                        }
                        else
                        {
                            openList.UpdatePriority(stairNode);
                        }

                        continue; // Skip to the next neighbor since the stairway cells have been processed.
                    }
                }

                // The current cost to access this cell
                var tentativeG = currentCell.G + GetMovementCost(neighbor);

                if (!openList.Contains(neighborCell))
                {
                    neighborCell.Parent = currentCell;
                    neighborCell.G = tentativeG;
                    neighborCell.H = Heuristic(neighborCell, end);

                    // Inherit the PreviousSet and add the current position
                    neighborCell.PreviousSet = new HashSet<Vector3Int>(currentCell.PreviousSet);
                    neighborCell.PreviousSet.Add(neighborCell.Position);

                    openList.Enqueue(neighborCell);
                }
                else if (tentativeG < neighborCell.G)
                {
                    neighborCell.Parent = currentCell;
                    neighborCell.G = tentativeG;
                    neighborCell.H = Heuristic(neighborCell, end);

                    // Update the PreviousSet in case of a shorter path
                    neighborCell.PreviousSet = new HashSet<Vector3Int>(currentCell.PreviousSet);
                    neighborCell.PreviousSet.Add(neighborCell.Position);

                    openList.UpdatePriority(neighborCell);
                }
            }
        }

        ResetNodes(allNodes);
        return cellPath;
    }


    private List<Cell> ReconstructPath(Cell endCell)
    {
        var path = new List<Cell>();
        var currentCell = endCell;

        while (currentCell != null)
        {
            // If the current cell's PreviousSet contains more than just its own position, it might be part of a stairway
            if (currentCell.PreviousSet.Count > 1)
            {
                foreach (var position in currentCell.PreviousSet)
                {
                    Cell stairCell = Grid[position];

                    //if (path.Contains(stairCell))
                        //continue;

                    path.Add(stairCell);
                    stairCell.Type = CellType.Stairway;
                    Grid.Set(stairCell.Position, CellType.Stairway);
                }
            }

            currentCell = currentCell.Parent;
        }

        for (int i = 0; i < path.Count; i++)
        {
            MazeController.Instance.CreateDebugCube(path[i].Position, new Vector3(4, 4, 4), "Gay");
        }

        path.Reverse();
        return path;
    }




    private bool IsStairwayPartOfValidPath(List<Cell> stairwayCells, List<Cell> currentPath)
    {
        // Ensure that the stairway's entrance or exit is connected to a cell already in the path
        var entranceCell = stairwayCells.First();
        var exitCell = stairwayCells.Last();

        // The stairway is valid if its entrance or exit connects to a cell already in the path
        return currentPath.Contains(entranceCell) || currentPath.Contains(exitCell);
    }

    private void CreateStairwayDebugCubes(List<Cell> stairwayCells, int i)
    {
        if (i >= 1 && i <= 4)
        {
            MazeController.Instance.CreateDebugCube(stairwayCells[i].Position, new Vector3(4, 4, 4), "Stair");
        }
    }

    private int GetMovementCost(Cell cell)
    {
        switch (cell.Type)
        {
            case CellType.Hallway:
                return 1; // Very cheap to move through hallways
            case CellType.None:
                return 10; // More expensive to move through empty spaces
            default:
                return 5; // Default cost for other cell types
        }
    }

    /// <summary>
    /// Returns the Heuristic cost for a given set of cells.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    private int Heuristic(Cell current, Cell end)
    {
        // Manhattan distance with additional penalty for vertical movement
        int dx = Mathf.Abs(current.Position.x - end.Position.x);
        int dz = Mathf.Abs(current.Position.z - end.Position.z);
        int dy = Mathf.Abs(current.Position.y - end.Position.y);

        // Use a dynamic penalty for vertical movement
        int verticalPenalty = Mathf.Max(1, 5 - Mathf.Min(dx, dz));

        return dx + dz + (dy * verticalPenalty);
    }

    /// <summary>
    /// Reset the seen cells back to their original values.
    /// </summary>
    /// <param name="allNodes"></param>
    private void ResetNodes(Dictionary<Vector3Int, Cell> allNodes)
    {
        foreach (var node in allNodes.Values)
        {
            node.G = int.MaxValue;
            node.H = int.MaxValue;
            node.Parent = null;
            node.PreviousSet?.Clear();
        }
    }
}
