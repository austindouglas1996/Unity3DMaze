using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents an edge within the <see cref="SimpleRoomPrimsAlgorithm"/> to help with finding edges and the minimum-spanning-tree.
/// </summary>
public class RoomEdge
{
    public RoomMono Room1 { get; }
    public RoomMono Room2 { get; }
    public float Weight { get; }

    public RoomEdge(RoomMono room1, RoomMono room2, float weight)
    {
        Room1 = room1;
        Room2 = room2;
        Weight = weight;
    }
}

public class SimpleRoomPrimsAlgorithm
{
    /// <summary>
    /// Calculate and determine the edges around the grid. 
    /// </summary>
    /// <param name="rooms"></param>
    /// <returns></returns>
    public List<RoomEdge> CalculateEdges(List<RoomMono> rooms)
    {
        var edges = new List<RoomEdge>();

        for (int i = 0; i < rooms.Count; i++)
        {
            for (int j = i + 1; j < rooms.Count; j++)
            {
                var room1 = rooms[i];
                var room2 = rooms[j];
                float distance = Vector3.Distance(room1.transform.position, room2.transform.position);
                edges.Add(new RoomEdge(room1, room2, distance));
            }
        }

        return edges;
    }

    /// <summary>
    /// Find the Minimum-Spanning-Tree (MST) for a list of rooms in a maze. 
    /// </summary>
    /// <param name="rooms">A collection of rooms inside the maze.</param>
    /// <param name="edges">A collection of edges for the rooms, see <see cref="CalculateEdges(List{RoomMono})"/> for help on finding this.</param>
    /// <returns></returns>
    public List<RoomEdge> FindMinimumSpanningTree(List<RoomMono> rooms, List<RoomEdge> edges)
    {
        var mst = new List<RoomEdge>();
        var visitedRooms = new HashSet<RoomMono>();
        var edgePriorityQueue = new SortedSet<RoomEdge>(Comparer<RoomEdge>.Create((e1, e2) =>
            e1.Weight == e2.Weight ? e1.GetHashCode().CompareTo(e2.GetHashCode()) : e1.Weight.CompareTo(e2.Weight)));

        // Start with the first room
        var startRoom = rooms[0];
        visitedRooms.Add(startRoom);

        // Add all edges connected to the start room
        foreach (var edge in edges)
        {
            if (edge.Room1 == startRoom || edge.Room2 == startRoom)
            {
                edgePriorityQueue.Add(edge);
            }
        }

        // While there are edges to process and not all rooms are visited
        while (edgePriorityQueue.Count > 0 && visitedRooms.Count < rooms.Count)
        {
            // Get the edge with the smallest weight
            var smallestEdge = edgePriorityQueue.Min;
            edgePriorityQueue.Remove(smallestEdge);

            RoomMono nextRoom = null;
            if (!visitedRooms.Contains(smallestEdge.Room1))
            {
                nextRoom = smallestEdge.Room1;
            }
            else if (!visitedRooms.Contains(smallestEdge.Room2))
            {
                nextRoom = smallestEdge.Room2;
            }

            // If the edge leads to an unvisited room, add it to the MST
            if (nextRoom != null)
            {
                visitedRooms.Add(nextRoom);
                mst.Add(smallestEdge);

                // Add all edges connected to the newly visited room, but only if they lead to unvisited rooms
                foreach (var edge in edges)
                {
                    if ((edge.Room1 == nextRoom && !visitedRooms.Contains(edge.Room2)) ||
                        (edge.Room2 == nextRoom && !visitedRooms.Contains(edge.Room1)))
                    {
                        edgePriorityQueue.Add(edge);
                    }
                }
            }
        }

        return mst;
    }
}