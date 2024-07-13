using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Represents a room in the maze. Rooms contain objects, doors, and things for the player to do.
/// </summary>
public class RoomMono : MonoBehaviour
{
    [Tooltip("Name of the room to help stop duplication.")]
    [SerializeField] public string InternalName = "";

    [Tooltip("Bounds and hallway pathfinding are determined by floors. If a room has multiple floors check this box as it can complicate pathfinding with blank space.")]
    [SerializeField] public bool MultiFloorRoom = false;

    [Tooltip("Don't automatically generate props for this room.")]
    [SerializeField] public bool NoAutomaticPropGeneration = false;

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
    /// Get the bounds of the room. Room must have a BoundingBox child.
    /// </summary>
    public Bounds Bounds
    {
        get
        {
            return base.transform.BoundingBox();
        }
    }

    /// <summary>
    /// Get the <see cref="Bounds"/> of this room as an integer.
    /// </summary>
    public BoundsInt GetBoundsInt
    {
        get
        {
            Bounds bounds = base.transform.BoundingBox();

            Vector3Int posInt = new Vector3Int(
                Mathf.RoundToInt(base.transform.position.x),
                Mathf.RoundToInt(base.transform.position.y),
                Mathf.RoundToInt(base.transform.position.z));

            Vector3Int minCornerInt = new Vector3Int(
                Mathf.RoundToInt(bounds.min.x),
                Mathf.RoundToInt(bounds.min.y),
                Mathf.RoundToInt(bounds.min.z));

            Vector3Int maxCornerInt = new Vector3Int(
                Mathf.RoundToInt(bounds.max.x),
                Mathf.RoundToInt(bounds.max.y),
                Mathf.RoundToInt(bounds.max.z));

            Vector3Int centerInt = (minCornerInt + maxCornerInt) / 2;

            Vector3Int sizeInt = new Vector3Int(
                Mathf.RoundToInt(bounds.size.x),
                Mathf.RoundToInt(bounds.size.y),
                Mathf.RoundToInt(bounds.size.z));

            return new BoundsInt(centerInt, sizeInt);
        }
    }

    /// <summary>
    /// Returns a list of doors this room contains.
    /// </summary>
    public List<GameObject> Doors = new List<GameObject>();

    /// <summary>
    /// Helpful for sometimes we can <see cref="Doors"/> before <see cref="GenerateDoors"/> called.
    /// </summary>
    public bool GenerateCalled = false;

    /// <summary>
    /// Helpful to know if the room has been fully generated.
    /// </summary>
    public bool GenerateFinished = false;

    /// <summary>
    /// A room style that controls what type of windows, floors, walls, etc. we get.
    /// </summary>
    private RoomThemePrefabs Style;

    /// <summary>
    /// Finds GameObjects with a <see cref="RoomFixtureMono"/> component that are within a specified distance of a given position.
    /// </summary>
    /// <remarks>
    /// Searches for neighbors within the child hierarchy of the current room.
    /// </remarks>
    /// <param name="position">The reference position to check neighbors against.</param>
    /// <param name="primaryType">The <see cref="RoomFixtureIdentityType"/> to filter results by. Use <see cref="RoomFixtureIdentityType.Any"/> to include all types.</param>
    /// <param name="offset">The maximum allowed distance between a neighbor and the reference position.</param>
    /// <returns>A list of GameObjects that meet the criteria, containing the <see cref="RoomFixtureMono"/> component and located within the specified distance.</returns>
    public List<Transform> FindNeighborPieces(Vector3 position, RoomFixtureIdentityType primaryType = RoomFixtureIdentityType.Any, RoomFixtureBehaviorType secondaryType = RoomFixtureBehaviorType.Any, float offset = 2f)
    {
        List<Transform> matches = new List<Transform>();
        foreach (Transform child in transform.GetComponentsInChildren<Transform>())
        {
            RoomFixtureMono pieceOptions = null; 
            Vector3 difference = (child.position - position);

            // Make sure sure this component does have a roomPiece attached.
            // Confirm whether all types are allowed, or this piece contains the type required
            // for primary and secondary.
            if (!child.TryGetComponent<RoomFixtureMono>(out pieceOptions)
                || primaryType != RoomFixtureIdentityType.Any && pieceOptions.Primary != primaryType
                || secondaryType != RoomFixtureBehaviorType.Any && pieceOptions.Behavior != secondaryType)
            {
                continue;
            }

            // Check if the difference is within the allowed offset.
            // We don't care much about Y as some rooms do have a slight difference.
            if (Mathf.Abs(difference.x) <= offset && Mathf.Abs(difference.z) <= offset) 
            {
                matches.Add(child);
            }
        }

        return matches;
    }

