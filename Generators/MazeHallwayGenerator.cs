using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.UIElements;
using VHierarchy.Libs;
using Random = UnityEngine.Random;

public class HallwayMap
{
    public HallwayMap(Vector3Int pos, bool isRoot)
    {
        this.Position = pos;
        this.IsRoot = isRoot;
    }

    public Vector3Int Position;
    public bool IsRoot = false;
    public bool IsTrap = false;

    public bool LeftV = false;
    public bool RightV = false;
    public bool UpV = false;
    public bool BottomV = false;

    public string NameOverride = "";
    public DoorPair DoorPair = null;
}

/// <summary>
/// Information on a request for a hallway stair to be generated. Information on the stairway
/// position, along with buffer positions to make sure no cells are generated inside the hallway.
/// Finally, additional information is supplied to help during final generation.
/// </summary>
/// <remarks>Old system used StartL and End for top/bottom stairs.</remarks>
public class HallwayStairMap
{
    public HallwayStairMap(Vector3Int top, Vector3Int end, Vector3Int bufferL, Vector3Int bufferR)
    {
        this.TopStair = top;
        this.BottomStair = end;
        this.BufferL = bufferL;
        this.BufferR = bufferR;
    }

    /// <summary>
    /// Stair locations are used for placing the top & bottom stair.
    /// </summary>
    public Vector3Int TopStair { get; set; }
    public Vector3Int BottomStair { get; set; }

    /// <summary>
    /// Buffer locations are used for determining the spacing above each stair.
    /// Each set of stairs requires 1 level (4f) above.
    /// </summary>
    public Vector3Int BufferL { get; set; }
    public Vector3Int BufferR { get; set; }

    /// <summary>
    /// Entrance and exit help determine if the stairway is still acceptable during 
    /// generation. There is many variables that cause a cell to be eliminated.
    /// These cells will be checked during generation to confirm the stairway can
    /// be generated.
    /// </summary>
    public Vector3Int Entrance { get; set; }
    public Vector3Int Exit { get; set; }

    /// <summary>
    /// Add the variables part of this request into an instance of <see cref="MazeGrid"/>.
    /// </summary>
    /// <param name="grid"></param>
    public void AddToGrid(MazeGrid grid)
    {
        grid.Set(TopStair, CellType.Stairway);
        grid.Set(BottomStair, CellType.Stairway);
        grid.Set(BufferL, CellType.Stairway);
        grid.Set(BufferR, CellType.Stairway);
    }

    /// <summary>
    /// Remove the variables part of this request from an instance of <see cref="MazeGrid"/>.
    /// </summary>
    /// <param name="grid"></param>
    public void RemoveFromGrid(MazeGrid grid)
    {
        grid.Clear(TopStair);
        grid.Clear(BottomStair);
        grid.Clear(BufferL);
        grid.Clear(BufferR);
    }

    /// <summary>
    /// Determine the rotation of each of the stairway cells.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public Quaternion DetermineRotation()
    {
        Vector3Int start = this.TopStair;
        Vector3Int end = this.BottomStair;

        if (start.x == end.x)
        {
            // Same X, compare Z
            int deltaZ = end.z - start.z;
            if (deltaZ > 0)
                return Quaternion.Euler(0, -90, 0); // Z positive
            else
                return Quaternion.Euler(0, 90, 0); // Z negative
        }
        else if (start.z == end.z)
        {
            // Same Z, compare X
            int deltaX = end.x - start.x;
            if (deltaX > 0)
                return Quaternion.Euler(0, 0, 0); // X positive
            else
                return Quaternion.Euler(0, 180, 0); // X negative
        }
        else
        {
            Debug.LogError("Invalid start and end positions: they must have either the same X or the same Z value.");
            return Quaternion.identity;
        }
    }
}

/// <summary>
/// Extends <see cref="MazeController"/> to generator hallways connecting rooms throughout a maze to help create a more unique
/// maze than just connecting <see cref="RoomMono"/> together.
/// </summary>
[RequireComponent(typeof(MazeController))]
public class MazeHallwayGenerator : MonoBehaviour, IGenerator<HallwayMono>
{
    [Tooltip("Basic prefab with 4 ways that can be distributed to create hallways.")]
    [SerializeField] private GameObject HallwayPrefab;

