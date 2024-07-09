using UnityEngine;

/// <summary>
/// Provides information about a cell.
/// </summary>
public class Cell : ICell
{
    public RoomMono Room { get; set; }
    public CellType Type { get; set; }
    public Vector3Int Position { get; set; }
}