    /// <summary>
    /// Generate the props for each room piece.
    /// </summary>
    public virtual async Task GenerateProps()
    {
        foreach (RoomFixtureMono piece in this.transform.GetComponentsInChildren<RoomFixtureMono>())
        {
            await piece.GenerateProps();
        }
    }

    /// <summary>
    /// Turn an existing door into a wall.
    /// </summary>
    /// <param name="go"></param>
    public async Task<GameObject> TurnWallIntoDoor(GameObject go, SpatialOrientation direction, bool generateProp = true)
    {
        RoomFixtureMono rp = go.GetComponent<RoomFixtureMono>(); 
        if (rp.Primary != RoomFixtureIdentityType.Wall)
        {
            throw new System.NotSupportedException("This object is not a door.");
        }

        if (direction == SpatialOrientation.None)
        {
            throw new System.NotSupportedException("Direction.None is not supported for a door.");
        }

        if (rp.Direction == SpatialOrientation.None)
        {
            Debug.LogError("RoomMono Line 404 tried to change direction.");
        }

        GameObject newObject = GenerateObject(RoomFixtureIdentityType.Door, RoomFixtureBehaviorType.None, go.transform, rp);
        Doors.Add(newObject);

        if (generateProp)
            await rp.GenerateProps();

        // Delete the calling object.
        DestroyImmediate(go);

        return newObject;
    }

    /// <summary>
    /// Turn an existing door into a wall.
    /// </summary>
    /// <param name="go"></param>
    public async Task TurnDoorIntoWall(GameObject go, bool generateProp = true)
    {
        RoomFixtureMono rp = go.GetComponent<RoomFixtureMono>();
        if (rp.Primary != RoomFixtureIdentityType.Door)
        {
            throw new System.NotSupportedException("This object is not a door.");
        }

        GameObject newObject = GenerateObject(RoomFixtureIdentityType.Wall, RoomFixtureBehaviorType.None, go.transform, rp);
        
        if (generateProp)
            await rp.GenerateProps();

        // Remove from doors.
        Doors.Remove(go);

        // Delete the calling object.
        DestroyImmediate(go);
    }

    /// <summary>
    /// Called on start.
    /// </summary>
    /// <exception cref="System.InvalidOperationException"></exception>
    private async void Start()
    {
        await Generate();
    }

    /// <summary>
    /// Update the room conditions.
    /// </summary>
    private void Update()
    {
    }

    /// <summary>
    /// Generate the room.
    /// </summary>
    public async Task Generate()
    {
        if (GenerateCalled || GenerateFinished)
            return;
        else
            GenerateCalled = true;

        Style = MazeResourceManager.Instance.Default;

        await GenerateX(RoomFixtureIdentityType.Floor);
        await GenerateX(RoomFixtureIdentityType.Wall);
        await GenerateX(RoomFixtureIdentityType.ShortWall);
        await GenerateX(RoomFixtureIdentityType.Window);
        await GenerateX(RoomFixtureIdentityType.Roof);

        Doors = await GenerateX(RoomFixtureIdentityType.Door);

        await this.GenerateProps();

        GenerateFinished = true;
    }

