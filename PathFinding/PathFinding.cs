using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(MazeController))] 
public class PathFinding : MonoBehaviour
{
    private MazeController controller;
    public GameObject debugCube;
    private List<GameObject> debugs = new List<GameObject>();

    /// <summary>
    /// A simple BFS greedy search algorithm to find the distance between two cells.
    /// </summary>
    /// <param name="starTilePos"></param>
    /// <param name="destTilePos"></param>
    /// <param name="maxTileBuffer"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public UniqueStack<Cell> DetermineCellPath2D(Vector3Int starTilePos, Vector3Int destTilePos, int maxTileBuffer)
    {
        UniqueStack<Cell> path = new UniqueStack<Cell>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

        Cell star = controller.Grid[starTilePos];
        Cell dest = controller.Grid[destTilePos]; 
        Cell initialStart = star;

        // Make sure we're not trying to path find to tiles that do not exist.
        if (star.Type == CellType.None || dest.Type == CellType.None)
            throw new ArgumentException("Start or End leads to an empty cell. Cannot pathfind.");

        // Set the buffer distance. (Distance / CellSize) * CellSize
        int bufferTileDistance = ((CalculateDistance(destTilePos, starTilePos) / 4) * 4) + 8;

        // Are we trying to path find in the same room? If so the logic is not as complicated
        // as trying to travel across rooms.
        path = DetermineCellPathInRoom2D(star, dest, bufferTileDistance, visited);

        if (path != null)
            OrganizeStack(path, dest, initialStart);

        return path;
    }

    /// <summary>
    /// Simple pathfinding through one specific room. Cannot traveral through multiple rooms, but can help with creating
    /// connections points inside of a room.
    /// </summary>
    /// <param name="star">Start of the room.</param>
    /// <param name="dest">End position we want to reach in the room.</param>
    /// <param name="maxTileBuffer">Max remaining buffer tiles.</param>
    /// <param name="visited">List of tiles we have visited before.</param>
    /// <returns></returns>
    private UniqueStack<Cell> DetermineCellPathInRoom2D(Cell star, Cell dest, int maxTileBuffer, HashSet<Vector3Int> visited)
    {
        UniqueStack<Cell> path = new UniqueStack<Cell>();

        // Set initial cell.
        Cell previous = null;
        Cell current = star;
        while (current != dest && maxTileBuffer > 0)
        {
            // Add this tile to the top of the stack.
            path.Push(current);

            // Add this tile also that it has been visited so we don't end in an infinity loop.
            visited.Add(current.Position);

            if (previous != null)
            {
                // We don't want this function crossing rooms. Because of this if our previous
                // cell was a room, and our current is a door we want to return.
                bool differentRooms = previous.Room != current.Room;
                bool bothDoors = previous.Type == CellType.Door && current.Type == CellType.Door;

                if (differentRooms && bothDoors)
                    break;
            }

            // Grab the next best possible cell to reach the destination.
            Cell nextCell = DetermineCheapestNextCell(current, dest, visited);

            // Were we unable to find another cell? If so, then we cannot reach our destination.
            // This only happens when the item cannot be reached, or something bad as happened. 
            if (nextCell == null)
            {
                return null;
            }

            // Set the next cell.
            current = nextCell;
            maxTileBuffer--;

            // Set previous.
            previous = current;
        }

        // In case we reached our destination, let's add it to the stack.
        // Don't add it to the stack in case we had to break early due to
        // room change.
        if (current == dest)
        {
            // Add the destination.
            path.Push(dest);
            visited.Add(dest.Position);
        }

        return path;
    }

    /// <summary>
    /// Find the nearest cheapest neighbor cell from another cell. Uses a list of already visited cells to avoid going backwards.
    /// Several other properties like, if the next cell is in another room this cell must be a door.
    /// </summary>
    /// <param name="curr"></param>
    /// <param name="dest"></param>
    /// <param name="visitedCells"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private Cell DetermineCheapestNextCell(Cell curr, Cell dest, HashSet<Vector3Int> visitedCells)
    {
        if (curr.Position == dest.Position)
            throw new ArgumentException("Start and end are the same position.");

        List<Cell> neighbors = controller.Grid.DirectNeighbors(curr);

        Cell closestCell = null;
        int closestDistance = Int32.MaxValue;

        foreach (Cell next in neighbors)
        {
            int distance = CalculateDistance(next.Position, dest.Position); 

            bool BothAreHallways = (next.Type == CellType.Hallway && curr.Type == CellType.Hallway);
            bool BothAreDoors = (next.Type == CellType.Door && curr.Type == CellType.Door) || BothAreHallways;

            if (visitedCells.Contains(next.Position))
                continue;

            if (next.Type == CellType.None)
                continue;

            if (distance >= closestDistance) 
                continue;

            if (curr.Room != next.Room && !BothAreDoors)
                continue;

            closestCell = next;
            closestDistance = distance;
        }

        return closestCell;
    }

