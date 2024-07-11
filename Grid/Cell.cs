using UnityEngine;

/// <summary>
/// Provides information about a cell.
/// </summary>
[System.Serializable]
public class Cell
{
    public CellType Type { get; set; }
    public Vector3Int Position { get; set; }
    public string GroupId { get; set; } = "None";
    public RoomMono Room { get; set; }
}