    [Tooltip("Basic prefab for 3D hallways. God help us all.")]
    [SerializeField] private GameObject StairwayPrefabT;
    [SerializeField] private GameObject StairwayPrefabB;

    /// <summary>
    /// Gets the <see cref="Maze"/> instance to use for accessing important properties.
    /// </summary>
    private MazeController Maze;

    /// <summary>
    /// Grid that keeps track of maze cells.
    /// </summary>
    private MazeGrid Grid;

    /// <summary>
    /// Returns whether <see cref="Generate"/> has been called.
    /// </summary>
    public bool GenerateCalled { get; private set; }

    /// <summary>
    /// Returns whether <see cref="Generate"/> has finished.
    /// </summary>
    public bool GenerateFinished { get; private set; }

    /// <summary>
    /// A list of generated hallways.
    /// </summary>
    public List<HallwayMono> Generated { get; private set; } = new List<HallwayMono>();

    /// <summary>
    /// Tells whether hallways have finished generation to stop
    /// <see cref="FindPossibleBridges"/> from failing due to <see cref="GenerateHallways"/> from
    /// throwing a collection was modified exception.
    /// </summary>
    private bool hallwayGenerated = false;

    private List<HallwayMono> HallwayCells = new List<HallwayMono>();
    private List<HallwayMap> PreMappedCells = new List<HallwayMap>();
    private List<HallwayStairMap> PreMappedStairCells = new List<HallwayStairMap>();

    /// <summary>
    /// Called when the generator is initialized.
    /// </summary>
    private void Start()
    {
        Maze = this.GetComponent<MazeController>();
        this.Grid = Maze.Grid;
    }

    /// <summary>
    /// Generate hallways around the maze causing for more confusion and fresh experienced compared
    /// to pre-set rooms.
    /// </summary>
    /// <returns></returns>
    public async Task Generate()
    {
        if (this.GenerateCalled) return;
        this.GenerateCalled = true;

        // Map the roots.
        this.MapRootCells(this.Maze.DoorRegistry.GetAvailable());

        // Map paths between roots.
        this.MapPathCells();

        // Find possible gaps between cells to create unique bridges.
        this.MapPathBridges();

        // Create 'noise' in the maze.
        this.MapPathAlleys();

        // Create the pathing for each maze cell.
        // This is like a cleanup.
        this.MapMathingPreRun();
        while (await this.MapPathing())
        {
            await this.MapPathing();
        }

        // Final touchups before deploying.
        this.MapDetails();

        // Finally, commit our changes to the Maze.
        await this.CommitCells();

        this.GenerateFinished = true;
    }

    /// <summary>
    /// Destroy everything this generator made.
    /// </summary>
    /// <returns></returns>
    public async Task ResetGenerator()
    {
        this.HallwayCells.Clear();
        this.PreMappedCells.Clear();
        this.PreMappedStairCells.Clear();

        // Destroy.
        foreach (HallwayMono hall in this.Generated)
        {
            hall.gameObject.Destroy();
        }
        this.Generated.Clear();

        this.GenerateCalled = false;
        this.GenerateFinished = false;
    }

    /// <summary>
    /// With a list of available vacant doors in the maze. Create hallway roots
    /// so we know where we can establish hallway cells. Roots are important because the position of
    /// each cell is not yet esatblished and stable. A root cell will make it so our cells are always stable.
    /// </summary>
    /// <param name="available"></param>
    /// <exception cref="System.Exception"></exception>
    private void MapRootCells(List<DoorPair> available)
    {
        foreach (DoorPair pair in available)
        {
            GameObject door = pair.Door;

            if (door.IsDestroyed()) 
                continue;

            // Piece options to grab direction.
            SpatialOrientation direction = door.GetComponent<RoomFixtureMono>().Direction;

            // Bounds.
            Bounds doorA = door.GetComponent<Renderer>().bounds;
            Bounds cubeBounds = HallwayPrefab.transform.BoundingBox();

            // Position for root.
            Vector3 position = door.transform.position;

            switch (direction)
            {
                case SpatialOrientation.Up:
                    position = new Vector3(doorA.max.x + cubeBounds.extents.x, position.y + cubeBounds.extents.y, doorA.center.z);
                    break;
                case SpatialOrientation.Right:
                    position = new Vector3(doorA.center.x, position.y + cubeBounds.extents.y, doorA.min.z - cubeBounds.extents.z);
                    break;
                case SpatialOrientation.Down:
                    position = new Vector3(doorA.min.x - cubeBounds.extents.x, position.y + cubeBounds.extents.y, doorA.center.z);
                    break;
                case SpatialOrientation.Left:
                    position = new Vector3(doorA.center.x, position.y + cubeBounds.extents.y, (doorA.max.z + cubeBounds.extents.z));
                    break;
                default:
                    throw new System.Exception("Direction: " + direction + " is not supported.");
            }

            this.CreateMap(position.RoundToInt(), true, pair);
        }
    }

