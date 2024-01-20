using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(EntityItemInventory))]
public abstract class EntityInventoryTrigger : MonoBehaviour
{
    public EntityItemInventory Inventory
    {
        get { return _inventory; } 
        protected set { _inventory = value; }
    }
    private EntityItemInventory _inventory;


    public List<PocketableItem> ItemsInRange = new List<PocketableItem>();

    public virtual void Start()
    {
        Inventory = this.GetComponent<EntityItemInventory>();
    }

    public virtual void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Item")
        {
            ItemsInRange.Add(other.gameObject.GetComponent<PocketableItem>());
        }
    }

    public virtual void OnTriggerExit(Collider other)
    {
        if (other.tag == "Item")
        {
            ItemsInRange.Remove(other.gameObject.GetComponent<PocketableItem>());
        }
    }
}