    /// <summary>
    /// Finds the cheapest path from room A to rom B, if a connection exists. Starts out by looking
    /// to see if there is a direct connection with room A & B (Like room0 & room1). If a direct connection
    /// cannot be located. performs a breadth-first-search (BFS) to try and find a cheap path through directly connected
    /// rooms, hallways.
    /// NOTICE: STAIRS ARE NOT SUPPORTED. 2D SEARCH ONLY.
    /// </summary>
    /// <param name="A">The starting room.</param>
    /// <param name="B">The ending room.</param>
    /// <returns>A list of tuple, each containing the next direct room jump and the corresponding door leading to it. Representing 
    /// path from the starting room to the destination room. If no connection can be made will return null.</returns>
    /// <exception cref="ArgumentNullException">A or B room is null.</exception>
    /// <exception cref="ArgumentException">A and B is the same exact room.</exception>
    public List<Tuple<RoomMono, GameObject>> DeterminePathToRoom(RoomMono A, RoomMono B)
    {
        if (A == null || B == null) throw new ArgumentNullException("start or end is null");
        if (A == B) throw new ArgumentException("Start and end is the same object.");

        // Check whether the rooms have a direct connection.
        List<DoorPair> directConnections = controller.DoorRegistry.GetConnections(A, B);
        if (directConnections.Count > 0)
        {
            return CreateListOfRoomPaths(A, directConnections);
        }

        // No direct path was able to be found.
        return FindPathUsingBFS(A, B);
    }

    /// <summary>
    /// Create a simple list of room paths from A. The list will include the room other than A.
    /// </summary>
    /// <param name="A">The starting room.</param>
    /// <param name="pairs">A list of rooms along with their corresponding door.</param>
    /// <returns></returns>
    private List<Tuple<RoomMono, GameObject>> CreateListOfRoomPaths(RoomMono A, List<DoorPair> pairs)
    {
        return pairs.Select(pair => new Tuple<RoomMono, GameObject>(pair.GetOtherRoom(A), pair.Door)).ToList();
    }

    /// <summary>
    /// Performs a simple Breadth-First-Search (BFS) from A room to B room by looking through interconnected rooms
    /// throughout the maze. A room is expanded to grab all directly connected rooms until a match is found.
    /// </summary>
    /// <param name="A">Starting room.</param>
    /// <param name="B">Ending room.</param>
    /// <returns></returns>
    private List<Tuple<RoomMono, GameObject>> FindPathUsingBFS(RoomMono A, RoomMono B)
    {
        // The rooms do not have a direct connection to one another. So we will want to load each room
        // grab their door connections until we find a path that leads to our desired room.
        Queue<RoomMono> roomsToExplore = new Queue<RoomMono>(new RoomMono[] { A });

        // We'll keep a list of rooms we have previously explored to stop an infinity loop.
        Dictionary<RoomMono, RoomMono> previousRoom = new Dictionary<RoomMono, RoomMono>();

        while (roomsToExplore.Count > 0)
        {
            RoomMono currentRoom = roomsToExplore.Dequeue();

            // This should never happen, but just in case.
            if (currentRoom == null) continue;

            // We have found a path to our desired room. 
            if (currentRoom == B)
                return BuildPathFromPreviousRooms(A, currentRoom, previousRoom);

            // We want to expand the current room to find their door connections.
            ExploreRoomConnections(currentRoom, previousRoom, roomsToExplore);
        }

        // Path is not possible.
        return null;
    }

