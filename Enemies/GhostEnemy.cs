using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;

/// <summary>
/// Represents the core personality of a ghost. The ghost controls the types of actions the ghost does.
/// </summary>
[System.Serializable]
public enum GhostCorePersonality
{
    /// <summary>
    /// This type of ghost will naturally wander through the maze.
    /// Will not get naturally angry, but will randomly move objects, open/close doors
    /// and try to scare the player.
    /// </summary>
    Wandering,

    /// <summary>
    /// This type of ghost will fall in low with a 'player'. The selected
    /// player will be followed. Randomly given gifts. If the player rejects
    /// enough of the gifts. The ghost becomes an aggressive isolated.
    /// </summary>
    Loving,

    /// <summary>
    /// This type of ghost is slower and angry. The ghost will throw items
    /// at the player and tend to stay in the favorite room.
    /// </summary>
    Aggressive,

    /// <summary>
    /// This type of ghost was originally a lover. The ghost had too many
    /// gifts rejected so now it's an angry ghost. The ghost will stay in
    /// its favorite room and occasionaly roam to grab more items. The ghost
    /// will defend its items by throwing items at players who enter.
    /// </summary>
    AggressiveBetrayed
}

/// <summary>
/// Represents the current state of the <see cref="GhostEnemy"/> and what it is doing.
/// </summary>
[System.Serializable]
public enum GhostState
{
    Wandering,
    Guarding,
    Transforming,
    Tantrum,
    PickingItem,
    TravelingToPlayer
}

[RequireComponent(typeof(Shake))]
[RequireComponent(typeof(LightController))]
public class GhostEnemy : CharacterMovementController
{
    [SerializeField] public GameObject Player;
    [SerializeField] public MazeController Maze;
    [SerializeField] private EntityItemInventory Inventory;

    [Header("Personality")]
    [SerializeField] private GhostCorePersonality Personality = GhostCorePersonality.Wandering;
    [SerializeField] private GhostState State = GhostState.Wandering;

    [Header("Angry")]
    [SerializeField] private float MinimumTantrumInSeconds = 30;
    [SerializeField] private float MaximumTantrumInSeconds = 60;
    [SerializeField] private float TimeBetweenTantrumInSeconds = 15;
    private float RemainingTantrumTime = -1;
    private float RemainingTimeBeforeNextTantrum = -1;

    [Header("Effects")]
    [SerializeField] private GameObject AngryTransformState;
    private Shake shake;
    private LightController lightController; 
    private Vector3 initialPosition;

    [Header("Debug options")]
    [Tooltip("For debug options. Not to be set")]
    [SerializeField] private RoomMono FavoriteRoom;
    [SerializeField] private bool DeterminePath = false;
    [SerializeField] private bool Stop = false;


    /// <summary>
    /// Initialize components and set events.
    /// </summary>
    /// <returns></returns>
    protected override async Task Start()
    {
        while (!this.Maze.GenerateFinished)
        {
            await Task.Delay(1000);
        }

        FavoriteRoom = Maze.Rooms.Generated.Random();
        shake = this.GetComponent<Shake>();
        lightController = this.GetComponent<LightController>();
        Inventory = this.GetComponent<EntityItemInventory>();
        initialPosition = this.transform.localPosition;

        base.Finished += GhostEnemy_Finished;
        await base.Start();
    }

    /// <summary>
    /// Update the current actions the ghost is currently performing.
    /// </summary>
    /// <returns></returns>
    protected override async Task Update()
    {
        while (!this.Maze.GenerateFinished)
        {
            await Task.Delay(1000);
        }

        if (Stop)
        {
            this.Cancel();
            return;
        }

        if (DeterminePath)
        {
            DeterminePath = false;
            DetermineNewPath();
        }

        foreach (var sway in this.GetComponentsInChildren<ItemSway>())
        {
            if (this.IsMoving)
                sway.SetSway(this.transform.position.RoundToInt(), this.MovingTo.Position);
            else
                sway.ClearSway();
        }

        if (this.Personality == GhostCorePersonality.Wandering)
        {
            this.UpdateWander();
        }
        else if (this.Personality == GhostCorePersonality.Aggressive)
        {
            this.UpdateAggressive();
        }
        else if (this.Personality == GhostCorePersonality.Loving)
        {
            this.UpdateLover();
        }
        else if (this.Personality == GhostCorePersonality.AggressiveBetrayed)
        {
            this.UpdateAggressiveBetrayed();
        }

        await base.Update();
    }

    /// <summary>
    /// Update the wonder personality. Like walking around.
    /// </summary>
    protected virtual void UpdateWander()
    {
        if (!this.IsMoving && Random.Range(1,100) == 99)
        {
            // Update state.
            this.State = GhostState.Wandering;

            this.DetermineNewPath();
        }
    }

    /// <summary>
    /// Update the aggresive personality. Choosing when to throw a tantrum
    /// or reduce the time between the next tantrums.
    /// </summary>
    protected virtual void UpdateAggressive()
    {
        // Reduce tantrum time if applicable.
        if (RemainingTimeBeforeNextTantrum > 0)
        {
            RemainingTimeBeforeNextTantrum -= Time.deltaTime;
        }

        // Handle tantrum state.
        if (State == GhostState.Tantrum)
        {
            UpdateTantrum();
        }
        else
        {
            HandleNonTantrumState();
        }
    }

