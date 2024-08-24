using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using VHierarchy.Libs;

[RequireComponent(typeof(MazeController))]
public class LootMazeGenerator : MazeGenerator<PocketableItem>
{
    [Tooltip("List of prefabs to select from when generating items.")]
    [SerializeField] public List<PocketableItem> SellablePrefabs = new List<PocketableItem>();

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

    protected override async Task OnGenerate(object[] args)
    {
        ResetNotUsedPrefabsList();
        await AddUntilSatisified();
    }

    protected override Task OnResetGenerator()
    {
        foreach (PocketableItem item in GeneratedEntities)
        {
            item.Destroy();
        }

        return Task.CompletedTask;
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
            this.GeneratedEntities.Add(newItem);

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
            selectedRoom = this.Maze.Grid.Cells.Random().Room;

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
        Transform selectedFloor = (await GetRandomRoom().GetChildrenByPieceType(RoomFixtureStructureType.Floor)).Random();

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
        Transform selectedFloor = (await room.GetChildrenByPieceType(RoomFixtureStructureType.Roof)).Random();

        if (selectedFloor == null)
            return null;

        return new Tuple<RoomMono, Vector3>(room, selectedFloor.position - item.transform.BoundingBox(true).size);
    }
}