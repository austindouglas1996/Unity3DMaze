using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using VHierarchy.Libs;

[RequireComponent(typeof(MazeController))]
public class ScatterRoomMazeGenerator : MazeGenerator<RoomMono>
{
    [SerializeField] private Vector3 SizeInTiles = Vector3.one;
    [SerializeField] private int RoomsToGenerate = 10;

    [Tooltip("The root room to be used when generating the room maze. If this is null one will be selected by random.")]
    [SerializeField] private RoomMono RootPrefab = null;

    [Tooltip("The chance (0-100%) that a special room will be selected.")]
    [SerializeField] private int ChanceForSpecial = 0;

    [Tooltip("A collection of generic rooms. This contains rooms that should be chosen regularly throughout the maze.")]
    [SerializeField] private List<RoomMono> GenericRoomPrefabs = new List<RoomMono>();

    [Tooltip("A collection of special rooms. This contains rooms that may have special functionality, or larger than normal.")]
    [SerializeField] private List<RoomMono> SpecialRoomPrefabs = new List<RoomMono>();

    /// <summary>
    /// This will help us with a simple path to know when generating a room if there is space for it.
    /// </summary>
    private float RoomAreaRemaining = 0;

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
    /// Set variables for generation.
    /// </summary>
    private void Start()
    {
        RoomAreaRemaining = (SizeInTiles.x * SizeInTiles.y * SizeInTiles.z) * 4;
    }

    /// <summary>
    /// Geenerate the random maze.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    protected override async Task OnGenerate(object[] args)
    {
        while (GeneratedEntities.Count < RoomsToGenerate)
        {
            RoomMono lastRoom = GeneratedEntities.Count > 0 ? GeneratedEntities.Last() : null;
            RoomMono newRoom = await InstantiateRoom(GetRandomRoomPrefab(lastRoom));
            newRoom.transform.position = GetRandomPositionInSpace();

            /*
             * NOTICE: THIS MAY CAUSE ISSUES. I wanted random room rotation.
             * I can't find anything in past code with how we rotated rooms. I know we rotated rooms once
             * did I remove rotation for a reason? 
             */
            List<float> randomRot = new List<float>() { 0, 90, 180 };
            newRoom.transform.rotation = Quaternion.Euler(0, randomRot.Random(), 0);

            // Align.
            AlignRoomToGrid(newRoom.gameObject);

            if (!IsRoomSizeAcceptable(newRoom))
            {
                RemoveRoom(newRoom);
                continue;
            }
            else
            {
                while (CheckForCollision(newRoom))
                {
                    newRoom.name += "_COLLISION";
                    newRoom.transform.position += new Vector3(4f, 4f, 4f);
                }
            }
        }

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
    /// Reset the generator contents. Removing anything this generator created like the rooms.
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
            this.Maze.DoorRegistry.Add(door.gameObject, room);
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
    /// 
    /// </summary>
    /// <param name="room"></param>
    /// <returns></returns>
    private bool IsRoomSizeAcceptable(RoomMono room)
    {
        Vector3 roomSize = RoomSizeInTiles(room);
        float area = roomSize.x * roomSize.z;

        if (area > RoomAreaRemaining)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Finds a random position in the world space we can use for a possible position.
    /// </summary>
    /// <returns></returns>
    private Vector3 GetRandomPositionInSpace()
    {
        // Calculate the size of the space in world units by multiplying by 4 (tile size)
        float maxX = SizeInTiles.x * 4f;
        float maxY = SizeInTiles.y * 4f;
        float maxZ = SizeInTiles.z * 4f;

        // Generate random positions within the specified bounds and round to the nearest multiple of 4
        float randomX = Mathf.Floor(Random.Range(0, maxX) / 4f) * 4f;
        float randomY = Mathf.Floor(Random.Range(0, maxY) / 4f) * 4f;
        float randomZ = Mathf.Floor(Random.Range(0, maxZ) / 4f) * 4f;

        return new Vector3(randomX, randomY, randomZ);
    }

    /// <summary>
    /// A problem specific to scatterRoom. Room sizes.
    /// </summary>
    /// <param name="room"></param>
    private void AlignRoomToGrid(GameObject room)
    {
        // Assuming the room has a collection of floor tiles, find one to use as the reference
        Transform referenceTile = room.transform.FindFirstChildByLayer(LayerMask.NameToLayer("Floor"));

        if (referenceTile != null)
        {
            Vector3 tilePosition = referenceTile.position;

            // Calculate the offset needed to align the tile to the grid
            float offsetX = Mathf.Round(tilePosition.x / 4f) * 4f - tilePosition.x;
            float offsetY = Mathf.Round(tilePosition.y / 4f) * 4f - tilePosition.y;
            float offsetZ = Mathf.Round(tilePosition.z / 4f) * 4f - tilePosition.z;

            Debug.LogWarning($"{room.name} Offset: {offsetX},{offsetY}, {offsetZ}");

            // Apply this offset to the entire room
            room.transform.position += new Vector3(offsetX, offsetY, offsetZ);
        }
        else
        {
            Debug.LogWarning("No floor tile found in the room to use for alignment.");
        }
    }

    /// <summary>
    /// Returns the size of a room in tiles.
    /// </summary>
    /// <param name="room"></param>
    /// <returns></returns>
    private Vector3 RoomSizeInTiles(RoomMono room)
    {
        Vector3 size = room.transform.BoundingBox().size;
        return size;
    }
}