    /// <summary>
    /// Update the lover personality. Choosing between following a player,
    /// finding an item to them gift later.
    /// </summary>
    protected virtual void UpdateLover()
    {
        if (Random.Range(1, 500) > 450)
        {
            if (this.Inventory.HeldItems.Count > 0)
            {
                this.GiveItemToPlayer();
            }
            else
            {
                this.PickupItem();
            }
        }

    }

    /// <summary>
    /// Update the aggresive betrayal personality. Choosing between staying
    /// in the room aggresive, or travel outside the room to find items.
    /// </summary>
    protected virtual void UpdateAggressiveBetrayed()
    {
        if (Random.Range(1, 500) > 490)
        {
            // TODO: Go around maze and find item.
        }

        this.DetermineNewPath();

    }

    /// <summary>
    /// Event invoked when a <see cref="CellMovementController.MoveTo(Cell)"/> has finished.
    /// We use this to update states and perform sub-actions if required.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void GhostEnemy_Finished(object sender, System.EventArgs e)
    {
        if (this.State == GhostState.TravelingToPlayer)
        {
            EntityItemInventory inventory = this.GetComponent<EntityItemInventory>();
            inventory.HeldItems[0].IsRecentlyGifted = true;
            inventory.Drop(0);

            this.State = GhostState.Wandering;
        }
    }

    /// <summary>
    /// Update the tantrum of the ghost. Reducing the times during, after,
    /// and planning a tantrum.
    /// </summary>
    private void UpdateTantrum()
    {
        RemainingTantrumTime -= Time.deltaTime;

        // Check if the tantrum has ended.
        if (RemainingTantrumTime < 0)
        {
            RemainingTimeBeforeNextTantrum = TimeBetweenTantrumInSeconds;
            TransformPassive();
        }

        // Randomly determine a new path during tantrum.
        if (Random.Range(0, 100) == 99)
        {
            DetermineNewPath();
        }
    }

    /// <summary>
    /// Handle the state of the aggressive personality when not in a tantrum.
    /// </summary>
    private void HandleNonTantrumState()
    {
        bool canTantrum = CanTantrum();

        // Attempt to transform to angry state if conditions are met.
        if (canTantrum && Random.Range(0, 2000) == 1999 && GetCurrentCell().Type == CellType.Room)
        {
            TransformAngry();
        }
        else
        {
            UpdateWander();
        }
    }

    /// <summary>
    /// Transform the ghost into the aggresive state.
    /// </summary>
    private void TransformAngry()
    {
        base.Cancel();
        this.State = GhostState.Transforming;
        StartCoroutine(lightController.ChangeLightProperties());
        AngryTransformState.SetActive(true);
        shake.StartShaking(6f);
        this.IsRunning = true;
        this.State = GhostState.Tantrum;
        this.RemainingTantrumTime = Random.Range(MinimumTantrumInSeconds, MaximumTantrumInSeconds);
    }

    /// <summary>
    /// Transform the ghost into the passive state.
    /// </summary>
    private void TransformPassive()
    {
        this.State = GhostState.Transforming;
        StartCoroutine(lightController.RevertLightProperties());
        AngryTransformState.SetActive(false);
        shake.StopShaking();
        this.IsRunning= false; 
        this.State= GhostState.Wandering;
    }

    /// <summary>
    /// Pickup an item and place into the character inventory.
    /// </summary>
    private void PickupItem()
    {
        EntityInventoryTrigger available = this.GetComponentInChildren<EntityInventoryTrigger>();
        ItemMovementController itemController = this.GetComponent<ItemMovementController>();

        itemController.GrabItem(available.ItemsInRange.FindAll(r => r.IsRecentlyGifted == false).Random());
        this.State = GhostState.PickingItem;
    }

    /// <summary>
    /// Take one of the items we have (We can hold 1) and give it to the player.
    /// </summary>
    private void GiveItemToPlayer()
    {
        EntityItemInventory inventory = this.GetComponent<EntityItemInventory>();

        this.State = GhostState.TravelingToPlayer;

        this.MoveTo(this.Player.transform.position.RoundToInt());
    }

    /// <summary>
    /// Determine the next cell to travel too. Mostly used for aimless wondering.
    /// </summary>
    /// <exception cref="System.ArgumentNullException"></exception>
    private void DetermineNewPath()
    {
        Cell chosenSpot;

        // Explore outside the room?
        bool aggressiveBetrayed = Personality == GhostCorePersonality.AggressiveBetrayed && State == GhostState.PickingItem;

        if (!aggressiveBetrayed && Random.Range(1, 100) > 50)
        {
            chosenSpot = this.Maze.Grid.Cells.Random();
        }
        else
        {
            chosenSpot = this.Maze.Grid.Cells.Where(r => r.Room == FavoriteRoom).ToList().Random();
        }

        if (chosenSpot == null)
        {
            throw new System.ArgumentNullException("Failed to find an appropiate cell.");
        }

        this.MoveTo(chosenSpot);
    }

    /// <summary>
    /// Returns whether this ghost can throw a tanrum.
    /// </summary>
    /// <returns></returns>
    private bool CanTantrum()
    {
        return RemainingTimeBeforeNextTantrum < 0
            && RemainingTantrumTime < 0
            && State != GhostState.Tantrum
            && State != GhostState.Transforming;
    }
}