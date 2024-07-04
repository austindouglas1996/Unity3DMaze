using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using VHierarchy.Libs;

/// <summary>
/// Specifies the possible behaviors or interactive features associated with room pieces within the game world.
/// This enum determines how room pieces function and respond to various events or actions,
/// contributing to gameplay mechanics and challenges.
/// </summary>
[Serializable]
public enum RoomFixtureBehaviorType
{
    /// <summary>
    /// No specific behavior is assigned to the room piece. This is used for setting options, exceptions should be thrown if this is returned.
    /// </summary>
    None,

    /// <summary>
    /// Acts as a wildcard, indicating that any type of behavior is acceptable for the room piece. Useful for generation purposes.
    /// </summary>
    Any,

    /// <summary>
    /// The room piece contains a trap.
    /// </summary>
    Trap,

    /// <summary>
    /// The room piece has a chance to not be spawned and instead deleted on load. This can be used for things like alternate hallways.
    /// </summary>
    ChanceForNoSpawn,

    /// <summary>
    /// The room piece is designed to be destroyed when it comes into contact with a hallway. This might be used for creating dynamic paths.
    /// </summary>
    HallwayDoor,
}

/// <summary>
/// Defines the fundamental types of room pieces that make up the structure of rooms within the game world.
/// This enum categorizes room pieces based on their core identity and role in defining room layouts and boundaries.
/// </summary>
[Serializable]
public enum RoomFixtureIdentityType
{
    /// <summary>
    /// No specific room piece type is assigned. This might indicate an empty space or a placeholder for future content.
    /// </summary>
    None = -1,

    /// <summary>
    /// Acts as a wildcard, indicating that any type of room piece is acceptable. This might be used for flexible room generation or selection.
    /// </summary>
    Any = 0,

    Door = 1,
    Wall = 2,
    ShortWall = 3,
    Window = 4,
    Floor = 8,
    Roof = 16
}

/// <summary>
/// Represents a piece of a room like a wall, floor, roof. This piece handles its own generation with props.
/// </summary>
public class RoomFixtureMono : MonoBehaviour
{
    [Tooltip("Can type of piece does is this item (Helps with prop choosing)")]
    [SerializeField] public RoomFixtureIdentityType Primary;

    [Tooltip("A second type of type this item could be. Set to same as primary to ignore this.")]
    [SerializeField] private RoomFixtureIdentityType ChanceType = RoomFixtureIdentityType.None;

    [Tooltip("Does this piece have a special like a trap?")]
    [SerializeField] public RoomFixtureBehaviorType Behavior = RoomFixtureBehaviorType.None;

    [Tooltip("Should this entity keep its name after generation?")]
    [SerializeField] public bool StaticName = false;


    [Header("Prop options")]
    [Tooltip("What type of category does this piece fall into? Categories help keep styles in-line")]
    [SerializeField] public MazeTheme Theme = MazeTheme.UseParent;

    [Tooltip("Can we spawn props with this item?")]
    [SerializeField] private bool AllowProps = true;

    [Tooltip("Can we spawn props that need to touch the floor?")]
    [SerializeField] private bool FloorLevel = false;

    [Tooltip("The size of the prop for this tile.")]
    [SerializeField] public PropSize Size = PropSize.Any;


    [Header("Door options")]
    [Tooltip("Can this door be completely removed when connected to another room?")]
    [SerializeField] private bool SupportsRemoval = true;

    [Header("Trap options")]
    [Tooltip("Can we spawn props on this trap if the trap is not chosen?")]
    [SerializeField] private bool SupportsPropsOnNoTrap = true;

    /// <summary>
    /// Gets the direction this fixture is facing.
    /// </summary>
    public SpatialOrientation Direction
    {
        get { return this.GetDirection(); }
    }

    /// <summary>
    /// Tells whether this room is a trap.
    /// </summary>
    public bool IsTrap = false;

    /// <summary>
    /// Returns whether this piece allows props.
    /// </summary>
    public bool SupportsProps
    {
        get { return AllowProps; }
        set { AllowProps = value; }
    }

