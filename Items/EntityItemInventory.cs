using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Rendering.LookDev;
using UnityEngine;

[RequireComponent(typeof(EntityInventoryTrigger))]
public class EntityItemInventory : MonoBehaviour
{
    /// <summary>
    /// The currently selected item index.
    /// </summary>
    private int SelectedItemIndex { get; set; } = -1;

    [Tooltip("How much health the entity has.")]
    [SerializeField] public int InventorySize = 8;

    [Tooltip("Does this item slow down the player and not allow them to make jumps easily?")]
    [SerializeField] public bool DropItemsOnDeath = true;

    /// <summary>
    /// Event called when the selected inventory item is changed.
    /// </summary>
    public OnHoldableEntityChange SelectedChange;

    /// <summary>
    /// Gets a read-only list of items that this entity holds.
    /// </summary>
    public IReadOnlyList<PocketableItem> HeldItems
    {
        get { return Items; }
    }
    private List<PocketableItem> Items = new List<PocketableItem>();

    /// <summary>
    /// Gets the currently held item by the entity.
    /// </summary>
    public PocketableItem SelectedItem
    {
        get 
        {
            if (SelectedItemIndex == -1 || SelectedItemIndex > Items.Count - 1)
            {
                return null;
            }

            return Items[SelectedItemIndex]; 
        }
    }

    /// <summary>
    /// Hide the currently selected item.
    /// </summary>
    public void HideSelected()
    {
        SetSelected(-1);
    }

    /// <summary>
    /// Select the next item in the entity inventory.
    /// </summary>
    public void SelectNext()
    {
        if (SelectedItemIndex + 1 < HeldItems.Count)
        {
            SetSelected(SelectedItemIndex + 1);
        }
        else
        {
            SetSelected(0);
        }
    }

    /// <summary>
    /// Select the previous item in the entity inventory.
    /// </summary>
    public void SelectBefore()
    {
        if (SelectedItemIndex -1 >= 0)
        {
            SetSelected(SelectedItemIndex - 1);
        }
        else
        {
            SetSelected(Items.Count - 1);
        }
    }

    /// <summary>
    /// Add a new item to the entity inventory.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="entityHolding"></param>
    public void Add(PocketableItem item, bool entityHolding = false)
    {
        if (InventorySize <= Items.Count)
            return;

        item.gameObject.SetActive(!entityHolding);
        item.GetComponent<Collider>().enabled = false;
        item.GetComponent<Rigidbody>().isKinematic = true;
        item.transform.rotation = item.OffsetRotation;
        item.transform.parent = this.transform;
        //item.transform.localPosition = Entity.HeldItemPosition + item.OffsetPosition;
        Items.Add(item);

        // Set as selected item.
        SetSelected(Items.Count - 1);
    }

    /// <summary>
    /// Drop an item from the entity inventory.
    /// </summary>
    /// <param name="item"></param>
    public void Drop(PocketableItem item)
    {
        Drop(Items.IndexOf(item));
    }

    /// <summary>
    /// Drop an item from the entity inventory by index.
    /// </summary>
    /// <param name="index"></param>
    /// <exception cref="System.IndexOutOfRangeException"></exception>
    public void Drop(int index)
    {
        if (Items.Count == 0) return;
        if (index > Items.Count || index < 0)
        {
            throw new System.IndexOutOfRangeException("Indes cannot be larger than collection or smaller than zero.");
        }

        PocketableItem item = Items[index];
        Items.RemoveAt(index);
        item.gameObject.SetActive(true);
        item.transform.parent = null;
        item.GetComponent<Collider>().enabled = true;
        item.GetComponent<Rigidbody>().isKinematic = false;

        if (SelectedItemIndex == index)
        {
            if (SelectedItemIndex == 0 && Items.Count > 0)
                SelectNext();
            else if (SelectedItemIndex == Items.Count &&  Items.Count > 0)
                SelectBefore();
        }
    }

    /// <summary>
    /// Called on start.
    /// </summary>
    private void Start()
    {
        //Entity = GetComponent<ControllableEntity>();
    }

    /// <summary>
    /// Sets the currently selected index of inventory item.
    /// </summary>
    /// <param name="newIndex"></param>
    private void SetSelected(int newIndex)
    {
        PocketableItem ItemInView = SelectedItem;

        // Make sure there is an item in view.
        if (ItemInView != null)
        {
            ItemInView.gameObject.SetActive(false);
        }

        // Update index.
        SelectedItemIndex = newIndex;

        // Don't continue if the current 
        if (SelectedItemIndex == -1)
        {
            return;
        }

        // Change to the new item.
        SelectedItem.gameObject.SetActive(true);

        // Invoke.
        OnSelectedChange(SelectedItem);
    }

    /// <summary>
    /// Calls <see cref="SelectedChange"/> event.
    /// </summary>
    /// <param name="entity"></param>
    private void OnSelectedChange(PocketableItem entity)
    {
        if (SelectedChange != null)
        {
            SelectedChange.Invoke(entity);
        }
    }
}