using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.MemoryProfiler;
using UnityEngine;

/// <summary>
/// Represents a connection between <see cref="RoomMono"/> instances to help with navigation.
/// </summary>
public class DoorPair
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DoorPair"/> class with the originating room and the connected room.
    /// </summary>
    /// <param name="door">The object that sits between the objects.</param>
    /// <param name="a">The originating point.</param>
    /// <param name="b">The point that is being connected to. Can be null</param>
    /// <exception cref="System.ArgumentNullException"></exception>
    public DoorPair(GameObject door, RoomMono A, RoomMono B = null)
    {
        this.Door = door;
        this.A = A;
        this.B = B;

        // 12/14/2023
        // Does this do anything? My git is broken, can't test. Too scared.
        door.GetComponent<RoomFixtureMono>().IsRegistered = true;
    }

    /// <summary>
    /// Gets the door for this pair.
    /// </summary>
    public GameObject Door;

    /// <summary>
    /// Returns the <see cref="Cell"/> that the door in <see cref="A"/> room is contained in.
    /// </summary>
    /// <remarks>This is added in <see cref="MazeController.CleanupDoors"/></remarks>
    public Cell ACell;

    /// <summary>
    /// Returns the <see cref="Cell"/> that the door in <see cref="B"/> roo mis contained in.
    /// </summary>
    /// <remarks>This is added in <see cref="MazeController.CleanupDoors"/></remarks>
    public Cell BCell;

    /// <summary>
    /// Gets the door for this pair.
    /// </summary>
    public RoomMono A;

    /// <summary>
    /// Gets the door for this pair.
    /// </summary>
    public RoomMono B;

    /// <summary>
    /// Returns the other room connected to this door.
    /// </summary>
    /// <param name="currentRoom">The current room.</param>
    /// <returns>The other room connected to the door.</returns>
    public RoomMono GetOtherRoom(RoomMono currentRoom)
    {
        if (currentRoom == A)
            return B;
        else if (currentRoom == B)
            return A;
        else
            Debug.Log("The specified room is not connected to this door.");

        return null;
    }

    /// <summary>
    /// Retrieve the other rooms cell.
    /// </summary>
    /// <param name="currentRoom"></param>
    /// <returns></returns>
    public Cell GetOtherCell(RoomMono currentRoom)
    {
        if (currentRoom == A)
            return BCell;
        else if (currentRoom == B)
            return ACell;
        else
            Debug.Log("The specified room is not connected to this door.");

        return null;
    }

    /// <summary>
    /// Checks if both rooms is connected.
    /// </summary>
    /// <returns></returns>
    public bool IsComplete()
    {
        return A != null && B != null;
    }

    /// <summary>
    /// Checks if the given room is connected to this door.
    /// </summary>
    /// <param name="room">The room to check.</param>
    /// <returns>True if the room is connected, otherwise false.</returns>
    public bool CheckConnection(RoomMono room)
    {
        return room == A || room == B;
    }
}

/// <summary>
/// Represents the manager for managing the connections between <see cref="RoomMono"/> and <see cref="HallwayMono"/> can provide helpful information when needing to find lost connections.
/// </summary>
public class DoorRegistry : MonoBehaviour
{
    /// <summary>
    /// A dictionary of doors with their connections.
    /// </summary>
    private Dictionary<GameObject, DoorPair> doors = new Dictionary<GameObject, DoorPair>();

    /// <summary>
    /// Do we need to update the current collection?
    /// </summary>
    private bool updateRequired = false;

    /// <summary>
    /// Returns the collection of doors this <see cref="DoorRegistry"/> knows about.
    /// </summary>
    public Dictionary<GameObject, DoorPair> Doors
    {
        get { return doors; }
    }

    /// <summary>
    /// Add a new door to the registry.
    /// </summary>
    /// <param name="door"></param>
    /// <param name="rooms"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public DoorPair Add(GameObject door, RoomMono A, RoomMono B = null)
    {
        if (doors.ContainsKey(door))
            throw new InvalidOperationException("A door already exists in this collection. Unregister the door, or update the collection.");

        DoorPair newPair = new DoorPair(door, A, B);
        doors.Add(door, newPair);

        updateRequired = true;
        return newPair;
    }

    /// <summary>
    /// Remove all instances of doors.
    /// </summary>
    public void Clear()
    {
        this.doors.Clear();
    }

    /// <summary>
    /// Return an instance of a door based on its position.
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public GameObject Get(Vector3Int position, float offset = 4f)
    {
        foreach (GameObject go in doors.Keys)
        {
            // Calculate the distance between the current GameObject's position and the given position
            if (Vector3.Distance(go.transform.position, position) <= offset)
            {
                return go;
            }
        }

        return null;
    }

