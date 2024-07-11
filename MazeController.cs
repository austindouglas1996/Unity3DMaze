using OverfortGames.FirstPersonController;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(DoorRegistry))]
[RequireComponent(typeof(MazeRoomGenerator))]
[RequireComponent(typeof(MazeHallwayGenerator))]
[RequireComponent(typeof(MazeItemGenerator))]
[RequireComponent (typeof(PathFinding))]
public class MazeController : MonoBehaviour
{
    /// <summary>
    /// Has the maze finished generating?
    /// </summary>
    [HideInInspector]
    public bool GenerateFinished = false;

    [Header("Generation Options")]
    [Tooltip("Generate rooms throughout the maze.")]
    public bool GenerateRooms = true;

    [Tooltip("Generate hallways. NOTE: This depends on Room generation.")]
    public bool GenerateHalls = true;

    [Tooltip("Generate props on room fixtures.")]
    public bool GenerateProps = true;

    [SerializeField] private GameObject Player;

    [Header("Debug")]
    [Tooltip("Render the pathing cells for the maze grid.")]
    public bool ShowPathingCells = false;
    [SerializeField] private GameObject PathCellCube;

    [Tooltip("Render the door position cells used for pathing.")]
    public bool ShowDoorPathingCells = false;
    [SerializeField] private GameObject DoorPathCube;

    [Tooltip("Destroy the current and regenerate.")]
    public bool ResetMaze = false;

    /// <summary>
    /// Container for path cells.
    /// </summary>
    private GameObject PathContainer;

    /// <summary>
    /// Container for door path cells.
    /// </summary>
    private GameObject DoorPathContainer;

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
        this.PathContainer = Instantiate(new GameObject("PathContainer"), this.transform.position, Quaternion.identity);
        this.DoorPathContainer = Instantiate(new GameObject("DoorPathContainer"), this.transform.position, Quaternion.identity);

        Grid = new MazeGrid();
        this.DoorRegistry = this.GetComponent<DoorRegistry>();
        this.Rooms = this.GetComponent<MazeRoomGenerator>();
        this.Hallways = this.GetComponent<MazeHallwayGenerator>();
        this.Items = this.GetComponent<MazeItemGenerator>();
        this.PathFinder = this.GetComponent<PathFinding>();

        await this.CreateMaze();
    }

    /// <summary>
    /// Create the mazes along with their respective addons.
    /// </summary>
    /// <returns></returns>
    private async Task CreateMaze()
    {
        if (GenerateRooms)
            await this.Rooms.Generate();

        await Task.Delay(1000);

        if (GenerateRooms && GenerateHalls)
            await this.Hallways.Generate();

        await Task.Delay(10);

        await this.Items.Generate();
        await CleanupDoors();


        foreach (var cell in Grid.Cells)
        {
            GameObject go = Instantiate(this.PathCellCube, cell.Position, Quaternion.identity, this.PathContainer.transform);
            go.tag = "Path";

            go.AddComponent<CellMono>();
            go.GetComponent<CellMono>().GroupId = cell.GroupId;
            go.GetComponent<CellMono>().Position = cell.Position;
            go.GetComponent<CellMono>().Room = cell.Room;
            go.GetComponent<CellMono>().Type = cell.Type;
        }


        DoorRegistry.Debug();
        GenerateFinished = true;
        this.OnValidate();

        this.Player.GetComponent<FirstPersonController>().Teleport(this.Rooms.Generated[0].transform.position);
        this.Player.gameObject.SetActive(true);
    }

    /// <summary>
    /// Destroy maze instances to clear the maze.
    /// </summary>
    /// <returns></returns>
    private async Task DestroyMaze()
    {
        this.Player.gameObject.SetActive(false);

        await this.Rooms.ResetGenerator();
        await this.Hallways.ResetGenerator();
        await this.Items.ResetGenerator();

        this.Grid.ClearAll();
        this.DoorRegistry.Clear();

        // Destroy path container.
        foreach (Transform child in this.PathContainer.transform)
        {
            Destroy(child.gameObject);
        }

        // Destroy door container.
        foreach (Transform child in this.DoorPathContainer.transform)
        {
            Destroy(child.gameObject);
        }
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
            Cell existingCell = this.Grid.Find(doors.Key.transform.position.RoundToInt(), new Vector3(2f, 2f, 2f));

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
            directNeighborCell.Room = doors.Value.GetOtherRoom(existingCell.Room);

            // Debug cubes.
            Instantiate(this.DoorPathCube, existingCell.Position, Quaternion.identity, this.DoorPathContainer.transform);
            Instantiate(this.DoorPathCube, directNeighborCell.Position, Quaternion.identity, this.DoorPathContainer.transform);
        }
    }

    private async Task OnValidate()
    {
        if (this.ResetMaze)
        {
            this.ResetMaze = false;
            await this.DestroyMaze();
            await this.CreateMaze();
        }
        this.DisplayPathingCells(this.ShowPathingCells);
        this.DisplayDoorCells(this.ShowDoorPathingCells);
    }

    private void DisplayPathingCells(bool show)
    {
        if (PathContainer == null) return;

        foreach (var meshRenderer in PathContainer.GetComponentsInChildren<MeshRenderer>())
        {
            meshRenderer.enabled = show;
        }
    }

    private void DisplayDoorCells(bool show)
    {
        if (DoorPathContainer == null) return;

        foreach (var meshRenderer in DoorPathContainer.GetComponentsInChildren<MeshRenderer>())
        {
            meshRenderer.enabled = show;
        }
    }
}
