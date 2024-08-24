using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using VHierarchy.Libs;

[RequireComponent(typeof(MazeController))]
public class RoomMazeGenerator : MazeGenerator<RoomMono>
{
    [Tooltip("The root room to be used when generating the room maze. If this is null one will be selected by random.")]
    [SerializeField] private RoomMono RootPrefab = null;

    [Tooltip("The chance (0-100%) that a special room will be selected.")]
    [SerializeField] private int ChanceForSpecial = 0;

    [Tooltip("The minimum amount of rooms to make.")]
    [SerializeField] private int MinRooms = 1;

    [Tooltip("The maximum amount of rooms to make.")]
    [SerializeField] private int MaxRooms = 1;

    [Tooltip("A collection of generic rooms. This contains rooms that should be chosen regularly throughout the maze.")]
    [SerializeField] private List<RoomMono> GenericRoomPrefabs = new List<RoomMono>();

    [Tooltip("A collection of special rooms. This contains rooms that may have special functionality, or larger than normal.")]
    [SerializeField] private List<RoomMono> SpecialRoomPrefabs = new List<RoomMono>();

    /// <summary>
    /// Gets a list of <see cref="RoomMono"/> that are in <see cref="Generated"/> that do not have any available room connections.
    /// </summary>
    private List<RoomMono> GeneratedWithoutAvailability = new List<RoomMono>();

    /// <summary>
    /// A list of special rooms that have been used. We want to try to not reuse.
    /// </summary>
    private List<RoomMono> UsedSpecialRooms = new List<RoomMono>();

    /// <summary>
    /// Returns whether a <see cref="RoomMono"/> intersects with another generated room in <see cref="Generated"/>.
    /// </summary>
    /// <param name="roomA"></param>
    /// <returns></returns>
    public bool CheckForCollision(RoomMono roomA)
    {
        foreach (RoomMono room in GeneratedEntities)
        {
            if (room == roomA)
                continue;

            if (RoomMono.CheckForIntersection(roomA, room.GetComponent<RoomMono>()) || RoomMono.CheckForContains(roomA, room.GetComponent<RoomMono>()))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Generate the rooms.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    protected override async Task OnGenerate(object[] args)
    {
        if (RootPrefab == null)
            RootPrefab = GetRandomRoomPrefab(null);

        await GenerateRoomsUntilSatisfied();

        // 8/21/2024
        // I forgot why we did this, but remember its for a big reason.
        this.Maze.Grid.ClearAll();
        for (int i = 0; i < GeneratedEntities.Count; i++)
        {
            RoomMono room = GeneratedEntities[i].GetComponent<RoomMono>();
            this.AddRoomToGrid(room);
        }
    }

    /// <summary>
    /// Reset the room generator.
    /// </summary>
    /// <returns></returns>
    protected override Task OnResetGenerator()
    {
        this.UsedSpecialRooms.Clear();
        this.GeneratedWithoutAvailability.Clear();

        foreach (RoomMono room in GeneratedEntities)
        {
            room.gameObject.Destroy();
        }

        this.GeneratedEntities.Clear();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Generate <see cref="RoomMono"/> instances until reaching <see cref="RoomsToGenerate"/>
    /// </summary>
    /// <returns></returns>
    private async Task GenerateRoomsUntilSatisfied()
    {
        int RoomsToGenerate = Random.Range(MinRooms, MaxRooms);

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
        while (rootDoorsToFill > 0 && GeneratedEntities.Count < RoomsToGenerate)
        {
            if (maxRetries < 0)
                break; 

            bool result = await ExtendRoom(root);
            if (result)
                rootDoorsToFill--;
            else
                maxRetries--;
        }

        // Populate will now only do one room then it will pick another room.
        while (GeneratedEntities.Count < RoomsToGenerate)
        {
            List<RoomMono> availableRooms = GeneratedEntities.Except(GeneratedWithoutAvailability).ToList();
            if (availableRooms.Count == 0)
            {
                Debug.LogError("Ran out of rooms with available doors before hitting the generate count.");
            }

            await ExtendRoom(availableRooms.Random().GetComponent<RoomMono>());
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
        room.transform.position = room.transform.GetBoundsInt().position;

        // Select a random door.
        GameObject door = GetRandomDoor(room);
        if (door == null)
        {
            return false;
        }

        // Get a new room to connect.
        RoomMono B = await InstantiateRoom(GetRandomRoomPrefab(room));

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
        this.Maze.DoorRegistry.SetConnection(door, B);

        // Change this to a solid too.
        B.transform.position = B.transform.GetBoundsInt().position;

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
        roomB.transform.rotation = Quaternion.Euler(0, 0, 0);

        // Adjust the room rotation so roomA and roomB can properly connect.
        float yRot = GetRoomRotationBasedOnDoor(roomADoor, roomBDoor);
        roomB.transform.rotation = Quaternion.Euler(0, yRot, 0);

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

    private static int GeneratedDoorsGlobal = 0;

    /// <summary>
    /// Create a new <see cref="RoomMono"/> object.
    /// </summary>
    /// <param name="prefab"></param>
    /// <returns></returns>
    private async Task<RoomMono> InstantiateRoom(RoomMono prefab)
    {
        if (prefab == null) throw new System.ArgumentNullException(nameof(prefab));

        RoomMono baked = Instantiate(prefab, Vector3.zero, prefab.transform.rotation, transform);
        baked.name = "Room" + GeneratedEntities.Count;

        RoomMono room = baked.GetComponent<RoomMono>();

        // Allow the room to generate.
        await room.Generate(null);

        // Wait until room generation is complete.
        while (!room.GenerateFinished)
        {
            await Task.Delay(10);
        }

        // Add it's doors into the registry.
        var doors = room.Doors;
        foreach (Transform door in doors)
        {
            door.name = $"DOOR-{GeneratedEntities.Count}-{GeneratedDoorsGlobal}";
            this.Maze.DoorRegistry.Add(door.gameObject, room);

            GeneratedDoorsGlobal = GeneratedDoorsGlobal + 1;
        }

        // Add to generated rooms.
        this.GeneratedEntities.Add(room);

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

        if (!this.GeneratedEntities.Contains(roomA))
            throw new System.NullReferenceException("roomA does not exist in GeneratedRooms list");

        // Remove.
        GeneratedEntities.Remove(roomA);

        // Remove bounds.
        this.Maze.Grid.ClearBounds(roomA.transform.BoundingBox(), roomA.transform.position.RoundToInt());

        // Destroy.
        DestroyImmediate(roomA.gameObject);

        return true;
    }

    /// <summary>
    /// Returns a random room that should be generated.
    /// </summary>
    /// <returns></returns>
    private RoomMono GetRandomRoomPrefab(RoomMono exclude)
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
            return GenericRoomPrefabs.Except(new RoomMono[] { exclude }).ToList().Random();
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

        return this.Maze.Grid.SetRoomCells(room);
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

        List<DoorPair> doors = this.Maze.DoorRegistry.GetAvailable(room);
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
            var roomBDoors = this.Maze.DoorRegistry.GetAvailable(roomB);

            if (roomBDoors.Count == 0)
                throw new System.InvalidOperationException("Failed to find any doors for this room.");

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