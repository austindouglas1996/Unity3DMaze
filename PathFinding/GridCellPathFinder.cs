using System.Collections.Generic;
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

    /// <summary>
    /// Returns the Heuristic cost for a given set of cells.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    private int Heuristic(Cell a, Cell b)
    {
        return Mathf.Abs(a.Position.x - b.Position.x) + Mathf.Abs(a.Position.y - b.Position.y);
    }

    /// <summary>
    /// Reconstruct the path from the destination cell to the starter cell.
    /// </summary>
    /// <param name="endCell"></param>
    /// <returns></returns>
    private List<Cell> ReconstructPath(Cell endCell)
    {
        var path = new List<Cell>();
        var currentCell = endCell;

        while (currentCell != null)
        {
            path.Add(currentCell);
            currentCell = currentCell.Parent;
        }

        path.Reverse();
        return path;
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
        }
    }
}
