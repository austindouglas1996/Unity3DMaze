using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(MazeController))] 
public class PathFinding : MonoBehaviour
{
    private MazeController controller;

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
        int bufferTileDistance = ((DistanceHelper.CalculateDistance(destTilePos, starTilePos) / 4) * 4) + 8;

        // This is not in the same room. Get possible routes to room.
        if (star.GroupId != dest.GroupId)
        {
            // Our path-finding is good enough to connect door to door, but hallways 
            // are special rooms and our path-finding does not like them. Because of this
            // we need to handle hallways in a special way. 
            //
            // First route the start/destination cell to the closest door cell.
            if (star.Type == CellType.Hallway)
            {
                Cell startDoorCells = FindPathToClosestDoor(star, bufferTileDistance, visited, path);
                if (startDoorCells == null)
                {
                    Debug.LogWarning($"Pathfinding: Failed to find a star door near {star.Position}");
                    return null;
                }

                // Update our start position to the door.
                star = startDoorCells;
            }

            if (dest.Type == CellType.Hallway)
            {
                Cell destDoorCells = FindPathToClosestDoor(dest, bufferTileDistance, visited, path);
                if (destDoorCells == null)
                {
                    Debug.LogWarning($"Pathfinding: Failed to find a dest door near {star.Position}");
                    return null;
                }

                // Update our destination position to the door.
                dest = destDoorCells;
            }

            // Reset the visited.
            visited.Clear();

            // Uses a greedy Breadth-First-Search to determine the cheapest
            // path from A room to B room. Returns the door object and the side of the room it should be in.
            var roomTravels = DeterminePathToRoom(star.Room, dest.Room);
            if (roomTravels == null)
                return null;

            foreach (var roomTravel in roomTravels)
            {
                DoorPair pair = this.controller.DoorRegistry.Get(roomTravel.Item2);
                if (pair == null)
                    throw new ArgumentNullException("During pathfinding across rooms. We failed to find a door causing a complete failure.");

                Cell doorCell = this.controller.Grid.Find(pair.Door.transform.position.RoundToInt(), new Vector3(2f, 2f, 2f));

                // This cell is not the correct cell, but we know the cell is on the otherside of the door.
                // There is a 50% chance this will occurr.
                if (doorCell.Room != roomTravel.Item1)
                {
                    // Find the direction the door is aiming in to find the correct cell.
                    SpatialOrientation doorDirection = pair.Door.GetComponent<RoomFixtureMono>().Direction;
                    doorCell = this.controller.Grid.Neighbor(doorCell, doorDirection);
                }

                // Create path to this cell.
                var cellPaths = DetermineCellPathInRoom2D(star, doorCell, bufferTileDistance, visited);
                if (cellPaths == null)
                    return null;

                // Reverse.
                cellPaths.Reverse();

                // Add the new cells.
                for (int i = 0; i < cellPaths.Count; i++)
                {
                    Cell cell = cellPaths.ElementAt(i);

                    // Our DetermineCellPath function does not know to not include the start.
                    // We will forgive trying to add the first, but after that we want to throw
                    // exceptions as we know it's a real error.
                    if (path.Contains(cell))
                        continue;

                    path.Push(cell);
                }

                // Set new current.
                star = doorCell;
            }
        }
        else
        {
            // Are we trying to path find in the same room? If so the logic is not as complicated
            // as trying to travel across rooms.
            path = DetermineCellPathInRoom2D(star, dest, bufferTileDistance, visited);
        }
        
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

        if (star.Position == dest.Position)
        {
            throw new InvalidOperationException("Pathfinding: You just tried to path find from the same start and destination.");
        }

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

        Cell closestCell = null;
        int closestDistance = Int32.MaxValue;

