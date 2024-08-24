using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    [Tooltip("Action key to perform the door action.")]
    [SerializeField] public KeyCode keyToPress = KeyCode.E;

    /// <summary>
    /// The door control to perform the action for.
    /// </summary>
    private DoorMono doorControl = null;

    /// <summary>
    /// Tells whether the player is in the proximity.
    /// </summary>
    private bool playerInsideTrigger;

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "DoorOpenable")
        {
            playerInsideTrigger = true;
            doorControl = other.GetComponent<DoorMono>();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.tag == "DoorOpenable")
        {
            playerInsideTrigger = false;
            doorControl = null;
        }
    }

    void Update()
    {
        if (playerInsideTrigger && Input.GetKeyDown(keyToPress))
        {
            doorControl.PerformAction();
        }

        if (playerInsideTrigger && Input.GetKeyDown(KeyCode.K))
        {
            doorControl.ChangeDoorLockState(false);
        }

        if (playerInsideTrigger && Input.GetKeyDown(KeyCode.L))
        {
            if (doorControl.CurrentState == DoorState.Open)
                Debug.Log("Door is open. Cannot lock.");

            doorControl.ChangeDoorLockState(true);
        }
    }
}