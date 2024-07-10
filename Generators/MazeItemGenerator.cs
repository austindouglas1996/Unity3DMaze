using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using VHierarchy.Libs;

[RequireComponent(typeof(MazeController))]
public class MazeItemGenerator : MonoBehaviour, IGenerator<PocketableItem>
{
    [Tooltip("List of prefabs to select from when generating items.")]
    [SerializeField] private List<PocketableItem> SellablePrefabs = new List<PocketableItem>();

    [Tooltip("Minimum sell value if all items picked.")]
    [SerializeField] private int MinItems = 10;

    [Tooltip("Maximum sell value if all items picked.")]
    [SerializeField] private int MaxItems = 65;

    /// <summary>
    /// Gets a list of prefabs that have not been used yet.
    /// </summary>
    private List<PocketableItem> SellablePrefabsNotUsed = new List<PocketableItem>();

    /// <summary>
    /// Gets a list of prefabs that have been used.
    /// </summary>
    private List<PocketableItem> SellablePrefabsUsed = new List<PocketableItem>();

    /// <summary>
    /// Gets the controller of this generator.
    /// </summary>
    private MazeController Maze;

    /// <summary>
    /// Returns whether <see cref="Generate"/> has been called.
    /// </summary>
    public bool GenerateCalled { get; private set; }

    /// <summary>
    /// Returns whether <see cref="Generate"/> has finished.
    /// </summary>
    public bool GenerateFinished { get; private set; }

    /// <summary>
    /// Returns a list of items generated.
    /// </summary>
    public List<PocketableItem> Generated { get; private set; } = new List<PocketableItem>();

    /// <summary>
    /// Generate items within this maze.
    /// </summary>
    /// <returns></returns>
    public async Task Generate()
    {
        if (this.GenerateCalled) return;
        this.GenerateCalled = true;

        if (SellablePrefabsNotUsed.Count == 0) return;

        ResetNotUsedPrefabsList();
        await AddUntilSatisified();

        this.GenerateFinished = true;
    }

    /// <summary>
    /// Destroy and reset this generator.
    /// </summary>
    /// <returns></returns>
    public async Task ResetGenerator()
    {
        foreach (PocketableItem item in Generated)
        {
            item.Destroy();
        }

        this.GenerateCalled = false;
        this.GenerateFinished = false;
    }

    /// <summary>
    /// Ran once.
    /// </summary>
    private void Start()
    {
        Maze = this.GetComponent<MazeController>();
    }

    /// <summary>
    /// Add <see cref="PocketableItem"/> until we reach <see cref="MaxValue"/>.
    /// </summary>
    /// <returns></returns>
    private async Task AddUntilSatisified()
    {
        int totalItems = 0;
        int generatedItems = UnityEngine.Random.Range(MinItems, MaxItems);

        while (totalItems < generatedItems)
        {
            PocketableItem newItem = GetRandomPrefab();
            Tuple<RoomMono, Vector3> newItemInfo = await GetRandomPosition(newItem);

            // Make sure a position was able to be found.
            if (newItemInfo == null)
                continue;

            PocketableItem generatedItem = InstantiatePocketableItem(newItem, newItemInfo.Item2, newItemInfo.Item1);

            while (!generatedItem.GenerateFinished)
            {
                await Task.Delay(3);
            }

            // Add.
            this.Generated.Add(newItem);

            // Increase count.
            totalItems++;
        }
    }

    /// <summary>
    /// Grab a random <see cref="RoomMono"/> from <see cref="MazeRoomGenerator"/> or <see cref="MazeHallwayGenerator"/>
    /// </summary>
    /// <returns></returns>
    private RoomMono GetRandomRoom()
    {
        int maxTries = 10;
        RoomMono selectedRoom = null;

        while (selectedRoom == null)
        {
            if (RandomHelper.Chance(50))
            {
                selectedRoom = this.Maze.Rooms.Generated.Random();
            }
            else
            {
                selectedRoom = this.Maze.Hallways.Generated.Random();
            }

            if (selectedRoom.IsDestroyed())
                selectedRoom = null;

            maxTries--;
        }

        return selectedRoom;
    }

    /// <summary>
    /// Grab a random prefab from <see cref="SellablePrefabsNotUsed"/>.
    /// </summary>
    /// <returns></returns>
    private PocketableItem GetRandomPrefab()
    {
        PocketableItem selectedItem = SellablePrefabsNotUsed.Random();
        SellablePrefabsUsed.Add(selectedItem);

        if (SellablePrefabsNotUsed.Count == 0)
        {
            ResetNotUsedPrefabsList();
        }

        return selectedItem;
    }

    /// <summary>
    /// Reset the list of <see cref="SellablePrefabsNotUsed"/> since we have used all prefabs at least once.
    /// </summary>
    private void ResetNotUsedPrefabsList()
    {
        SellablePrefabsNotUsed = new List<PocketableItem>(SellablePrefabs);
        SellablePrefabsUsed.Clear();
    }

    /// <summary>
    /// Create a new <see cref="PocketableItem"/> from a prefab, with a position in a <see cref="RoomMono"/>.
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="LocalPosition"></param>
    /// <param name="owner"></param>
    /// <returns></returns>
    private PocketableItem InstantiatePocketableItem(PocketableItem prefab, Vector3 LocalPosition, RoomMono owner)
    {
        PocketableItem newItem = Instantiate(prefab, LocalPosition, Quaternion.identity, owner.transform);
        newItem.Name = "SELLABLE";

        return newItem;
    }

    /// <summary>
    /// Grab a random position from a <see cref="RoomMono"/> based on if the item is heavy will return the floor, or roof.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    private async Task<Tuple<RoomMono, Vector3>> GetRandomPosition(PocketableItem item)
    {
        PocketableItem holdSettings = item.GetComponent<PocketableItem>();

        // if the item requires two hands we want to put somewhere safe.
        // if else we want it to just fall.
        if (holdSettings.RequiresTwoHands)
        {
            return await GetRandomFloorPosition(item);
        }
        else
        {
            return await GetRandomRoofPosition(item);
        }
    }

    /// <summary>
    /// Grab a random position from a <see cref="RoomMono"/> from a floor tile.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    private async Task<Tuple<RoomMono, Vector3>> GetRandomFloorPosition(PocketableItem item)
    {
        RoomMono room = GetRandomRoom();
        Transform selectedFloor = (await GetRandomRoom().GetChildrenByPieceType(RoomFixtureIdentityType.Floor)).Random(); 
        
        if (selectedFloor == null)
            return null;

        return new Tuple<RoomMono, Vector3>(room, selectedFloor.position + item.transform.BoundingBox(true).size);
    }

    /// <summary>
    /// Grab a random position from a <see cref="RoomMono"/> from a roof tile.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    private async Task<Tuple<RoomMono, Vector3>> GetRandomRoofPosition(PocketableItem item)
    {
        RoomMono room = GetRandomRoom();
        Transform selectedFloor = (await room.GetChildrenByPieceType(RoomFixtureIdentityType.Roof)).Random();

        if (selectedFloor == null)
            return null;

        return new Tuple<RoomMono, Vector3>(room, selectedFloor.position - item.transform.BoundingBox(true).size);
    }
}