    /// <summary>
    /// Returns whether this piece is on floor level.
    /// </summary>
    public bool IsFloorLevel { get { return FloorLevel;} }

    /// <summary>
    /// Returns whether this object should be destroyed.
    /// </summary>
    public bool DestroyThis { get; set; } = false;

    /// <summary>
    /// Returns whether this object has finished generating.
    /// </summary>
    public bool GenerateFinished { get; private set; }

    /// <summary>
    /// Returns whether this piece is a door and if it's registered.
    /// </summary>
    public bool IsRegistered = false;

    /// <summary>
    /// Provides the instance for random chance values.
    /// </summary>
    private RoomSpawnOptions Chances
    {
        get { return Store.Chances; }
    }

    /// <summary>
    /// Provides the store for prop resources.
    /// </summary>
    private MazeResourceManager Store
    {
        get { return MazeResourceManager.Instance; }
    }

    /// <summary>
    /// Tells whether props are already being generated.
    /// </summary>
    private bool GeneratingProps = false;

    /// <summary>
    /// The object that holds the props for this piece.
    /// </summary>
    private GameObject PropHolder { get; set; }

    /// <summary>
    /// Hide the piece details.
    /// </summary>
    /// <returns></returns>
    public bool Hide()
    {
        if (!this.GenerateFinished)
            return false;

        if (PropHolder != null)
            PropHolder.SetActive(false);

        return true;
    }

    /// <summary>
    /// Show the piece details.
    /// </summary>
    /// <returns></returns>
    public bool Show()
    {
        if (!this.GenerateFinished)
            return false;

        if (PropHolder != null)
            PropHolder.SetActive(true);

        return true;
    }

    /// <summary>
    /// Generate a collection of props for this piece depending on settings.
    /// </summary>
    /// <returns></returns>
    public async Task GenerateProps()
    {
        if (GeneratingProps || !SupportsProps)
            return;

        GeneratingProps = true;

        // Retrieve the prop holder.
        // We will delete the holder and regenerate if this method is called.
        await CreatePropHolderChild();

        // Decide and place the props.
        DecideProps();

        GeneratingProps = false;
    }

    /// <summary>
    /// Set some properties.
    /// </summary>
    private void Start()
    {
        /*
        if (ShouldDestroy())
        {
            GenerateFinished = true;
            return;
        }*/

        ShouldGenerateTrap();
        ShouldGenerateChange();

        GenerateFinished = true;
    }

    /// <summary>
    /// Unity BS >:( need to correct piece.
    /// </summary>
    private void Update()
    {
        if (this.IsTrap && this.Behavior != RoomFixtureBehaviorType.Trap)
        {
            Debug.LogWarning("Set as trap, but also not?");
            this.Behavior = RoomFixtureBehaviorType.Trap;
        }

        if (this.IsTrap && PropHolder != null)
        {
            this.transform.Find("PropHolder").gameObject.Destroy();
            this.AllowProps = false;
        }
    }

    /// <summary>
    /// Grab the direction this fixture is facing. Important for door connections and props.
    /// </summary>
    /// <returns></returns>
    public SpatialOrientation GetDirection()
    {
        Vector3 forward = transform.forward;

        // Compare the forward vector's components to determine the direction
        if (Mathf.Abs(forward.x) > Mathf.Abs(forward.z))
            return forward.x > 0 ? SpatialOrientation.Up : SpatialOrientation.Down;
        else
            return forward.z > 0 ? SpatialOrientation.Left : SpatialOrientation.Right;
    }

    /// <summary>
    /// Create a new prop child to hold props for this piece.
    /// </summary>
    /// <returns></returns>
    private async Task CreatePropHolderChild()
    {
        if (!this.SupportsProps)
            return;

        // Check if a child already exists.
        if (PropHolder != null)
        {
            PropHolder.Destroy();
            while (PropHolder.IsDestroyed())
            {
                await Task.Delay(100);
            }
        }

        // Create the child.
        PropHolder = new GameObject("PropHolder"); 
        PropHolder.name = "PropHolder";
        PropHolder.transform.position = gameObject.transform.position;
        PropHolder.transform.rotation = gameObject.transform.rotation;
        PropHolder.transform.parent = gameObject.transform;
        PropHolder.SetActive(true);
    }