    /// <summary>
    /// Creates a direct room-path for leading the way from room A to room B. Finding the initial destination requires we go forward. This will reverese
    /// the results and return a simple list that can be used to help form the directions needed for simple path finding.
    /// </summary>
    /// <param name="A">The starting room.</param>
    /// <param name="currentRoom">The current room in the search.</param>
    /// <param name="previousRoom">The previous room that was searched.</param>
    /// <returns>A list of rooms with their repsective door connections in reverse order to help with starting from the beginning.</returns>
    private List<Tuple<RoomMono, GameObject>> BuildPathFromPreviousRooms(RoomMono A, RoomMono currentRoom, Dictionary<RoomMono, RoomMono> previousRoom)
    {
        // A list of connections needed to get the best path.
        List<Tuple<RoomMono, GameObject>> connections = new List<Tuple<RoomMono, GameObject>>();

        // We want to load the rooms in reverse order. We found the rooms going forward from start to end.
        // Now we want to return the list following that same logic. So we will be walking backwards to get
        // that information.
        while (currentRoom != A)
        {
            GameObject previousRoomDoor = controller.DoorRegistry.GetConnections(previousRoom[currentRoom], currentRoom)[0].Door;
            connections.Add(new Tuple<RoomMono, GameObject>(currentRoom, previousRoomDoor));

            // Set the currentRoom as the previous.
            currentRoom = previousRoom[currentRoom];
        }

        // Now to finish we want to reverse the list.
        connections.Reverse();
        return connections;
    }

    /// <summary>
    /// Expand a rooms connections to add their direct connections into an existing queue to be searched deeper.
    /// </summary>
    /// <param name="currentRoom">The current room ebing searched.</param>
    /// <param name="previousRoom">The previous room being searched.</param>
    /// <param name="roomsToExplore">A queue of rooms scheduled to be searched.</param>
    private void ExploreRoomConnections(RoomMono currentRoom, Dictionary<RoomMono, RoomMono> previousRoom, Queue<RoomMono> roomsToExplore)
    {
        foreach (DoorPair pair in controller.DoorRegistry.Get(currentRoom))
        {
            RoomMono nextRoom = pair.GetOtherRoom(currentRoom);
            if (previousRoom.ContainsKey(nextRoom)) continue;

            roomsToExplore.Enqueue(nextRoom);
            previousRoom[nextRoom] = currentRoom;
        }
    }

    /// <summary>
    /// Initializes the component.
    /// </summary>
    private void Start()
    {
        this.controller = this.GetComponent<MazeController>();
    }

    /// <summary>
    /// Simple function to help with calculating the distance between two vectors.
    /// </summary>
    /// <param name="C1"></param>
    /// <param name="C2"></param>
    /// <returns></returns>
    private static int CalculateDistance(Vector3Int C1, Vector3Int C2)
    {
        // Calculate the absolute difference in x, y, and z coordinates (Manhattan distance)
        int x = Mathf.Abs(C1.x - C2.x);
        int y = Mathf.Abs(C1.y - C2.y);
        int z = Mathf.Abs(C1.z - C2.z);

        return x + y + z;
    }

    /// <summary>
    /// Organize a <see cref="UniqueStack{T}"/>. This helps with the edge cases when working with pathing across rooms, sometimes the stack
    /// needs to be reversed one way or the other this will prioritize putting the elements back in order.
    /// </summary>
    /// <param name="stack"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    private void OrganizeStack(UniqueStack<Cell> stack, Cell start, Cell end)
    {
        // Extract cells from stack
        List<Cell> cells = new List<Cell>();
        while (stack.Count > 0)
        {
            cells.Add(stack.Pop());
        }

        // Create a dictionary to hold the parent of each cell for path reconstruction
        Dictionary<Cell, Cell?> parent = new Dictionary<Cell, Cell?>();
        foreach (var cell in cells)
        {
            parent[cell] = null;
        }

        // BFS to find the shortest path
        Queue<Cell> queue = new Queue<Cell>();
        queue.Enqueue(start);
        parent[start] = start;

        while (queue.Count > 0)
        {
            Cell current = queue.Dequeue();
            if (current.Position == end.Position)
            {
                break;
            }

            // Get neighbors using the Neighbors function
            List<Cell> neighbors = controller.Grid.DirectNeighbors(current, 1);
            foreach (var neighbor in neighbors)
            {
                if (neighbor != null && parent.ContainsKey(neighbor) && parent[neighbor] == null)
                {
                    parent[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }
        }

        // Reconstruct the path from start to end
        List<Cell> path = new List<Cell>();
        Cell? step = end;
        while (step != null && step != start)
        {
            path.Add(step);
            step = parent[step];
        }
        path.Add(start);
        path.Reverse();

        // Push sorted cells back into the stack
        foreach (var cell in path)
        {
            stack.Push(cell);
        }
    }
}