    /// <summary>
    /// Generic generator based on primary type will decide on object properties.
    /// </summary>
    /// <param name="primary">The primary type to find in the list of children.</param>
    /// <param name="min">The minimum amount of objects to replace.</param>
    /// <param name="max">The maximum amount of objects to replace.</param>
    /// <returns>A list of created items.</returns>
    /// <exception cref="System.IndexOutOfRangeException"></exception>
    private async Task<List<GameObject>> GenerateX(RoomFixtureIdentityType primary, bool generateProps = false)
    {
        List<GameObject> newItems = new List<GameObject>();
        List<Transform> items = await GetChildrenByPieceType(primary);

        // Debug.
        int itemsMade = 0;

        foreach (Transform item in items)
        {
            itemsMade++;

            // Grab the options for generation.
            var pieceOptions = item.GetComponent<RoomFixtureMono>();

            GameObject newItem = null;

            if (pieceOptions.DestroyThis)
            {
                // Destroy the generated item.
                Destroy(item.gameObject);
                continue;
            }

            newItem = GenerateObject(pieceOptions.Primary, pieceOptions.Behavior, item, item.gameObject.GetComponent<RoomFixtureMono>());

            // Need to place tag back.
            newItem.tag = item.gameObject.tag;

            if (pieceOptions.StaticName)
            {
                // Reset name.
                newItem.name = item.name;
            }
            else
            {
                newItem.name = pieceOptions.Primary.ToString() + itemsMade;
            }

            newItems.Add(newItem);

            // Destroy the generated item.
            Destroy(item.gameObject);
        }

        return newItems;
    }

    /// <summary>
    /// Generate a new object. 
    /// </summary>
    /// <param name="type">Type of object to generate.</param>
    /// <param name="sub">The sub category this object should be./param>
    /// <param name="parent">The parent of this new object.</param>
    /// <param name="pieceOptions">The piece options found on the source object.</param>
    /// <returns></returns>
    private GameObject GenerateObject(RoomFixtureIdentityType type, RoomFixtureBehaviorType sub, Transform parent, RoomFixtureMono pieceOptions)
    {
        GameObject newObject = null;

        switch (type)
        {
            case RoomFixtureIdentityType.Floor:
                if (pieceOptions.IsTrap && MazeResourceManager.Instance.Castle.FloorTraps.Any())
                {
                    newObject = Instantiate(MazeResourceManager.Instance.Castle.FloorTraps.Random(), parent.position, parent.rotation);
                }
                else
                {
                    newObject = Instantiate(Style.FloorsPrefabs.Random(), parent.position, parent.rotation);
                }

                newObject.layer = 3;
                break;
            case RoomFixtureIdentityType.Wall:
                switch (sub)
                {
                    case RoomFixtureBehaviorType.Trap:
                        //newObject = Instantiate(RoomResourceStore.Instance.WallTraps.Random(), parent.position, parent.rotation);
                        newObject = Instantiate(Style.WallsPrefabs.Random(), parent.position, parent.rotation);
                        Debug.LogWarning("You just tried to make a wall trap. We don't have those yet.");
                        break;
                    default:
                        newObject = Instantiate(Style.WallsPrefabs.Random(), parent.position, parent.rotation);
                        break;
                }

                newObject.layer = 7;
                break;
            case RoomFixtureIdentityType.Window:
                newObject = Instantiate(Style.WindowsPrefabs.Random(), parent.position, parent.rotation);
                break;
            case RoomFixtureIdentityType.ShortWall:
                newObject = Instantiate(Style.ShortWallsPrefabs.Random(), parent.position, parent.rotation);
                break;
            case RoomFixtureIdentityType.Door:
                newObject = Instantiate(Style.DoorsPrefabs.Random(), parent.position, parent.rotation);
                newObject.layer = 6;
                break;
            case RoomFixtureIdentityType.Roof:
                newObject = Instantiate(Style.RoofPrefabs.Random(), parent.position, parent.rotation);
                break;
            default:
                throw new System.Exception("Type is not supported." + type.ToString());
        }

        // Modify properties.
        newObject.transform.parent = transform;
        newObject.tag = "Piece";

        // Does it have room piece options?
        if (newObject.GetComponent<RoomFixtureMono>() == null)
        {
            newObject.AddComponent<RoomFixtureMono>();
        }

        // Copy existing.
        EditorUtility.CopySerialized(pieceOptions, newObject.GetComponent<RoomFixtureMono>());

        return newObject;
    }

    /// <summary>
    /// Get a list of children based on <see cref="RoomFixtureIdentityType"/>.
    /// </summary>
    /// <param name="pieceType"></param>
    /// <returns></returns>
    public async Task<List<Transform>> GetChildrenByPieceType(params RoomFixtureIdentityType[] pieceType)
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

                if (child.tag != "Piece")
                {
                    Debug.LogWarning("Warning: Found an object with a RoomPiece but without a proper Piece tag in " + child.name);
                }

                if (pieceType.Contains(pieceOptions.Primary))
                {
                    matches.Add(child);
                }      
            }
        }

        return matches;
    }
}