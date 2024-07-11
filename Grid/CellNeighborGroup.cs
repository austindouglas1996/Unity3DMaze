using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;


/// <summary>
/// A simple container to help with grabbing neighbor cell positions from a <see cref="MazeGrid"/> instance
/// without overcomplicating the process. This instance does not keep a copy of each cells neighbors and instead
/// will grab their current state on load.
/// </summary>
public class CellNeighborGroup : IEnumerable<Cell>
{
    /// <summary>
    /// The grid this maze is involved with.
    /// </summary>
    private readonly MazeGrid m_Grid;

    /// <summary>
    /// The cell to serve as the starting position.
    /// </summary>
    private readonly Cell cell;

    /// <summary>
    /// Distance to go away from the root cell.
    /// </summary>
    private readonly int distance;

    /// <summary>
    /// Initalize the <see cref="CellNeighborGroup"/> class.
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="rootCell"></param>
    public CellNeighborGroup(MazeGrid grid, Vector3Int rootCell, int dist)
        : this(grid, grid[rootCell], dist)
    {
    }

    /// <summary>
    /// Initialize the <see cref="CellNeighborGroup"/> class.
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="cell"></param>
    public CellNeighborGroup(MazeGrid grid, Cell cell, int dist)
    {
        this.m_Grid= grid;
        this.cell = cell;
        this.distance = dist;
    }

    /// <summary>
    /// The cell that is chosen for the neighbors.
    /// </summary>
    public Cell RootCell { get => cell; }

    /// <summary>
    /// Gets the distance set from the root cell.
    /// </summary>
    public int Distance { get => distance; }

    /// <summary>
    /// Simple directions.
    /// </summary>
    public Cell Up { get => m_Grid.Neighbor(cell, SpatialOrientation.Up, distance); }
    public Cell Right { get => m_Grid.Neighbor(cell, SpatialOrientation.Right, distance); }
    public Cell Down { get => m_Grid.Neighbor(cell, SpatialOrientation.Down, distance); }
    public Cell Left { get => m_Grid.Neighbor(cell, SpatialOrientation.Left, distance); }

    /// <summary>
    /// A bit abstract directions.
    /// </summary>
    public Cell UpRight { get => m_Grid.Neighbor(cell, SpatialOrientation.UpRight, distance); }
    public Cell UpLeft { get => m_Grid.Neighbor(cell, SpatialOrientation.UpLeft, distance); }
    public Cell DownRight { get => m_Grid.Neighbor(cell, SpatialOrientation.DownRight, distance); }
    public Cell DownLeft { get => m_Grid.Neighbor(cell, SpatialOrientation.DownLeft, distance); }

    public IEnumerator<Cell> GetEnumerator()
    {
        return GetNeighbors().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private IEnumerable<Cell> GetNeighbors()
    {
        yield return Up;
        yield return Right;
        yield return Down;
        yield return Left;
        //yield return UpRight;
        //yield return UpLeft;
        //yield return DownRight;
        //yield return DownLeft;
    }
}