    /// <summary>
    /// Go through each root cell and try to connect them with the others. This creates pathing throughout the maze.
    /// </summary>
    /// <returns></returns>
    private void MapPathCells()
    {
        List<HallwayMap> rootCells = PreMappedCells.Where(r => r.IsRoot).ToList();

        int count = rootCells.Count;
        for (int i = 0; i < count; i++)
        {
            for (int j = count - 1; j > i; j--)
            {
                if (rootCells[i] == rootCells[j]) continue;
                ConnectTwoRoots(rootCells[i], rootCells[j]);
            }
        }
    }

    /// <summary>
    /// Connects hallways have a cell gap.
    /// </summary>
    private void MapPathBridges()
    {
        // TODO:
    }

    /// <summary>
    /// Creates random pathways that don't connect to a room.
    /// </summary>
    private void MapPathAlleys()
    {
        foreach (var cell in this.PreMappedCells.ToList())
        {
            // 40% chance to spawn an alley.
            if (Random.Range(0, 100) > 40) continue;

            // How many alleys should we spawn?
            int distance = Random.Range(3, 11);

            for (int i = 0; i < distance; i++)
            {
                // Grab neighbors that are empty. Continue is there is none.
                List<Cell> neighbors = this.Grid.Neighbors(cell.Position,1).Where(r => r.Type == CellType.None).ToList();
                if (neighbors.Count() == 0) continue;

                Cell chosenCell = neighbors.Random();

                this.CreateMap(chosenCell.Position, false);
            }
        }
    }

    /// <summary>
    /// Clean up some existing elements before creating hallway paths.
    /// </summary>
    private void MapMathingPreRun()
    {
        List<HallwayStairMap> removeStairs = new List<HallwayStairMap>();

        // Check our stairs and make sure they are valid.
        foreach (var stairMap in this.PreMappedStairCells)
        {
            Cell entranceCell = this.Grid[stairMap.Entrance];
            Cell exitCell = this.Grid[stairMap.Exit];

            // No double hallways.
            if (exitCell.Type == CellType.Stairway || entranceCell.Type == CellType.Stairway)
            {
                stairMap.RemoveFromGrid(this.Grid);
                removeStairs.Add(stairMap);
                continue;
            }

            if (entranceCell.Type == CellType.None || exitCell.Type == CellType.None)
            {
                stairMap.RemoveFromGrid(this.Grid);
                removeStairs.Add(stairMap);
                continue;
            }
        }

        // Remove stairs.
        foreach (var stairMap in removeStairs)
            this.PreMappedStairCells.Remove(stairMap);
    }