    /// <summary>
    /// Decide and generate the props for this piece.
    /// </summary>
    private void DecideProps()
    {
        // Generate a small prop?
        if (RandomHelper.Chance(80))
        {
            GenerateProp(PropSize.SuperSmall);
        }

        // Should we not generate props?
        if (!RandomHelper.Chance(Chances.GetSpawnChance(Primary)))
        {
            return;
        }

        if (Size == PropSize.Any)
        {
            foreach (PropSizeChance chance in Chances.PropSizeChances)
            {
                // We do not allow special to have a chance.
                if (chance.Size == PropSize.Special)
                    continue;

                if (RandomHelper.Chance(chance.Chance))
                {
                    GenerateProp(chance.Size);

                    // Super small is super unfun. Generate another maybe?
                    if (chance.Size == PropSize.SuperSmall)
                    {
                        continue;
                    }

                    break;
                }
            }
        }
        else
        {
            GenerateProp(Size);
        }
    }

    /// <summary>
    /// Generate a prop for this piece based on size.
    /// </summary>
    /// <param name="size"></param>
    /// <param name="forbidSuperSmall"></param>
    private void GenerateProp(PropSize size, bool forbidSuperSmall = false)
    {
        // The collection of props for this primary type.
        List<PropMono> props = Store.Default.GetProp(Primary);

        // Should we only allow certain types?
        if (size != PropSize.Any)
        {
            var list = props.Where(r => r.GetComponent<PropMono>().Size == size);
            props = list != null ? list.ToList() : null;
        }

        // Does this tile support floor level?
        if (!IsFloorLevel)
        {
            props = props.Where(r => r.GetComponent<PropMono>().RequiresFloor == false).ToList();
        }

        // Warn?
        if (props == null || props.Count == 0)
        {
            //Debug.LogWarning("Zero props found for " + type.ToString() + " at size " + size.ToString());
        }
        else
        {
            // Generate a new prop. If the prop is randomly chosen as super small. 
            Instantiate(props.Random(), PropHolder.transform.position, PropHolder.transform.rotation, PropHolder.transform);
        }
    }

    /// <summary>
    /// Returns whether a piece should generate a trap.
    /// </summary>
    /// <param name="pieceOptions"></param>
    /// <returns></returns>
    private bool ShouldDestroy()
    {
        if (Behavior == RoomFixtureBehaviorType.ChanceForNoSpawn && RandomHelper.Chance(80))
        {
            DestroyThis = this;
            return true;
        }

        if (Behavior == RoomFixtureBehaviorType.ChanceForNoSpawn)
            Behavior = RoomFixtureBehaviorType.None;

        return false;
    }

    /// <summary>
    /// Returns whether a piece should generate a trap.
    /// </summary>
    /// <param name="pieceOptions"></param>
    /// <returns></returns>
    private void ShouldGenerateTrap()
    {
        if (Behavior == RoomFixtureBehaviorType.Trap)
        {
            if (!RandomHelper.Chance(Chances.ChanceForTrap))
            {
                Behavior = RoomFixtureBehaviorType.ChanceForNoSpawn;

                if (SupportsPropsOnNoTrap)
                    SupportsProps = true;
                else
                    SupportsProps = false;
            }
            else
            {
                IsTrap = true;
            }
        }
    }

    /// <summary>
    /// Returns whether we should generate the secondary object. Like a wall to a door.
    /// </summary>
    /// <param name="pieceOptions"></param>
    /// <returns></returns>
    private void ShouldGenerateChange()
    {
        if (Primary != ChanceType && ChanceType != RoomFixtureIdentityType.Any && ChanceType != RoomFixtureIdentityType.None)
        {
            if (RandomHelper.Chance(MazeResourceManager.Instance.Chances.GetSpawnChance(ChanceType)))
            {
                Primary = ChanceType;
            }
        }
    }
}
