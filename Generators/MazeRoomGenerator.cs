using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using VHierarchy.Libs;
using Random = UnityEngine.Random;


[RequireComponent(typeof(MazeController))]
public class MazeRoomGenerator : MonoBehaviour, IGenerator<RoomMono>
{
    [Tooltip("A collection of generic rooms. This contains rooms that should be chosen regularly throughout the maze.")]
    [SerializeField] private List<GameObject> GenericRoomPrefabs = new List<GameObject>();

    [Tooltip("A collection of special rooms. This contains rooms that may have special functionality, or larger than normal.")]
    [SerializeField] private List<GameObject> SpecialRoomPrefabs = new List<GameObject>();

    [Tooltip("The root room to be used when generating the room maze. If this is null one will be selected by random.")]
    [SerializeField] private GameObject RootPrefab = null;

    [Tooltip("The chance (0-100%) that a special room will be selected.")]
    [SerializeField] private int ChanceForSpecial = 2;

    [Tooltip("The minimum amount of rooms to make.")]
    [SerializeField] private int MinRooms = 5;

    [Tooltip("The maximum amount of rooms to make.")]
    [SerializeField] private int MaxRooms = 15;

    private bool StartCalled = false;

    /// <summary>
    /// The chosen amount of rooms to generate.
    /// </summary>
    private int RoomsToGenerate = -1;

    /// <summary>
    /// A list of special rooms that have been used. We want to try to not reuse.
    /// </summary>
    private List<GameObject> UsedSpecialRooms = new List<GameObject>();

    /// <summary>
    /// Gets the <see cref="Maze"/> instance to use for accessing important properties.
    /// </summary>
    private MazeController Maze;

    /// <summary>
    /// Gets the <see cref="Maze"/> door registry to control what <see cref="DoorPair"/> are in play.
    /// </summary>
    private DoorRegistry DoorRegistry;

    /// <summary>
    /// Returns whether <see cref="Generate"/> has been called.
    /// </summary>
    public bool GenerateCalled { get; private set; }

    /// <summary>
    /// Returns whether <see cref="Generate"/> has finished.
    /// </summary>
    public bool GenerateFinished { get; private set; }

    /// <summary>
    /// Gets a list of <see cref="RoomMono"/> that are in <see cref="Generated"/> that do not have any available room connections.
    /// </summary>
    private List<RoomMono> GeneratedWithoutAvailability = new List<RoomMono>();

    /// <summary>
    /// Gets a list of <see cref="RoomMono"/> instances generated in this maze.
    /// </summary>
    public List<RoomMono> Generated { get; private set; } = new List<RoomMono>();