    /// <summary>
    /// Determines map walls and if they should be open, closed, or destroyed.
    /// </summary>
    private async Task<bool> MapPathing()
    {
        await Task.Delay(10);
        bool unclean = false;

        foreach (var map in PreMappedCells.ToList())
        {
            CellNeighborGroup neighbors = this.Grid.Neighbors(map.Position, 1);

            if (neighbors.Up.Type != CellType.None 
                && neighbors.Left.Type != CellType.None
                && neighbors.Right.Type != CellType.None 
                && neighbors.Down.Type != CellType.None)
            {
                if (neighbors.UpRight.Type != CellType.None
                    && neighbors.UpLeft.Type != CellType.None
                    && neighbors.DownRight.Type != CellType.None
                    && neighbors.DownLeft.Type != CellType.None)
                {
                    // Delete this one.
                    this.RemoveMap(map);
                    unclean = true;
                    continue;
                }

                if (neighbors.UpRight.Type != CellType.None
                    && neighbors.UpLeft.Type != CellType.None
                    && neighbors.DownRight.Type == CellType.None
                    && neighbors.DownLeft.Type == CellType.None)
                {
                    map.LeftV = true;
                    map.BottomV = true;
                    map.UpV = false;
                    map.RightV = false;
                }
            }

            /* I do not know why, and im very mad, but this is a solution
             * to getting what walls should be visible from an edge case.
             I spent over 3 hours trying to figure out why randomly
            some walls would break here. I added a lot of debug logic 
            and came all the way back here for it for Visual Studio
            if (up.Type == None) do something when up.type == none it would not trigger.*/
            bool up = neighbors.Up.Type == CellType.None;
            bool left = neighbors.Left.Type == CellType.None;
            bool right = neighbors.Right.Type == CellType.None;
            bool down = neighbors.Down.Type == CellType.None;

            if (up)
            {
                map.UpV = true;
            }

            if (left)
            {
                map.LeftV = true;
            }

            if (right)
            {
                map.RightV = true;
            }

            if (down)
            {
                map.BottomV = true;
            }
        }

        return unclean;
    }

    /// <summary>
    /// Create the small details of the maze.
    /// </summary>
    private void MapDetails()
    {
        this.MapDetailsTraps();
    }

    /// <summary>
    /// Distribute traps throughout the hallways, because we're E V I L >:D
    /// </summary>
    private void MapDetailsTraps()
    {
        // How many hallway cells should contain a trap?
        int remainingTraps = (int)(this.PreMappedCells.Count * 0.4);
        int remainingFailures = remainingTraps * 2;

        while (remainingTraps > 0 && remainingFailures > 0)
        {
            HallwayMap map = this.PreMappedCells.Random();

            // Check if we already set this one as a trap.
            if (map.IsTrap)
            {
                remainingFailures--;
                continue;
            }

            map.IsTrap = true;
            remainingTraps--;
        }
    }

    /// <summary>
    /// Empty the <see cref="PreMappedCells"/> list and create <see cref="HallwayMono"/> instances.
    /// </summary>
    private async Task CommitCells()
    {
        await CommitStairCells();
        await CommitHallways();
    }

    /// <summary>
    /// Generate the hallway stairs.
    /// </summary>
    /// <returns></returns>
    private async Task CommitStairCells()
    {
        // Take our stairways and create into objects.
        foreach (var stairMap in this.PreMappedStairCells)
        {
            GameObject bottomGO = Instantiate(this.StairwayPrefabB, stairMap.BottomStair, stairMap.DetermineRotation(), this.transform);
            GameObject topGO = Instantiate(this.StairwayPrefabT, stairMap.TopStair, stairMap.DetermineRotation(), this.transform);

            //GameObject g = Instantiate(this.Maze.debugCube4, stairMap.Entrance, stairMap.DetermineRotation(), this.transform);
            //GameObject g1 = Instantiate(this.Maze.debugCube4, stairMap.Exit, stairMap.DetermineRotation(), this.transform);

            //Instantiate(this.Maze.debugCube, stairMap.BufferR, stairMap.DetermineRotation(), this.transform);
            //Instantiate(this.Maze.debugCube, stairMap.BufferL, stairMap.DetermineRotation(), this.transform);

            HallwayMono bottom = bottomGO.GetComponent<HallwayMono>();
            HallwayMono top = topGO.GetComponent<HallwayMono>();

            // Generate.
            await bottom.Generate();
            await top.Generate();

            // Add to generated.
            Generated.Add(bottom);
            Generated.Add(top);
        }
    }

