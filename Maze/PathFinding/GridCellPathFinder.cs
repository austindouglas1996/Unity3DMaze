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
                cellPath = ReconstructPath(currentCell).Item1;
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

    /// <summary>
    /// Use A* pathfinding to find a way to create hallways from one position to another. Hallways are different from <see cref="FindPath(Cell, Cell)"/>. 
    /// Hallways look for empty space, or already existing hallway cells.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public Tuple<List<Cell>, List<Cell>> FindHallwayPath(Cell start, Cell end)
    {
        var openList = new PriorityQueue<Cell>(Comparer<Cell>.Create((a, b) => a.F.CompareTo(b.F)));
        var closedList = new HashSet<Vector3Int>();
        var allNodes = new Dictionary<Vector3Int, Cell>();

        Cell currentCell = null;
        Tuple<List<Cell>, List<Cell>> cellPath = null;

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

            foreach (var neighbor in this.Grid.Neighbors(currentCell))
            {
                if (!IsInBounds(neighbor.Position))
                    continue;

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

                // Find if this is a valid path.
                var pathCost = currentCell.IsValidHallway(Grid, Controller, neighbor, end);
                if (!pathCost.IsTraversable) continue;

                // Check if the previous set contains one of these cells.
                if (pathCost.IsStairs)
                {
                    if (pathCost.StairCells.Any(cell => currentCell.PreviousSet.Contains(cell.Position) && cell != currentCell)) 
                        continue;
                }

                // The current cost to access this cell
                var tentativeG = currentCell.G + GetMovementCost(neighbor);

                if (tentativeG < neighbor.G)
                {
                    if (pathCost.IsStairs)
                    {
                        neighborCell = pathCost.StairCells.Last();
                        neighborCell.StairCells.AddRange(pathCost.StairCells);
                    }

                    neighborCell.Parent = currentCell;
                    neighborCell.G = tentativeG;
                    neighborCell.H = Heuristic(neighborCell, end);

                    neighborCell.PreviousSet.Clear();
                    neighborCell.PreviousSet.UnionWith(currentCell.PreviousSet);
                    neighborCell.PreviousSet.Add(currentCell.Position);

                    if (pathCost.IsStairs)
                    {
                        foreach (var stair in pathCost.StairCells)
                            neighborCell.PreviousSet.Add(stair.Position);
                    }

                    if (openList.Contains(neighborCell))
                        openList.UpdatePriority(neighborCell);
                    else
                        openList.Enqueue(neighborCell);
                }
            }
        }

        ResetNodes(allNodes);
        return cellPath;
    }

    /// <summary>
    /// Reconstructs the path to the original cell. Stairway cells, if required are included in their own list 
    /// for reconstruction later. 
    /// </summary>
    /// <param name="endCell">The destination cell.</param>
    /// <returns>A list of cells to reach the destination along with a list of cells to reconstruct stairs.</returns>
    private Tuple<List<Cell>, List<Cell>> ReconstructPath(Cell endCell)
    {
        var path = new List<Cell>();
        var stairs = new List<Cell>();
        var currentCell = endCell;

        while (currentCell != null)
        {
            if (currentCell.StairCells.Count != 0)
            {
                // We don't want to include the entrance or exit. 
                Cell entrance = currentCell.StairCells.First();
                Cell exit = currentCell.StairCells.Last();

                currentCell.StairCells.Remove(entrance);
                currentCell.StairCells.Remove(exit);

                stairs.Add(entrance);

                foreach (var stair in currentCell.StairCells)
                    stairs.Add(stair);

                stairs.Add(exit);
            }

            path.Add(currentCell);
            currentCell = currentCell.Parent;
        }

        path.Reverse();
        return new Tuple<List<Cell>, List<Cell>>(path, stairs);
    }

    /// <summary>
    /// Returns the base cost of a cell.
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    private int GetMovementCost(Cell cell)
    {
        switch (cell.Type)
        {
            case CellType.Hallway:
                return 5; // Very cheap to move through hallways
            case CellType.Stairway:
                return 20; // Very cheap to move through hallways
            case CellType.None:
                return 10; // More expensive to move through empty spaces
            default:
                return 15; // Default cost for other cell types
        }
    }

    /// <summary>
    /// Returns whether a position is within the grid bounds.
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public bool IsInBounds(Vector3Int position)
    {
        return position.x >= -150 && position.x < 150 &&
               position.y >= -150 && position.y < 150 &&
               position.z >= -150 && position.z < 150;
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
            node.StairCells?.Clear();
        }
    }
}
