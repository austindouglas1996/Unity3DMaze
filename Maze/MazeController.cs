using OverfortGames.FirstPersonController;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(DoorRegistry))]
[RequireComponent(typeof(RoomMazeGenerator))]
[RequireComponent(typeof(HallwayMazeGenerator))]
[RequireComponent(typeof(LootMazeGenerator))]
public class MazeController : MonoBehaviour
{
    /// <summary>
    /// Returns the existing <see cref="MazeController"/>.
    /// </summary>
    public static MazeController Instance { get; private set; }

    [Tooltip("Helps with controlling the player.")]
    [SerializeField] private GameObject Player;

    [Header("Debug")]
    [Tooltip("Render the pathing cells for the maze grid.")]
    public bool ShowPathingCells = false;

    [Tooltip("Render the door position cells used for pathing.")]
    public bool ShowDoorPathingCells = false;

    [Tooltip("Destroy the current and regenerate.")]
    [SerializeField] public bool ResetMaze = false;

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
    /// Helps with path-finding and keeping the positions of rooms/hallways.
    /// </summary>
    public MazeGrid Grid;

    /// <summary>
    /// Helps with generating rooms.
    /// </summary>
    [HideInInspector] public RoomMazeGenerator Rooms;

    /// <summary>
    /// Helps with generating hallways.
    /// </summary>
    [HideInInspector] public HallwayMazeGenerator Hallways;

    /// <summary>
    /// Helps with generating loot around the maze.
    /// </summary>
    [HideInInspector] public LootMazeGenerator Loot;

    /// <summary>
    /// Unity function called.
    /// </summary>
    private async void Start()
    {
        Grid = new MazeGrid();

        this.DoorRegistry = this.GetComponent<DoorRegistry>();
        this.Rooms = this.GetComponent<RoomMazeGenerator>();
        this.Hallways = this.GetComponent<HallwayMazeGenerator>();
        this.Loot = this.GetComponent<LootMazeGenerator>();

        await this.CreateMaze();
    }

    /// <summary>
    /// Generate the maze.
    /// </summary>
    /// <returns></returns>
    private async Task CreateMaze()
    {
        // Disable player so they don't respawn.
        this.Player.SetActive(false);

        // Create containers for Debug functions.
        this.PathContainer = Instantiate(new GameObject("PathContainer"), this.transform.position, Quaternion.identity);
        this.DoorPathContainer = Instantiate(new GameObject("DoorPathContainer"), this.transform.position, Quaternion.identity);

        await this.Rooms.Generate(null);
        await this.Hallways.Generate(null);
        //await this.Loot.Generate(null);

        // Cleanup the door connections around the maze.
        await this.Cleanup();

        foreach (var cell in Grid.Cells)
        {
            GameObject go = Instantiate(MazeResourceManager.Instance.DebugCube, cell.Position, Quaternion.identity, this.PathContainer.transform);
            go.transform.parent = this.PathContainer.transform;
            go.tag = "Path";

            CellMono cMono = go.AddComponent<CellMono>();
            cMono.Set(cell);
        }


        // Bring player back.
        this.Player.GetComponent<FirstPersonController>().Teleport(this.Grid.Cells[0].Position);
        this.Player.gameObject.SetActive(true);
    }

    /// <summary>
    /// Cleanup the door connections around the maze.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="System.ArgumentException"></exception>
    private async Task Cleanup()
    {
        // Remove doors without connections.
        foreach (DoorPair pair in DoorRegistry.GetAvailable())
        {
            if (pair.Door.IsDestroyed())
            {
                continue;
            }

            DoorRegistry.Remove(pair.Door);

            pair.Door.GetComponent<RoomFixtureMono>().Type = RoomFixtureStructureType.Wall;
            await pair.Door.GetComponent<RoomFixtureMono>().ReGenerate(null);
        }

        // Register doors into grid.
        foreach (var doors in DoorRegistry.Doors)
        {
            if (doors.Key.IsDestroyed())
            {
                continue;
            }

            Cell existingCell = Grid.Find(doors.Key.transform.position.RoundToInt(), new Vector3(2f, 2f, 2f));

            // The door should already exist if not we have problems.
            if (existingCell.Type == CellType.None)
            {
                throw new System.ArgumentException("Failed to find door cell position for " + doors.Key.name + " in room " + doors.Value.A.name);
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

            // Set cells.
            doors.Value.ACell = doors.Value.A == existingCell.Room ? existingCell : directNeighborCell;
            doors.Value.BCell = doors.Value.B == existingCell.Room ? existingCell : directNeighborCell;
        }
    }

    /// <summary>
    /// Destroy maze instances to clear the maze.
    /// </summary>
    /// <returns></returns>
    private async Task DestroyMaze()
    {
        this.Player.gameObject.SetActive(false);

        this.Grid.ClearAll();
        this.DoorRegistry.Clear();

        await this.Rooms.ResetGenerator();
        await this.Hallways.ResetGenerator();
        await this.Loot.ResetGenerator();

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
    /// This helps with the debug functions.
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Display the registered cells.
    /// </summary>
    /// <param name="show"></param>
    private void DisplayPathingCells(bool show)
    {
        if (PathContainer == null) return;

        foreach (var meshRenderer in PathContainer.GetComponentsInChildren<MeshRenderer>())
        {
            meshRenderer.enabled = show;
        }
    }

    /// <summary>
    /// Display the registered doors.
    /// </summary>
    /// <param name="show"></param>
    private void DisplayDoorCells(bool show)
    {
        if (DoorPathContainer == null) return;

        foreach (var meshRenderer in DoorPathContainer.GetComponentsInChildren<MeshRenderer>())
        {
            meshRenderer.enabled = show;
        }
    }
}