    /// <summary>
    /// Generate our hallway cells into the grid.
    /// </summary>
    /// <returns></returns>
    private async Task CommitHallways()
    {
        // Take our map and create into objects.
        foreach (var map in this.PreMappedCells)
        {
            GameObject inst = Instantiate(HallwayPrefab, map.Position, quaternion.identity, this.transform);
            HallwayMono newHall = inst.GetComponent<HallwayMono>();

            // Root cells are connected to a door.
            if (map.IsRoot)
            {
                // Set a connection with the door.
                newHall.name = "Root";
                this.Maze.DoorRegistry.SetConnection(map.DoorPair.Door, newHall);
            }

            if (!string.IsNullOrEmpty(map.NameOverride))
                newHall.name = map.NameOverride;

            // Set the cell room.
            this.Grid[map.Position].Room = newHall;

            // Generate.
            await newHall.Generate(map);

            // Add to generated for later processing.
            Generated.Add(newHall);
        }
    }

    /// <summary>
    /// Connect two root hallways to one another. Roots are used as the foundation of a 
    /// hallway. We know the doors are open and safe. We're now wanting to make the path to them.
    /// </summary>
    /// <param name="A"></param>
    /// <param name="B"></param>
    private void ConnectTwoRoots(HallwayMap A, HallwayMap B)
    {
        // Position of each root.
        Vector3Int APOS = A.Position;
        Vector3Int BPOS = B.Position;

        if (APOS == BPOS)
        {
            Debug.LogWarning($"Just tried to connect a root to itself? A: {A.Position} B:{B.Position}");
            return;
        }

        // Don't allow hallway paths that are outside a small range.
        if ((Vector3.Distance(APOS, BPOS) > 6 * 4))
            return;

        // Get (possibly) best direction.
        Vector3 direction = DistanceHelper.DetermineDirectionBetweenPointsXZ(this.Grid, APOS, BPOS);

        // Keep a list of stair hallways that will be made during this
        // this connection between these two roots.
        List<HallwayStairMap> stairways = new List<HallwayStairMap>();

        // Try to create stairs between two cells if possible.
        if (APOS.y != BPOS.y)
        {
            var verticalSpaces = IsThereVerticalOpening(APOS, A.Position, B.Position);
            if (verticalSpaces.Item1)
            {
                stairways = verticalSpaces.Item2;

                // Map each set of stairways.
                foreach (var stairway in stairways)
                {                
                    // Add for later processing.
                    this.PreMappedStairCells.Add(stairway);

                    // Add to grid.
                    stairway.AddToGrid(this.Grid);
                }

            }
        }

        Vector3Int currentPosition = APOS;

        /*
         * You may be looking at this and thinking this is built wrong. You would be right, but
         * when this is built correctly the maze suffers. Because of this we purposefully
         * break the maze.
         * 
         * In case we need to correct it:
         * 
         *  direction.X == 1
         *  currentPosition = CreatePathBetweenCellsZ(APOS, A.Position, B.Position, stairways);
         *  CreatePathBetweenCellsX(currentPosition, A.Position, B.Position, stairways);
         *  
         *  direction.z == 1
         *  currentPosition = CreatePathBetweenCellsX(APOS, A.Position, B.Position, stairways);
         *  CreatePathBetweenCellsX(currentPosition, A.Position, B.Position, stairways);
         */

        if (direction.x == 1)
        {
            currentPosition = CreatePathBetweenCellsX(APOS, A.Position, B.Position, stairways);
            CreatePathBetweenCellsZ(currentPosition, A.Position, B.Position, stairways);
        }
        else
        {
            currentPosition = CreatePathBetweenCellsX(APOS, A.Position, B.Position, stairways);
            CreatePathBetweenCellsZ(currentPosition, A.Position, B.Position, stairways);
        }

        if (currentPosition == APOS)
            Debug.LogWarning($"Failed to make any connections for {A.Position} to {B.Position}");
    }

    /// <summary>
    /// Create cell instances along a path from one direction to another along the Z axis.
    /// </summary>
    /// <param name="curr"></param>
    /// <param name="A"></param>
    /// <param name="B"></param>
    /// <returns></returns>
    private Vector3Int CreatePathBetweenCellsX(Vector3Int curr, Vector3Int A, Vector3Int B, List<HallwayStairMap> request)
    {
        // Determine if we're going up or down.
        bool positive = DistanceHelper.IsPositiveDirection(curr.x, B.x);

        while (positive && curr.x - B.x < 0 || !positive && curr.x - B.x > 0)
        {
            curr.x += positive ? 4 : -4;

            // Check if this is the start position of a stairway. If so, adjust our position to the end.
            var stairway = request.FirstOrDefault(r => r.Entrance == curr);
            if (stairway != null)
            {
                curr = stairway.Exit;
            }

            var result = this.CreateMap(curr, false);
        }

        return curr;
    }