    /// <summary>
    /// Grab an existing <see cref="DoorPair"/> entry based on a door. 
    /// </summary>
    /// <param name="door"></param>
    /// <returns></returns>
    public DoorPair Get(GameObject door)
    {
        if (door == null) return null;
        return doors[door];
    }

    /// <summary>
    /// Grab an array of <see cref="DoorPair"/> that contains a connection to this room.
    /// </summary>
    /// <param name="room"></param>
    /// <returns></returns>
    public List<DoorPair> Get(RoomMono room)
    {
        return doors.Values.Where(r => r.CheckConnection(room)).ToList();
    }

    /// <summary>
    /// Grabs an array of <see cref="DoorPair"/> where there is a maximum of one connection points.
    /// </summary>
    /// <returns></returns>
    public List<DoorPair> GetAvailable()
    {
        return doors.Values.Where(r => !r.IsComplete()).ToList();
    }

    /// <summary>
    /// Grab an array of <see cref="DoorPair"/> instance where there is a maximum of one connection points.
    /// </summary>
    /// <param name="room"></param>
    /// <returns></returns>
    public List<DoorPair> GetAvailable(RoomMono room)
    {
        var connections = doors.Values.Where(door => door.Door != null && door.A == room && door.B == null).ToList();
        updateRequired = doors.Values.Any(door => door.Door == null);
        return connections;
    }

    /// <summary>
    /// Grab an array of <see cref="DoorPair"/> instances where there is a minimum of two connection points.
    /// </summary>
    /// <param name="room"></param>
    /// <returns></returns>
    public List<DoorPair> GetComplete(RoomMono room)
    {
        return doors.Values.Where(r => r.CheckConnection(room) && r.IsComplete()).ToList();
    }

    /// <summary>
    /// Retrieve a list of <see cref="DoorPair"/> instances found where two <see cref="RoomMono"/> contain a connection.
    /// </summary>
    /// <param name="A"></param>
    /// <param name="B"></param>
    /// <returns>Null if not found.</returns>
    public List<DoorPair> GetConnections(RoomMono A, RoomMono B)
    {
        List<DoorPair> connections = new List<DoorPair>();

        foreach (DoorPair pair in Get(A))
        {
            if (pair.CheckConnection(B))
            {
                if (connections.Contains(pair)) continue;
                connections.Add(pair);
            }    
        }

        foreach (DoorPair pair in Get(B))
        {
            if (pair.CheckConnection(A))
            {
                if (connections.Contains(pair)) continue;
                connections.Add(pair);
            }
        }

        return connections;
    }

    /// <summary>
    /// Remove a <see cref="GameObject"/> door from the collection.
    /// </summary>
    /// <param name="door"></param>
    /// <returns></returns>
    public bool Remove(GameObject door)
    {
        updateRequired = true;
        return doors.Remove(door);
    }

    /// <summary>
    /// Set the collection point for an existing connection. This will override if it exists.
    /// </summary>
    /// <param name="door"></param>
    /// <param name="B"></param>
    public void SetConnection(GameObject door, RoomMono B)
    {
        doors[door].B = B; updateRequired = true;
    }

    /// <summary>
    /// Create a <see cref="DoorPairMono"/> object on known doors for visual help in finding connection issues.
    /// </summary>
    public void Debug(int level = 1)
    {
       foreach (var room in doors)
       {
            DoorPairMono mono = room.Key.AddComponent<DoorPairMono>();
            mono.A = room.Value.A;
            mono.B = room.Value.B == null ? null : room.Value.B;
            mono.Door = room.Key == null ? null : room.Key;

            CellMono aCell = mono.AddComponent<CellMono>();
            CellMono bCell = mono.AddComponent<CellMono>();

            aCell.Set(room.Value.ACell);
            bCell.Set(room.Value.BCell);

            mono.ACell = aCell;
            mono.BCell = bCell;

            if (level == 2)
            {
                Instantiate(MazeResourceManager.Instance.DebugCube, mono.Door.transform.position, mono.Door.transform.rotation, mono.A.transform);
            }
       }
    }

    /// <summary>
    /// Update the door collection to clear of invalid values.
    /// </summary>
    private void Update()
    {
        // No update is required.
        if (!updateRequired) return;

        // Remove destroyed keys.
        foreach (var key in doors.Keys.ToList())
        {
            if (key == null)
                doors.Remove(key);
        }

        // Check the doors to make sure there is no destroyed originating points.
        foreach (var door in new Dictionary<GameObject, DoorPair>(doors))
        {
            List<GameObject> removeConnection = new List<GameObject>();

            // If the originating point is gone, destroy the entire object.
            if (door.Value.A.IsDestroyed() || door.Value.A == null)
                removeConnection.Add(door.Key);

            foreach (var connection in removeConnection)
                Remove(connection);
        }
    }
}