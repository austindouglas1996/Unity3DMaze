using System;
using UnityEngine;

/// <summary>
/// A pathfinding controller to help navigate a <see cref="CharacterMovementController"/> to go to and grab items
/// around in the maze. This controller will cancel actions, navigate, and pickup and item before restoring access.
/// </summary>
[RequireComponent(typeof(CharacterMovementController))]
public class ItemMovementController : MonoBehaviour
{
    [Tooltip("Used to help control the inventory trigger which contains item in the character range.")]
    [SerializeField] private EntityInventoryTrigger inventoryTrigger;

    [Tooltip("Used to help control the inventory of the character.")]
    [SerializeField] private EntityItemInventory inventory;

    [Tooltip("Should the item when picked up become the selected item for the character.")]
    [SerializeField] private bool SelectedOnPickup = true;

    /// <summary>
    /// The movement controller for the character.
    /// </summary>
    private CharacterMovementController movementController;

    /// <summary>
    /// The items we're looking to grab.
    /// </summary>
    private PocketableItem ChosenItem;

    /// <summary>
    /// Tells whether we're monitoring finish/cancel actions.
    /// </summary>
    private bool IsActive = false;

    /// <summary>
    /// Head for an grab an item for a <see cref="CharacterMovementController"/>.
    /// </summary>
    /// <param name="item"></param>
    public void GrabItem(PocketableItem item)
    {
        // Cancel any active actions
        this.movementController.Cancel();

        this.ChosenItem = item;
        this.movementController.MoveTo(this.ChosenItem.transform.position.RoundToInt());

        // Set to active so we monitor finish/cancel actions.
        this.IsActive = true;
    }

    /// <summary>
    /// Setup components and confirm variables are set.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    private void Start()
    {
        this.movementController = GetComponent<CharacterMovementController>();

        if (this.inventory == null)
            throw new ArgumentNullException("Inventory is null.");

        if (this.inventoryTrigger == null)
            throw new ArgumentNullException("Inventory trigger is null.");

        this.movementController.Finished += MovementController_Finished;
        this.movementController.Cancelled += MovementController_Cancelled;
    }

    /// <summary>
    /// Called when we an action is cancelled. This can be called without
    /// finishing an action we started. So a small check to make sure we 
    /// are active.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MovementController_Cancelled(object sender, EventArgs e)
    {
        if (!this.IsActive)
            return;

        this.CleanUp();
    }

    /// <summary>
    /// Called when we reached our destination. This can be called without 
    /// finishing an action we started. So a small check to make sure we are
    /// active.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MovementController_Finished(object sender, EventArgs e)
    {
        if (!this.IsActive)
            return; 

        this.inventory.Add(ChosenItem, SelectedOnPickup);

        this.CleanUp();
    }

    /// <summary>
    /// Cleanup the environment from a finish/cancel action.
    /// </summary>
    private void CleanUp()
    {
        // This way we stop confusion later.
        ChosenItem = null;

        // No longer need to monitor events.
        this.IsActive = false;
    }
}