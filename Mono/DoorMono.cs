using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Helps define the state of a door.
/// </summary>
[System.Serializable]
public enum DoorState
{
    /// <summary>
    /// Door is currently closed.
    /// </summary>
    Closed,

    /// <summary>
    /// Door is currently in the process of closing.
    /// </summary>
    Closing,

    /// <summary>
    /// Ddoor is current open.
    /// </summary>
    Open,

    /// <summary>
    /// Door is currently in the process of opening.
    /// </summary>
    Opening,

    /// <summary>
    /// Door is locked.
    /// </summary>
    Locked
}

public class DoorMono : MonoBehaviour
{
    /// <summary>
    /// The speed doors open at.
    /// </summary>
    private readonly float OpenAndCloseSpeed = 9.5f;

    [Tooltip("The period to wait until resetting the input value. Set this as low as possible.")]
    [SerializeField] private float inputDelay = 150f;

    [Tooltip("The rotation that the door should swing in, normally 90 or -90.")]
    [SerializeField] public float GoalRotation = 90;

    [Tooltip("Related doors, this is literally only for handling double doors to make sure they play nice.")]
    [SerializeField] public List<DoorMono> RelatedDoors = new List<DoorMono>();

    [Tooltip("Is the door locked and unable to be opened.")]
    [SerializeField] public bool IsLocked = true;

    /// <summary>
    /// The current door rotation.
    /// </summary>
    private float CurrentRotation = 0;

    /// <summary>
    /// The minimum rotation.
    /// </summary>
    private float RotationMin = 0;

    /// <summary>
    /// The maximum rotation.
    /// </summary>
    private float RotationMax = 0;

    /// <summary>
    /// The current state of the door. Before I was using a bool for IsOpen which was SUPER CONFUSING
    /// </summary>
    private DoorState CurrentState = DoorState.Closed;

    /// <summary>
    /// The delay remaining between when the player can touch the door.
    /// </summary>
    private float InputDelayRemaining = 0;

    /// <summary>
    /// Change the state of the door lock.
    /// </summary>
    /// <param name="newState"></param>
    /// <returns></returns>
    public void ChangeDoorLockState(bool newState)
    {
        this.IsLocked = newState;
    }

    /// <summary>
    /// Performs the door action whether that is opening or closing.
    /// </summary>
    /// <returns></returns>
    public bool PerformAction(bool childDoor = false)
    {
        if (!childDoor)
        {
            foreach (DoorMono go in RelatedDoors)
                go.PerformAction(true);
        }

        // Already running an action.
        if (CurrentState == DoorState.Closing || CurrentState == DoorState.Opening)
            return false;

        if (CurrentState == DoorState.Closed)
        {
            if (IsLocked)
            {
                StartCoroutine(ShakeDoor());
                return false;
            }

            CurrentState = DoorState.Opening;
        }
        else if (CurrentState == DoorState.Open)
        {
            CurrentState = DoorState.Closing;
        }

        // Reset input delay.
        InputDelayRemaining = inputDelay;

        return true;
    }

    /// <summary>
    /// Called on initialization.
    /// </summary>
    private void Start()
    {
        RotationMin = 0;
        RotationMax = GoalRotation;
    }

    /// <summary>
    /// Update the door animation.
    /// </summary>
    private void Update()
    {
        if (CurrentState != DoorState.Closing && CurrentState != DoorState.Opening)
        {
            if (InputDelayRemaining > 0)
                InputDelayRemaining--;

            return;
        }

        float newRotation = CurrentRotation;

        if (CurrentState == DoorState.Opening)
        {
            // -90 handling.
            if (RotationMin > RotationMax)
            {
                newRotation -= OpenAndCloseSpeed;
            }
            else
            {
                newRotation += OpenAndCloseSpeed;
            }

            if (newRotation >= RotationMax)
            {
                newRotation = RotationMax;
                CurrentState = DoorState.Open;
            }
        }
        else if (CurrentState == DoorState.Closing)
        {
            // -90 handling.
            if (RotationMin > RotationMax)
            {
                newRotation += OpenAndCloseSpeed;
            }
            else
            {
                newRotation -= OpenAndCloseSpeed;
            }

            if (newRotation <= RotationMin)
            {
                newRotation = RotationMin;
                CurrentState = DoorState.Closed;
            }
        }

        this.transform.rotation = Quaternion.Euler(this.transform.rotation.x, newRotation, this.transform.rotation.z);

        // Issue with unity going from 0 to 180 and 0 to 270.
        if (this.transform.localEulerAngles.y != newRotation)
            this.transform.localEulerAngles = new Vector3(this.transform.localEulerAngles.x, newRotation, this.transform.localEulerAngles.z);

        CurrentRotation = newRotation;
    }

    // Coroutine to shake the door
    private IEnumerator ShakeDoor()
    {
        Vector3 originalPosition = transform.position;
        float duration = 0.5f;  // Duration of the shake
        float magnitude = 0.03f; // Magnitude of the shake

        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = originalPosition.x + Random.Range(-1f, 1f) * magnitude;
            float y = originalPosition.y + Random.Range(-1f, 1f) * magnitude;

            transform.position = new Vector3(x, y, originalPosition.z);

            elapsed += Time.deltaTime;

            yield return null;
        }

        transform.position = originalPosition;
    }
}