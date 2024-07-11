using UnityEngine;

/// <summary>
/// Represents a visual representation of a <see cref="DoorPair"/> within the Unity editor.
/// 
/// This MonoBehaviour serves as a visual aid for debugging and understanding door connections,
/// providing a clear overview of the door and its connected rooms directly within the editor.
/// </summary>
public class DoorPairMono : MonoBehaviour
{
    public DoorPairMono(DoorPair doorPair)
    {
        this.Door = doorPair.Door;
        this.A = doorPair.A;
        this.B = doorPair.B;
        this.ACell = new CellMono(doorPair.ACell);
        this.BCell = new CellMono(doorPair.BCell);
    }

    /// <summary>
    /// The GameObject representing the actual door within the game world.
    /// This is the physical door that players can interact with.
    /// </summary>
    [SerializeField] public GameObject Door;

    /// <summary>
    /// The RoomMono representing the first room connected to the door.
    /// </summary>
    [SerializeField] public RoomMono A;

    /// <summary>
    /// The RoomMono representing the second room connected to the door.
    /// </summary>
    [SerializeField] public RoomMono B;

    /// <summary>
    /// The <see cref="CellMono"/> representing a <see cref="Cell"/> for the A door.
    /// </summary>
    [SerializeField] public CellMono ACell;

    /// <summary>
    /// The <see cref="CellMono"/> representing a <see cref="Cell"/> for the B door.
    /// </summary>
    [SerializeField] public CellMono BCell;
}