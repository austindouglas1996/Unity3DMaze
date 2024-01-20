using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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
    public DoorPair(GameObject door, RoomMono a, RoomMono b)
    {
        if (door == null || a == null)
            throw new System.ArgumentNullException("Door or A is null.");

        Door = door;
        A = a;
        B = b;

        door.GetComponent<RoomFixtureMono>().IsRegistered = true;
    }

    /// <summary>
    /// Gets the door for this pair.
    /// </summary>
    public GameObject Door;

    /// <summary>
    /// Gets the originating point.
    /// </summary>
    public RoomMono A;

    /// <summary>
    /// Get the connected point.
    /// </summary>
    public RoomMono B;
}

/// <summary>
/// Represents a visual representation of a <see cref="DoorPair"/> within the Unity editor.
/// 
/// This MonoBehaviour serves as a visual aid for debugging and understanding door connections,
/// providing a clear overview of the door and its connected rooms directly within the editor.
/// </summary>
public class DoorPairMono : MonoBehaviour
{
    /// <summary>
    /// The GameObject representing the actual door within the game world.
    /// This is the physical door that players can interact with.
    /// </summary>
    [SerializeField] public GameObject Door;

    /// <summary>
    /// The RoomMono representing the first room connected to the door.
    /// </summary>
    [SerializeField] public RoomMono A;

    /// <summary>
    /// The RoomMono representing the second room connected to the door.
    /// </summary>
    [SerializeField] public RoomMono B;
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
    /// Add a new connection point with the connecting object not yet set.
    /// </summary>
    /// <param name="door"></param>
    /// <param name="A"></param>
    /// <returns></returns>
    public DoorPair Add(GameObject door, RoomMono A)
    {
        return this.Add(door, A, null);
    }

    /// <summary>
    /// Add a new connection point with the connection point known.
    /// </summary>
    /// <param name="door"></param>
    /// <param name="A"></param>
    /// <param name="B"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public DoorPair Add(GameObject door, RoomMono A, RoomMono B)
    {
        if (door == null || A == null)
            throw new ArgumentNullException("Door or A is null.");

        DoorPair newConnection = new DoorPair(door, A, B);

        if (doors.ContainsKey(door))
        {
            throw new ArgumentException("There is already a connection with that door.");
        }

        doors.Add(door, new DoorPair(door, A, B));

        updateRequired = true;
        return newConnection;
    }

    /// <summary>
    /// Grab connection information for a <see cref="GameObject"/> door.
    /// </summary>
    /// <param name="door"></param>
    /// <returns></returns>
    public DoorPair Get(GameObject door)
    {
        if (door == null) return null;
        return doors[door];
    }

    /// <summary>
    /// Grab connection information for a <see cref="RoomMono"/>.
    /// </summary>
    /// <param name="room"></param>
    /// <returns></returns>
    public List<DoorPair> Get(RoomMono room)
    {
        List<DoorPair> connections = new List<DoorPair>();
        foreach (DoorPair door in doors.Values)
        {
            if (door.Door == null)
            {
                updateRequired = true;
                continue;
            }

            if (door.A == room) connections.Add(door);
        }

        return connections;
    }

    /// <summary>
    /// Grab connection information for a <see cref="RoomMono"/> where pairs are complete.
    /// </summary>
    /// <param name="room"></param>
    /// <returns></returns>
    public List<DoorPair> GetTaken(RoomMono room)
    {
        List<DoorPair> connections = new List<DoorPair>();
        foreach (DoorPair door in doors.Values)
        {
            if (door.Door == null)
            {
                updateRequired = true;
                continue;
            }

            if (door.A == room && door.B != null) connections.Add(door);
        }

        return connections;
    }

    /// <summary>
    /// Grab connection information for a <see cref="RoomMono"/> where pairs are incomplete.
    /// </summary>
    /// <param name="room"></param>
    /// <returns></returns>
    public List<DoorPair> GetAvailable(RoomMono room)
    {
        List<DoorPair> connections = new List<DoorPair>();
        foreach (DoorPair door in doors.Values)
        {
            if (door.Door == null)
            {
                updateRequired = true;
                continue;
            }

            if (door.A == room && door.B == null) connections.Add(door);   
        }

        return connections;
    }

