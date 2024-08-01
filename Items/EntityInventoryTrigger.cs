using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EntityInventoryTrigger : MonoBehaviour
{
    public List<PocketableItem> ItemsInRange = new List<PocketableItem>();

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