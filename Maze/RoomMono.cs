using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.MessageBox;

public class RoomMono : GenerateMono
{
    [Tooltip("Name of the room to help with identification.")]
    [SerializeField] private string InternalName = "";

    /// <summary>
    /// Returns whether this <see cref="GameObject"/> contains with another <see cref="RoomMono"/> instance.
    /// </summary>
    /// <param name="roomA"></param>
    /// <param name="roomB"></param>
    /// <returns></returns>
    public static bool CheckForContains(RoomMono roomA, RoomMono roomB)
    {
        // Get the bounds of both objects and then apply a small offset.
        Bounds boundsA = roomA.transform.Find("BoundingBox").GetComponent<Renderer>().bounds;
        Bounds boundsB = roomB.transform.Find("BoundingBox").GetComponent<Renderer>().bounds;

        // Combine the bounds and positions to create more precise bounds
        Bounds combinedBoundsA = new Bounds(roomA.transform.position, boundsA.size);
        Bounds combinedBoundsB = new Bounds(roomB.transform.position, boundsB.size);

        return combinedBoundsA.Contains(combinedBoundsB.center);
    }

    /// <summary>
    /// Returns whether this <see cref="GameObject"/> intersects with another <see cref="GameObject"/> class.
    /// </summary>
    /// <param name="roomA"></param>
    /// <param name="roomB"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public static bool CheckForIntersection(RoomMono roomA, RoomMono roomB, float offset = 0.3f)
    {
        // Get the bounds of both objects and then apply a small offset.
        Bounds boundsA = roomA.transform.Find("BoundingBox").GetComponent<Renderer>().bounds;
        Bounds boundsB = roomB.transform.Find("BoundingBox").GetComponent<Renderer>().bounds;

        // Apply a small offset.
        boundsA.extents -= Vector3.one * offset;

        // Combine the bounds and positions to create more precise bounds
        Bounds combinedBoundsA = new Bounds(roomA.transform.position, boundsA.size);
        Bounds combinedBoundsB = new Bounds(roomB.transform.position, boundsB.size);

        // Check for intersections using the combined bounds
        return combinedBoundsA.Intersects(combinedBoundsB);
    }

    /// <summary>
    /// Returns a list of doors this room contains.
    /// </summary>
    public List<Transform> Doors = new List<Transform>();

    /// <summary>
    /// Get a list of children based on <see cref="RoomFixtureIdentityType"/>.
    /// </summary>
    /// <param name="pieceType"></param>
    /// <returns></returns>
    public async Task<List<Transform>> GetChildrenByPieceType(params RoomFixtureStructureType[] pieceType)
    {
        List<Transform> matches = new List<Transform>();
        foreach (Transform child in transform.GetComponentsInChildren<Transform>())
        {
            if (child.IsDestroyed() || child == null)
                continue;

            RoomFixtureMono pieceOptions = null;
            if (child.TryGetComponent<RoomFixtureMono>(out pieceOptions))
            {
                while (!pieceOptions.GenerateFinished)
                {
                    await Task.Delay(5);
                }

                if (pieceType.Contains(pieceOptions.Type))
                {
                    matches.Add(child);
                }
            }
        }

        return matches;
    }

    /// <summary>
    /// Generate the room.
    /// </summary>
    protected override async Task OnGenerate(object[] args)
    {
        foreach (RoomFixtureMono child in transform.GetComponentsInChildren<RoomFixtureMono>())
        {
            if (child.IsDestroyed() || child == null)
                continue;

            await child.Generate(null);
        }

        await Task.Yield();

        foreach (RoomFixtureMono child in transform.GetComponentsInChildren<RoomFixtureMono>())
        {
            if (child.Type == RoomFixtureStructureType.Door)
            {
                this.Doors.Add(child.transform);
            }
        }
    }
}