        foreach (Cell next in controller.Grid.Neighbors(curr))
        {
            int distance = DistanceHelper.CalculateDistance(next.Position, dest.Position);
            bool BothSameGroupId = (next.GroupId == curr.GroupId);
            bool BothAreDoors = (next.Type == CellType.Door && curr.Type == CellType.Door) || BothSameGroupId;

            if (visitedCells.Contains(next.Position))
                continue;

            if (next.Type == CellType.None)
                continue;

            if (distance >= closestDistance) 
                continue;

            if (curr.Room != next.Room && !BothAreDoors)
                continue;

            // Edge case.
            if (IsLandLocked(next))
                continue;

            closestCell = next;
            closestDistance = distance;
        }

        return closestCell;
    }

    /// <summary>
    /// Tells whether a cell during path finding will reach a dead end.
    /// </summary>
    /// <param name="curr"></param>
    /// <returns></returns>
    private bool IsLandLocked(Cell curr)
    {
        foreach (Cell neighbor in  controller.Grid.Neighbors(curr))
        {
            if (neighbor.Type != CellType.None) return false;
        }

        return true;
    }

    /// <summary>
    /// Find the fastest path to route a <see cref="Cell"/> to the nearest door. This method helps with pathfinding. Our pathfinding
    /// system does not like crossing rooms. Hallways are special rooms that break pathfinding. To fix this, we first path find
    /// to the nearest door then continue the find.
    /// </summary>
    /// <param name="star"></param>
    /// <param name="bufferTileDistance"></param>
    /// <param name="visited"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    private Cell FindPathToClosestDoor(Cell star, int bufferTileDistance, HashSet<Vector3Int> visited, UniqueStack<Cell> path)
    {
        // Make sure this type is a hallway.
        if (star.Type == CellType.Hallway)
        {
            // Find the closest door connection.
            int lowestDistance = int.MaxValue;
            DoorPair closestDoorPair = null;

            // Get all door connections this tile has access to. We want to find
            // the closest one near the position we're looking for. 
            var connections = this.controller.Hallways.GetDoorCon(star.Position);
            if (connections == null)
            {
                Debug.LogWarning($"Failed to find door connections for: {star.Position}");
                return null;
            }

            foreach (var connection in this.controller.Hallways.GetDoorCon(star.Position))
            {
                var pair = this.controller.DoorRegistry.Get(connection);
                if (pair == null) continue;

                int distance = DistanceHelper.CalculateDistance(star.Position, pair.Door.transform.position.RoundToInt());

                if (distance < lowestDistance)
                {
                    closestDoorPair = pair;
                    lowestDistance = distance;
                }
            }

            if (closestDoorPair == null)
            {
                Debug.LogWarning($"Failed to find an appropiate door pair for: {star.Position}");
                return null;
            }

            // The target cell will always be opposite of what we're looking for
            // but just in case, we'll make sure.
            Cell starRoot = closestDoorPair.BCell;

            // Find the path to the door.
            var cellPath = DetermineCellPathInRoom2D(star, starRoot, bufferTileDistance, visited);
            if (cellPath == null)
            {
                Debug.LogWarning("Found door connections, found an appropiate door. Failed to connect though.");
                return null;
            }

            // Merge cells.
            path.Combine(cellPath);

            return starRoot;
        }

        return null;
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
            foreach (var neighbor in controller.Grid.Neighbors(current.Position, 1))
            {
                if (neighbor != null && parent.ContainsKey(neighbor) && parent[neighbor] == null)
                {
                    parent[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }
        }

        // Reconstruct the path from end to start
        List<Cell> path = new List<Cell>();
        Cell? step = end;
        while (step != null && step != start)
        {
            path.Add(step);
            step = parent[step];
        }
        path.Add(start);

        // Check if the reconstructed path includes all required cells
        if (!path.Contains(end) || path.Count < cells.Count)
        {
            // Handle the case where not all cells are included in the path
            // For now, let's log the issue and add missing cells back
            Debug.Log("Path reconstruction did not include all cells. Adding missing cells back.");

            foreach (var cell in cells)
            {
                if (!path.Contains(cell))
                {
                    path.Add(cell);
                }
            }
        }

        // Push sorted cells back into the stack
        for (int i = path.Count - 1; i >= 0; i--)
        {
            stack.Push(path[i]);
        }
    }

}
