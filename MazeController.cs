using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(DoorRegistry))]
[RequireComponent(typeof(MazeRoomGenerator))]
[RequireComponent(typeof(MazeHallwayGenerator))]
[RequireComponent(typeof(MazeItemGenerator))]
[RequireComponent (typeof(PathFinding))]
public class MazeController : MonoBehaviour
{
    public GameObject debugCube;

    /// <summary>
    /// Has the maze finished generating?
    /// </summary>
    [HideInInspector]
    public bool GenerateFinished = false;

    /// <summary>
    /// Controls the connections to doors.
    /// </summary>
    public DoorRegistry DoorRegistry { get; private set; }

    /// <summary>
    /// Helps with generating rooms in the maze.
    /// </summary>
    public MazeRoomGenerator Rooms { get; private set; }

    /// <summary>
    /// Helps with generating hallways to extend the rooms.
    /// </summary>
    public MazeHallwayGenerator Hallways { get; private set; }

    /// <summary>
    /// Helps with generating sellable items for the player.
    /// </summary>
    public MazeItemGenerator Items { get; private set; }

    /// <summary>
    /// Helps with pathfinding around the maze.
    /// </summary>
    public PathFinding PathFinder { get; private set; }

    /// <summary>
    /// Helps with path-finding and keeping the positions of rooms/hallways.
    /// </summary>
    public MazeGrid Grid;

    /// <summary>
    /// Called on start.
    /// </summary>
    private async void Start()
    {
        Grid = new MazeGrid();
        this.DoorRegistry = this.GetComponent<DoorRegistry>();
        this.Rooms = this.GetComponent<MazeRoomGenerator>();
        this.Hallways = this.GetComponent<MazeHallwayGenerator>();
        this.Items = this.GetComponent<MazeItemGenerator>();
        this.PathFinder = this.GetComponent<PathFinding>();

        await this.Rooms.Generate();
        await this.Hallways.Generate();

        await Task.Delay(1000);
        await this.Items.Generate();
        await CleanupDoors();

        /*
        foreach (var cell in Grid.Cells)
        {
            Instantiate(debugCube, cell.Position, Quaternion.identity, this.transform);
        }
        */

        DoorRegistry.Debug();
        GenerateFinished = true;
    }

    /// <summary>
    /// Remove any excess doors.
    /// </summary>
    /// <returns></returns>
    private async Task CleanupDoors()
    {
        // Remove doors without connections.
        foreach (DoorPair pair in DoorRegistry.GetAvailable())
        {
            await pair.A.TurnDoorIntoWall(pair.Door);
        }

        // Remove somehow unregistered doors.
        foreach (RoomMono room in this.Rooms.Generated)
        {
            foreach (Transform go in await room.GetChildrenByPieceType(RoomFixtureIdentityType.Door))
            {
                RoomFixtureMono piece = go.GetComponent<RoomFixtureMono>();
                if (!piece.IsRegistered && DoorRegistry.Get(go.gameObject) != null)
                {
                    await room.TurnDoorIntoWall(go.gameObject);
                }
            }
        }

        // Register doors into grid.
        foreach (var doors in DoorRegistry.Doors)
        {
            Cell existingCell = this.Grid.Find(doors.Key.transform.position.RoundToInt(), 2);

            // The door should already exist if not we have problems.
            if (existingCell.Type == CellType.None)
            {
                //throw new System.ArgumentException("Failed to find door cell position for " + doors.Key.name + " in room " + doors.Value.A.name);
                continue;
            }

            // Find the direction the door is aiming in then set it's closest neighbor to also a door.
            SpatialOrientation doorDirection = doors.Value.Door.GetComponent<RoomFixtureMono>().Direction; 

            if (doorDirection == SpatialOrientation.None)
            {
                throw new System.ArgumentException("Door does not have a direction set.");
            }

            Cell directNeighborCell = Grid.Neighbor(existingCell, doorDirection);

            // Set the current cell and neighbor cell to door.
            existingCell.Type = CellType.Door;
            directNeighborCell.Type = CellType.Door;

            // Debug cubes.
            Instantiate(debugCube, existingCell.Position, Quaternion.identity, this.transform);
            Instantiate(debugCube, directNeighborCell.Position, Quaternion.identity, this.transform);
        }
    }
}