    /// <summary>
    /// Create cell instances along a path from one direction to another along the Z axis.
    /// </summary>
    /// <param name="curr"></param>
    /// <param name="A"></param>
    /// <param name="B"></param>
    /// <returns></returns>
    private Vector3Int CreatePathBetweenCellsZ(Vector3Int curr, Vector3Int A, Vector3Int B, List<HallwayStairMap> request)
    {        
        // Determine if we're going up or down.
        bool positive = DistanceHelper.IsPositiveDirection(curr.z, B.z);

        while (positive && curr.z - B.z < 0 || !positive && curr.z - B.z > 0)
        {            
            curr.z += positive ? 4 : -4;

            // Check if this is the start position of a stairway. If so, adjust our position to the end.
            var stairway = request.FirstOrDefault(r => r.Entrance == curr);
            if (stairway != null)
            {
                curr = stairway.Exit;
            }

            var result = this.CreateMap(curr, false);
        }

        return curr;
    }

    /// <summary>
    /// Map out vertical openings along a path between A and B. 
    /// </summary>
    /// <param name="curr"></param>
    /// <param name="A"></param>
    /// <param name="B"></param>
    /// <returns></returns>
    private Tuple<bool, List<HallwayStairMap>> IsThereVerticalOpening(Vector3Int curr, Vector3Int A, Vector3Int B)
    {
        List<HallwayStairMap> results = new List<HallwayStairMap>();
        int stepsRequired = Math.Abs((A.y - B.y) / 4);

        // Too deep.
        if (stepsRequired > 6 || stepsRequired <= 0)
            return new Tuple<bool, List<HallwayStairMap>>(false, null);

        // Determine if we're going up or down.
        bool positiveX = DistanceHelper.IsPositiveDirection(curr.x, B.x);
        bool positiveY = DistanceHelper.IsPositiveDirection(A.y, B.y);
        bool positiveZ = DistanceHelper.IsPositiveDirection(curr.z, B.z);

        // Stairways sometimes turn. Due to this we need a one cell break
        // between the two. This is a rare case, but this resolves the problem
        // when dealing with an L staircase.
        bool spacingBreak = false;

        // Handle the X direction.
        while (positiveX && curr.x - B.x < 0 || !positiveX && curr.x - B.x > 0)
        {
            if (stepsRequired <= 0)
                break;

            curr.x += positiveX ? 4 : -4;

            // Is a space required?
            if (spacingBreak)
            {
                spacingBreak = false;
                continue;
            }

            var result = IsVerticalOpening(curr, new Vector3Int(1, 0, 0), positiveX, positiveY ? 1 : -1);
            if (result.Item1)
            {
                results.Add(result.Item2);

                // Adjust the position.
                curr.y += positiveY ? 4 : -4;
                curr.x += positiveX ? 8 : -8;

                stepsRequired--;
                spacingBreak = true;
            }
        }

        // Handle the Z direction.
        while (positiveZ && curr.z - B.z < 0 || !positiveZ && curr.z - B.z > 0)
        {
            if (stepsRequired <= 0)
                break;

            curr.z += positiveZ ? 4 : -4;

            // Is a space required?
            if (spacingBreak)
            {
                spacingBreak = false;
                continue;
            }

            var result = IsVerticalOpening(curr, new Vector3Int(0, 0, 1), positiveZ, positiveY ? 1 : -1);
            if (result.Item1)
            {
                results.Add(result.Item2);

                // Adjust the position.
                curr.y += positiveY ? +4 : -4;
                curr.z += positiveZ ? +8 : -8;

                stepsRequired--;
            }
        }

        if (stepsRequired != 0)
            return new Tuple<bool, List<HallwayStairMap>>(false, null);

        return new Tuple<bool, List<HallwayStairMap>>(true, results);
    }