    /// <summary>
    /// Grab connection information for all available rooms.
    /// </summary>
    /// <returns></returns>
    public List<DoorPair> GetAllAvailable()
    {
        List<DoorPair> connections = new List<DoorPair>();
        foreach (var pair in doors.Values)
        {
            if (pair.B == null) connections.Add(pair);
        }

        return connections;
    }

    /// <summary>
    /// Returns whether two <see cref="RoomMono"/> has connection between each other.
    /// </summary>
    /// <param name="A"></param>
    /// <param name="B"></param>
    /// <returns></returns>
    public bool HasConnection(RoomMono A, RoomMono B)
    {
        return GetConnection(A, B) != null;
    }

    /// <summary>
    /// Returns the connection between two <see cref="RoomMono"/> .
    /// </summary>
    /// <param name="A"></param>
    /// <param name="B"></param>
    /// <returns></returns>
    public DoorPair GetConnection(RoomMono A, RoomMono B)
    {
        foreach (DoorPair door in Get(A))
        {
            bool C = door.A == A || door.A == B;
            bool D = door.B == A || door.B == B;
            if (C && D) return door;
        }

        return null;
    }

    /// <summary>
    /// Grab connection information for a <see cref="RoomMono"/> where it's the originating point, or the connection point.
    /// </summary>
    /// <param name="room"></param>
    /// <returns></returns>
    public List<DoorPair> GetConnections(RoomMono room)
    {
        if (room == null) return null;

        List<DoorPair> connections = new List<DoorPair>();
        foreach (DoorPair door in doors.Values)
        {
            if (door.A == room) connections.Add(door);
            if (door.B == room) connections.Add(door);
        }

        return connections;
    }

    /// <summary>
    /// Find the best path between two rooms.
    /// </summary>
    /// <param name="A">The starting room.</param>
    /// <param name="B">The destination room.</param>
    /// <returns></returns>
    public List<Tuple<RoomMono, GameObject>> FindBestPath(RoomMono A, RoomMono B)
    {
        // A list of connections needed to get the best path.
        List<Tuple<RoomMono, GameObject>> connections = new List<Tuple<RoomMono, GameObject>>();

        // Check if A and B are directly related so we can avoid my spaghetti.
        if (HasConnection(B,A))
        {
            // Grab the connections, return the first matching one.
            foreach (DoorPair pair in GetConnections(A))
                if (pair.B == A)
                    connections.Add(new Tuple<RoomMono, GameObject>(B, pair.Door));

            return connections;
        }

        // List of rooms yet explored, we'll add A in this first.
        Queue<RoomMono> roomsToExplore = new Queue<RoomMono>();
        roomsToExplore.Enqueue(A);

        // A list of rooms previously explored. 
        Dictionary<RoomMono, RoomMono> previousRoom = new Dictionary<RoomMono, RoomMono>();

        while (roomsToExplore.Count > 0)
        {
            RoomMono currentRoom = roomsToExplore.Dequeue();

            if (currentRoom == null) continue;

            // Check if we've reached the target room, if so we want to reconstruct the path.
            if (currentRoom == B)
            {
                while (currentRoom != A)
                {
                    // Add connections in reverse order to get the path from A to B
                    connections.Add(new Tuple<RoomMono, GameObject>(currentRoom, GetConnection(previousRoom[currentRoom], currentRoom).Door));
                    currentRoom = previousRoom[currentRoom];
                }

                // Reverse the list to get the path from A to B
                connections.Reverse();
                return connections;
            }

            // Explore connected rooms, queue their rooms to be searched too.
            foreach (DoorPair pair in GetConnections(currentRoom))
            {
                RoomMono nextRoom = pair.B;
                if (!previousRoom.ContainsKey(nextRoom))
                {
                    roomsToExplore.Enqueue(nextRoom);
                    previousRoom[nextRoom] = currentRoom;
                }
            }
        }

        // No path found
        return null;
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
    /// Remove all connection points that include a <see cref="RoomMono"/> as an originating point.
    /// </summary>
    /// <param name="room"></param>
    /// <returns></returns>
    public bool RemoveAll(RoomMono room)
    {
        foreach (GameObject door in room.Doors)
            Remove(door);

        return true;
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