/// <summary>
/// Determines the cell of cell at this location.
/// </summary>
[System.Serializable]
public enum CellType
{
    /// <summary>
    /// Default action.
    /// </summary>
    None,
    
    /// <summary>
    /// A part of a room is in this cell.
    /// </summary>
    Room,

    /// <summary>
    /// A way to go through walls (door) is here.
    /// </summary>
    Door,

    /// <summary>
    /// A different type of room is here. 
    /// NOTE: Maybe we can get rid of this and allow hallways to be rooms?
    /// </summary>
    Hallway,

    /// <summary>
    /// This cell is contained with a stairway that travels between Y's.
    /// </summary>
    Stairway
}