    /// <summary>
    /// Returns whether a <see cref="RoomMono"/> intersects with another generated room in <see cref="Generated"/>.
    /// </summary>
    /// <param name="roomA"></param>
    /// <returns></returns>
    public bool CheckForCollision(RoomMono roomA)
    {
        foreach (RoomMono room in Generated)
        {
            if (room == roomA)
                continue;

            if (RoomMono.CheckForIntersection(roomA, room) || RoomMono.CheckForContains(roomA, room))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Generate the rooms for this maze.
    /// </summary>
    /// <returns></returns>
    public async Task Generate()
    {
        if (this.GenerateCalled) return;
        this.GenerateCalled = true;

        if (!StartCalled) Start();

        ChooseRootRoom();
        await GenerateRoomsUntilSatisfied();

        await Task.Delay(10);

        this.Maze.Grid.ClearAll();
        for (int i = 0; i < Generated.Count; i++)
        {
            RoomMono room = Generated[i];

            if (room.MultiFloorRoom)
                this.Maze.Grid.SetBounds(room.Bounds, room.transform.position.RoundToInt(), CellType.Room, room.name);
            else
                this.Maze.Grid.SetRoomCells(room, room.name, CellType.Room);
        }

        this.GenerateFinished = true;
    }

    public async Task ResetGenerator()
    {
        this.UsedSpecialRooms.Clear();
        this.GeneratedWithoutAvailability.Clear();

        foreach (RoomMono room in Generated)
        {
            room.gameObject.Destroy();
        }

        this.Generated.Clear();

        this.GenerateCalled = false;
        this.GenerateFinished = false;
    }

    /// <summary>
    /// Called on initialization, or something.
    /// </summary>
    private void Start()
    {
        RoomsToGenerate = Random.Range(MinRooms, MaxRooms);
        Maze = this.GetComponent<MazeController>();
        DoorRegistry = this.GetComponent<DoorRegistry>();
    }

    /// <summary>
    /// Set the <see cref="RootPrefab"/>. Only sets this value if <see cref="RootPrefab"/> is null.
    /// </summary>
    private void ChooseRootRoom()
    {
        if (RootPrefab != null) return;
        RootPrefab = GetRandomRoomPrefab(null);
    }

    /// <summary>
    /// Generate <see cref="RoomMono"/> instances until reaching <see cref="RoomsToGenerate"/>
    /// </summary>
    /// <returns></returns>
    private async Task GenerateRoomsUntilSatisfied()
    {
        // Stops an infinity loop if something is wrong internally.
        // Note: This is for debug purposes.
        int maxRetries = RoomsToGenerate * 3;

        // Create our root object.
        RoomMono root = await InstantiateRoom(RootPrefab);

        // Add to grid.
        AddRoomToGrid(root);

        // Let's have some fun with this!
        // Let's choose a random amount of root rooms to fill 
        // then we will choose a random room to extend.
        int rootDoorsToFill = UnityEngine.Random.Range(1, root.Doors.Count());
        while (rootDoorsToFill > 0 && Generated.Count < RoomsToGenerate)
        {
            bool result = await ExtendRoom(root);
            if (result)
                rootDoorsToFill--;
            else
                maxRetries--;
        }

        // Populate will now only do one room then it will pick another room.
        while (Generated.Count < RoomsToGenerate)
        {
            List<RoomMono> availableRooms = Generated.Except(GeneratedWithoutAvailability).ToList();
            if (availableRooms.Count == 0)
            {
                Debug.LogError("Ran out of rooms with available doors before hitting the generate count.");
            }

            await ExtendRoom(availableRooms.Random());
        }
    }

    /// <summary>
    /// Expands the maze structure by extending an existing room with a new, randomly connected room.
    /// </summary>
    /// <param name="room">The existing room to be extended.</param>
    /// <returns>True if the extension was successful, false otherwise.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if the provided room is null.</exception>
    private async Task<bool> ExtendRoom(RoomMono room)
    {
        if (room == null)
        {
            Debug.LogError("Room is null.");
            throw new System.ArgumentNullException(nameof(room));
        }

        // Make the position a solid number.
        room.transform.position = room.GetBoundsInt.position;

        // Select a random door.
        GameObject door = GetRandomDoor(room);
        if (door == null)
        {
            return false;
        }

        // Get a new room to connect.
        RoomMono B = await InstantiateRoom(GetRandomRoomPrefab(room.gameObject));

        // Attempt to connect the room.
        if (!await ConnectRoom(room, door.transform, B))
        {        
            // Make sure Room B is destroyed.
            if (B.IsDestroyed() == false)
            {
                Debug.LogWarning("RoomB not destroyed. Name:" + B.name);
            }

            return false;
        }

        // Set the door connection.
        this.DoorRegistry.SetConnection(door, B);

        // Change this to a solid too.
        B.transform.position = B.GetBoundsInt.position;

        return true;
    }

    /// <summary>
    /// Attempts to connect two <see cref="RoomMono"/> objects together by a door in RoomA with a randomly selected door in RoomB.
    /// </summary>
    /// <param name="roomA">The first room to be connected.</param>
    /// <param name="roomADoor">The specific door on roomA through which the connection will be established.</param>
    /// <param name="roomB">The second room to be joined with roomA.</param>
    /// <returns>True if the connection was successful, false otherwise.</returns>
    public async Task<bool> ConnectRoom(RoomMono roomA, Transform roomADoor, RoomMono roomB)
    {
        // Put roomB on top of roomA. 
        roomB.transform.position = roomA.transform.position;

        // Grab best door location.
        // NOTE: This function may not work since we put roomB on top of roomA.
        Transform roomBDoor = GetBestDoor(roomADoor, roomB);

        // Reset room rotation to zero. to stop any nonsense from occurring.
        // you would not believe the amount of non-sense that happens around here.
        // Whole damn codebase is haunted.
        // 🎵 Who you gonna call? 🎵
        roomB.transform.rotation = quaternion.Euler(0, 0, 0);

        // Adjust the room rotation so roomA and roomB can properly connect.
        roomB.transform.rotation = Quaternion.Euler(0, GetRoomRotationBasedOnDoor(roomADoor, roomBDoor), 0);

        // Adjust the room position to fully connect with RoomA's door.
        roomB.transform.position = GetRoomPositionWithCalculatedOffset(roomA.gameObject, roomB.gameObject, roomADoor, roomBDoor);

        // Make sure there is no collisions with adding the room.
        if (CheckForCollision(roomB))
        {
            RemoveRoom(roomB);

            while (!roomB.IsDestroyed())
            {
                await Task.Delay(10);
            }

            return false;
        }

        // Add to grid.
        AddRoomToGrid(roomB);

        // Destroy B door since it's now connected to A door.
        DestroyImmediate(roomBDoor.gameObject);

        return true;
    }

    /// <summary>
    /// Create a new <see cref="RoomMono"/> object.
    /// </summary>
    /// <param name="prefab"></param>
    /// <returns></returns>
    private async Task<RoomMono> InstantiateRoom(GameObject prefab)
    {
        if (prefab == null) throw new System.ArgumentNullException(nameof(prefab));

        GameObject baked = Instantiate(prefab, Vector3.zero, prefab.transform.rotation, transform);
        baked.name = "Room" + Generated.Count;

        RoomMono room = baked.GetComponent<RoomMono>();

        // Allow the room to generate.
        await room.Generate();

        // Wait until room generation is complete.
        while (!room.GenerateFinished)
        {
            await Task.Delay(10);
        }

        // Add it's doors into the registry.
        var doors = room.Doors;
        foreach (GameObject door in doors)
        {
            door.name = $"DOOR{doors.Count}";
            this.DoorRegistry.Add(door, room);
        }

        // Add to generated rooms.
        this.Generated.Add(room);

        return room;
    }

    /// <summary>
    /// Remove a <see cref="RoomMono"/> that is in <see cref="Generated"/>.
    /// </summary>
    /// <param name="roomA"></param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentNullException"></exception>
    /// <exception cref="System.NullReferenceException"></exception>
    public bool RemoveRoom(RoomMono roomA)
    {
        if (roomA == null)
            throw new System.ArgumentNullException(nameof(roomA));

        if (!Generated.Contains(roomA))
            throw new System.NullReferenceException("roomA does not exist in GeneratedRooms list");

        // Remove.
        Generated.Remove(roomA);

        // Remove bounds.
        this.Maze.Grid.ClearBounds(roomA.Bounds, roomA.transform.position.RoundToInt());

        // Destroy.
        DestroyImmediate(roomA.gameObject);

        return true;
    }

    /// <summary>
    /// Returns a random room that should be generated.
    /// </summary>
    /// <returns></returns>
    private GameObject GetRandomRoomPrefab(GameObject exclude)
    {
        if (RandomHelper.Chance(ChanceForSpecial) && SpecialRoomPrefabs.Count != 0)
        {
            // Have we used all special rooms?
            if (UsedSpecialRooms.Count == SpecialRoomPrefabs.Count)
                UsedSpecialRooms.Clear();

            return SpecialRoomPrefabs.Except(UsedSpecialRooms).ToList().Random();
        }

        if (exclude == null)
            return GenericRoomPrefabs.Random();
        else
            return GenericRoomPrefabs.Except(new GameObject[] { exclude }).ToList().Random();
    }

    /// <summary>
    /// Add a <see cref="RoomMono"/> instance to <see cref="MazeGrid"/> instance in <see cref="Maze"/>
    /// </summary>
    /// <param name="room"></param>
    /// <returns></returns>
    private List<Cell> AddRoomToGrid(RoomMono room)
    {
        if (room == null)
            throw new System.ArgumentNullException("Room cannot be null.");

        List<Cell> cells = null;

        if (room.MultiFloorRoom)
        {
            cells = this.Maze.Grid.SetBounds(room.transform.BoundingBox(), room.transform.position.RoundToInt(), CellType.Room, room.name);
            foreach (Cell cell in cells)
                cell.Room = room;
        }
        else
        {
            this.Maze.Grid.SetRoomCells(room, room.name);
        }

        return cells;
    }

    /// <summary>
    /// Returns a random door to use out of a <see cref="RoomMono"/>. If the room is out of doors, it will add it to <see cref="GeneratedWithoutAvailability"/> list.
    /// </summary>
    /// <param name="room"></param>
    /// <returns></returns>
    private GameObject GetRandomDoor(RoomMono room)
    {
        GameObject selectedDoor = null;
        bool roomFinished = false;

        List<DoorPair> doors = this.DoorRegistry.GetAvailable(room);
        if (doors.Count == 0)
            roomFinished = true;
        else
        {
            selectedDoor = doors.Random().Door;

            // Was this the only door?
            if (doors.Count == 1)
                roomFinished = true;
        }

        // Does this room no longer have any available doors?
        if (roomFinished)
            if (!this.GeneratedWithoutAvailability.Contains(room))
                this.GeneratedWithoutAvailability.Add(room);

        return selectedDoor;
    }

    /// <summary>
    /// Loop through <see cref="Transform"/> of a room to find the best door suited to fit another rooms door.
    /// </summary>
    /// <param name="roomADoor"></param>
    /// <param name="roomB"></param>
    /// <returns></returns>
    /// <remarks>I made this function awhile ago. I don't believe it's used in the current stack, but if you remove it
    /// then sometimes doors break, but if you keep it. Doors don't break. </remarks>
    public Transform GetBestDoor(Transform roomADoor, RoomMono roomB)
    {
        Transform bestDoor = null;
        float closestDistance = Mathf.Infinity;

        Quaternion defaultRot = roomB.transform.rotation;
        Quaternion bestRot = Quaternion.identity;
        Quaternion currentRot = Quaternion.identity;

        for (int i = 0; i < 3; i++)
        {
            // Get the current rotation.
            currentRot = Quaternion.Euler(0, i * 90, 0);

            // Set it temporarily.
            roomB.transform.rotation = currentRot;

            // Loop through all doors in room B
            var roomBDoors = this.DoorRegistry.GetAvailable(roomB);
            foreach (DoorPair roomBDoorConnection in roomBDoors)
            {
                Transform roomBDoor = roomBDoorConnection.Door.transform;

                // Calculate distance between room B door and room A door
                float distance = Vector3.Distance(roomBDoor.position, roomADoor.position);

                // Check if current door is closer than the best door so far
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    bestDoor = roomBDoor;
                    bestRot = currentRot;
                }
            }

            // Reset rot.
            roomB.transform.rotation = defaultRot;
        }

        // Set values.
        roomB.transform.rotation = bestRot;

        // Return the chosen.
        return bestDoor;
    }

    /// <summary>
    /// Grab the rotation offset required to get RoomB to align with RoomA.
    /// </summary>
    /// <param name="roomADoor"></param>
    /// <param name="roomBDoor"></param>
    /// <returns></returns>
    /// <remarks>This entire function is a big plate of spaghetti, but it works!</remarks>
    private static float GetRoomRotationBasedOnDoor(Transform roomADoor, Transform roomBDoor)
    {
        if (roomADoor == null || roomBDoor == null)
            throw new System.ArgumentNullException("A door or B door is null.");

        SpatialOrientation A = roomADoor.GetComponent<RoomFixtureMono>().Direction;
        SpatialOrientation B = roomBDoor.GetComponent<RoomFixtureMono>().Direction;

        if (A == B
            || A == SpatialOrientation.Left && B == SpatialOrientation.Up)
        {
            return 180;
        }
        else if (A == SpatialOrientation.Left && B == SpatialOrientation.Down
            || A == SpatialOrientation.Down && B == SpatialOrientation.Right
            || A == SpatialOrientation.Right && B == SpatialOrientation.Up
            || A == SpatialOrientation.Up && B == SpatialOrientation.Left)
        {
            return -90;
        }
        else if (A == SpatialOrientation.Down && B == SpatialOrientation.Left
            || A == SpatialOrientation.Right && B == SpatialOrientation.Down
            || A == SpatialOrientation.Up && B == SpatialOrientation.Right
            || A == SpatialOrientation.Left && B == SpatialOrientation.Down)
        {
            return 90;
        }

        return 0;
    }

    /// <summary>
    /// Calculate a rooms position based on it's current position, the distance between roomADoor and roomBDoor and roomA and roomB's size.
    /// </summary>
    /// <param name="roomA">The parent room.</param>
    /// <param name="roomB">The child room that is being connected.</param>
    /// <param name="roomADoor">The door connected to RoomA.</param>
    /// <param name="roomBDoor">The door connected to RoomB.</param>
    /// <returns>A <see cref="Vector3"/> with the calculated position to place RoomB.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown if roomADoor tag contains an invalid value.</exception>
    private static Vector3 GetRoomPositionWithCalculatedOffset(GameObject roomA, GameObject roomB, Transform roomADoor, Transform roomBDoor)
    {
        // Room bounds. So very helpful
        Bounds roomABounds = roomA.transform.BoundingBox();
        Bounds roomBBounds = roomB.transform.BoundingBox();

        // The offset used to align the room with the door.
        Vector3 doorOffset = roomADoor.transform.position - roomBDoor.transform.position;

        // Get piece options to grab door direction.
        RoomFixtureMono pieceA = roomADoor.GetComponent<RoomFixtureMono>();

        switch (pieceA.Direction)
        {
            case SpatialOrientation.Up:
                return new Vector3(roomABounds.max.x + roomBBounds.extents.x, roomABounds.center.y + doorOffset.y, roomABounds.center.z + doorOffset.z);
            case SpatialOrientation.Right:
                return new Vector3(roomABounds.center.x + doorOffset.x, roomABounds.center.y + doorOffset.y, roomABounds.min.z - roomBBounds.extents.z);
            case SpatialOrientation.Down:
                return new Vector3(roomABounds.min.x - roomBBounds.extents.x, roomABounds.center.y + doorOffset.y, roomABounds.center.z + doorOffset.z);
            case SpatialOrientation.Left:
                return new Vector3(roomABounds.center.x + doorOffset.x, roomABounds.center.y + doorOffset.y, roomABounds.max.z + roomBBounds.extents.z);
            default:
                Debug.LogError("We should not have reached here... A " + pieceA.Direction + " RoomA Name: " + roomA.name);
                throw new System.ArgumentOutOfRangeException(roomADoor.tag + " tag is not supported.");
        }
    }
}