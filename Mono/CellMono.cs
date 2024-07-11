using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CellMono : MonoBehaviour
{
    /// <summary>
    /// The GameObject representing the actual door within the game world.
    /// This is the physical door that players can interact with.
    /// </summary>
    [SerializeField] public CellMono Root;
    [SerializeField] public string GroupId;
    [SerializeField] public Vector3Int Position;
    [SerializeField] public CellType Type;
    [SerializeField] public RoomMono Room;

    public void Set(Cell cell)
    {
        this.GroupId = cell.GroupId;
        this.Position = cell.Position;
        this.Type = cell.Type;
        this.Room = cell.Room;
    }
}
