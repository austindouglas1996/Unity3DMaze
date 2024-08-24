using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CraneInventoryTrigger : MonoBehaviour
{
    [SerializeField] public CraneMachineController controller;

    public virtual void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Item")
        {
            controller.Inventory.Add(other.gameObject.GetComponent<PocketableItem>());
            controller.GrabbedItem = true;
        }
    }
}