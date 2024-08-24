using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;
using static UnityEditor.Progress;
using static UnityEngine.Rendering.DebugUI.MessageBox;

/// <summary>
/// Defines the fundamental types of room pieces that make up the structure of rooms within the game world.
/// This enum categorizes room pieces based on their core identity and role in defining room layouts and boundaries.
/// </summary>
[Serializable]
public enum RoomFixtureStructureType
{
    /// <summary>
    /// No specific room piece type is assigned. This might indicate an empty space or a placeholder for future content.
    /// </summary>
    None = -1,

    /// <summary>
    /// Acts as a wildcard, indicating that any type of room piece is acceptable. This might be used for flexible room generation or selection.
    /// </summary>
    Any = 0,

    Door = 1,
    Wall = 2,
    ShortWall = 3,
    Window = 4,
    Floor = 8,
    Roof = 16
}

/// <summary>
/// Specifies the possible behaviors or interactive features associated with room pieces within the game world.
/// This enum determines how room pieces function and respond to various events or actions,
/// contributing to gameplay mechanics and challenges.
/// </summary>
[Serializable]
public enum RoomFixtureBehaviorType
{
    /// <summary>
    /// No specific behavior is assigned to the room piece. This is used for setting options, exceptions should be thrown if this is returned.
    /// </summary>
    None,

    /// <summary>
    /// Acts as a wildcard, indicating that any type of behavior is acceptable for the room piece. Useful for generation purposes.
    /// </summary>
    Any,

    /// <summary>
    /// The room piece contains a trap.
    /// </summary>
    Trap
}

/// <summary>
/// Represents a fixture in the maze. Like a wall, floor, door.
/// </summary>
public class RoomFixtureMono : GenerateMono
{
    /// <summary>
    /// The type of structure this entity is. From a wall, door, floor.
    /// </summary>
    [SerializeField] public RoomFixtureStructureType Type;

    /// <summary>
    /// The behavior this object should have. Like being a trap. This will change it's generation.
    /// </summary>
    [SerializeField] public RoomFixtureBehaviorType Behavior;

    /// <summary>
    /// Tells whether this fixture is located on floor level.
    /// </summary>
    [SerializeField] private bool FloorLevel = false;

    /// <summary>
    /// Direction this fixture is facing. Used for some types like Doors.
    /// </summary>
    public SpatialOrientation Direction
    {
        get { return this.transform.GetDirection(); }
    }

    /// <summary>
    /// Generate the room fixture.
    /// </summary>
    /// <returns></returns>
    protected override Task OnGenerate(object[] args)
    {
        // Generate a new object.
        GameObject newObject = this.GenerateObject(this.Type, this.Behavior, this.transform.parent, this);

        // Delete this object.
        Destroy(this.gameObject);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Generate a new object. 
    /// </summary>
    /// <param name="type">Type of object to generate.</param>
    /// <param name="sub">The sub category this object should be./param>
    /// <param name="parent">The parent of this new object.</param>
    /// <param name="pieceOptions">The piece options found on the source object.</param>
    /// <returns></returns>
    private GameObject GenerateObject(RoomFixtureStructureType type, RoomFixtureBehaviorType sub, Transform parent, RoomFixtureMono pieceOptions)
    {
        RoomThemePrefabs Style = MazeResourceManager.Instance.Default;
        GameObject newObject = null;

        switch (type)
        {
            case RoomFixtureStructureType.Floor:
                if (pieceOptions.Behavior == RoomFixtureBehaviorType.Trap)
                {
                    newObject = Instantiate(Style.FloorTraps.Random(), this.transform.position, this.transform.rotation, parent);
                }
                else
                {
                    newObject = Instantiate(Style.FloorsPrefabs.Random(), this.transform.position, this.transform.rotation, parent);
                }

                newObject.layer = 3;
                break;
            case RoomFixtureStructureType.Wall:
                switch (sub)
                {
                    case RoomFixtureBehaviorType.Trap:
                        newObject = Instantiate(Style.WallTraps.Random(), this.transform.position, this.transform.rotation, parent);
                        break;
                    default:
                        newObject = Instantiate(Style.WallsPrefabs.Random(), this.transform.position, this.transform.rotation, parent);
                        break;
                }

                newObject.layer = 7;
                break;
            case RoomFixtureStructureType.Window:
                newObject = Instantiate(Style.WindowsPrefabs.Random(), this.transform.position, this.transform.rotation, parent);
                break;
            case RoomFixtureStructureType.ShortWall:
                newObject = Instantiate(Style.ShortWallsPrefabs.Random(), this.transform.position, this.transform.rotation, parent);
                break;
            case RoomFixtureStructureType.Door:
                newObject = Instantiate(Style.DoorsPrefabs.Random(), this.transform.position, this.transform.rotation, parent);
                newObject.layer = 6;
                break;
            case RoomFixtureStructureType.Roof:
                newObject = Instantiate(Style.RoofPrefabs.Random(), this.transform.position, this.transform.rotation, parent);
                break;
            default:
                throw new System.Exception("Type is not supported." + type.ToString());
        }

        // Place back properties.
        newObject.tag = this.gameObject.tag;
        newObject.name = this.name;
        newObject.layer = this.gameObject.layer;

        // Does it have room piece options?
        if (newObject.GetComponent<RoomFixtureMono>() == null)
        {
            newObject.AddComponent<RoomFixtureMono>();
        }

        // Copy existing. We do this so it can be rengerated later. Like to switch a wall to a door.
        EditorUtility.CopySerialized(pieceOptions, newObject.GetComponent<RoomFixtureMono>());

        return newObject;
    }
}