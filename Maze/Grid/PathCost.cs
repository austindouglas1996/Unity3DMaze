using System.Collections.Generic;

/// <summary>
/// A helper class for the <see cref="Cell.IsValidHallway(MazeGrid, MazeController, Cell, Cell)"/> function. This class
/// will determine path costs when including hallway cells.
/// </summary>
public class PathCost
{
    public bool IsTraversable { get; set; } = false;
    public bool IsStairs { get; set; } = false;
    public List<Cell> StairCells = new List<Cell>();
}
