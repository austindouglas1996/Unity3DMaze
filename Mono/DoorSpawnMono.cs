using System;
using UnityEngine;

[Serializable]
public enum DoorType
{
    Square,
    Rounded
}

/// <summary>
/// This class is a helper for generating a random door prefab. Once constructed this <see cref="DoorSpawnMono"/> will be destroyed and a new
/// door with an instance of <see cref="DoorMono"/> will be made. 
/// <remarks>This file may be best stored in /Props/Pickers.</remarks>
/// </summary>
public class DoorSpawnMono : MonoBehaviour
{
    [Tooltip("Helps determine what pool to choose door prefabs from.")]
    [SerializeField] private DoorType Type;

    [Tooltip("Chance a door will not be selected and instead be an open door.")]
    [SerializeField] private int NoDoorChance = 5;

    [Tooltip("Chance a door will not be selected and instead be an open door.")]
    [SerializeField] private int LockChance = 35;

    /// <summary>
    /// Called on initialization.
    /// </summary>
    private void Start()
    {
        GameObject newDoor = null;

        if (Type == DoorType.Square)
            newDoor = MazeResourceManager.Instance.Default.DoorsSquarePrefabs.Random();
        else if (Type == DoorType.Rounded)
            newDoor = MazeResourceManager.Instance.Default.DoorsRoundPrefabs.Random();

        Instantiate(newDoor, this.transform.position, this.transform.rotation, this.transform.parent);

        // Delete the old one.
        Destroy(this.gameObject);
    }
}