    /// <summary>
    /// Determine if there is a vertical opening in a cell for a 2x2 block created.
    /// </summary>
    /// <param name="pos">Start position of the cell.</param>
    /// <param name="direction">Direction X or Z. One must be set to one.</param>
    /// <param name="positive">Direction next cells need to be created. +4/-4</param>
    /// <param name="YDifference">The difference in the Y going up or down.</param>
    /// <returns>False if no acceptable Y, or an instance with positions.</returns>
    /// <exception cref="NotSupportedException"></exception>
    private Tuple<bool, HallwayStairMap> IsVerticalOpening(Vector3Int pos, Vector3Int direction, bool positive, int YDifference)
    {
        if (YDifference != -1 && YDifference != 1)
            throw new NotSupportedException("Must be between -1 or 1.");

        if (direction.x != 1 && direction.z != 1)
            throw new NotSupportedException("X or Z must be supplied.");

        Cell t1, t2, b1, b2, start, end;

        int x = positive ? 4 : -4;
        int y = YDifference * 4;
        int z = positive ? -4 : 4;

        // Stairs require 4 cells.
        if (direction.x == 1)
        {
            // Left - Right (Top)
            t1 = this.Grid[pos];
            t2 = this.Grid[pos + new Vector3Int(x, 0, 0)];

            // Left - Right (Bottom)
            b1 = this.Grid[pos + new Vector3Int(0, y, 0)];
            b2 = this.Grid[pos + new Vector3Int(x, y, 0)];

            // Assign locations.
            start = this.Grid[b1.Position + new Vector3Int(-x, 4, 0)];
            end = this.Grid[pos + new Vector3Int((x * 2), y, 0)];
        }
        else // We're doing Z.
        {
            // Left - Right (Top)
            t1 = this.Grid[pos];
            t2 = this.Grid[pos + new Vector3Int(0, 0, z)];

            // Left - Right (Bottom)
            b1 = this.Grid[pos + new Vector3Int(0, y, 0)];
            b2 = this.Grid[pos + new Vector3Int(0, y, z)];

            // Assign locations.
            start = this.Grid[b1.Position + new Vector3Int(0, 4, -z)];
            end = this.Grid[pos + new Vector3Int(0, y, (z * 2))];
        }

        // Must be empty.
        if (t1.Type != CellType.None
            || t2.Type != CellType.None
            || b1.Type != CellType.None
            || b2.Type != CellType.None)
        {
            return new Tuple<bool, HallwayStairMap>(false, null);
        }

        var newMap = new HallwayStairMap(b1.Position, b2.Position, t1.Position, t2.Position);

        // If our Y is going up switch the start and end.
        if (YDifference == 1)
        {
            newMap.Entrance = end.Position;
            newMap.Exit = start.Position;
        }
        else
        {
            newMap.Entrance = start.Position;
            newMap.Exit = end.Position;
        }

        return new Tuple<bool, HallwayStairMap>(true, newMap);
    }

    /// <summary>
    /// Attemps to create a new <see cref="HallwayMap"/> instance. Performs several checks
    /// to see if the cell is taken in the <see cref="Maze.Grid"/>, or <see cref="MapGrid"/>.
    /// </summary>
    /// <param name="pos">Position to make the map</param>
    /// <param name="isRoot"></param>
    /// <returns></returns>
    private HallwayMap CreateMap(Vector3Int pos, bool isRoot, DoorPair pair = null)
    {
        if (!this.Grid.IsValid(pos))
        {
            return null;
        }

        HallwayMap newMap = new HallwayMap(pos, isRoot);
        newMap.DoorPair = pair;

        // Set the value.
        this.Grid.Set(pos, CellType.Hallway);
        this.PreMappedCells.Add(newMap);

        return newMap;
    }

    private bool RemoveMap(HallwayMap map)
    {
        bool mapGrid = this.Grid.Clear(map.Position);
        bool premapped = this.PreMappedCells.Remove(map);

        if (!mapGrid)
            throw new InvalidOperationException("Cell does not exist in map.");

        if (!premapped)
            throw new InvalidOperationException("Cell does not exist in premap.");

        return true;
    }
}