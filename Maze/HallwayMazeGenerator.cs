using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using VHierarchy.Libs;
using Random = UnityEngine.Random;

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

[RequireComponent(typeof(MazeController))]
public class HallwayMazeGenerator : MazeGenerator<HallwayMono>
{
    [Header("Alley Generation Options")]
    [Tooltip("The maximum amount of cells two hallway roots can be from each other. The higher the number, along with the higher number of rooms the more ridiculous the mazes.")]
    [SerializeField] private int MaxPathRange = 6;

    [Tooltip("HallwayGenerator line 615: You may be looking at this and thinking this is built wrong. You would be right, but\r\n * when this is built correctly the maze suffers. Because of this we purposefully\r\n * break the maze. \r\n Tldr; Hallways are generated on X & Z and not just X.")]
    [SerializeField] private bool CorrectHallways = false;


    [Header("Alley Generation Options")]
    [SerializeField] private bool Alleys = true;
    [SerializeField] private float AlleyChance = 0.4f;
    [SerializeField] private int MinAlley = 1;
    [SerializeField] private int MaxAlley = 4;

    [Tooltip("Basic prefab with 4 ways that can be distributed to create hallways.")]
    [SerializeField] private GameObject HallwayPrefab;

    [Tooltip("Basic prefab for 3D hallways. God help us all.")]
    [SerializeField] private GameObject StairwayPrefabT;
    [SerializeField] private GameObject StairwayPrefabB;

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
    /// Generate the hallway.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    protected override async Task OnGenerate(object[] args)
    {
        // Map the roots.
        this.MapRootCells(Maze.DoorRegistry.GetAvailable());

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
    /// Reset the hallway generator.
    /// </summary>
    /// <returns></returns>
    protected override Task OnResetGenerator()
    {
        this.HallwayCells.Clear();
        this.PreMappedCells.Clear();
        this.PreMappedStairCells.Clear();

        // Destroy.
        foreach (HallwayMono hall in this.GeneratedEntities)
        {
            hall.gameObject.Destroy();
        }

        this.GeneratedEntities.Clear();

        return Task.CompletedTask;
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
        int seen = 0;
        foreach (DoorPair pair in available)
        {
            GameObject door = pair.Door;

            if (door.IsDestroyed())
                continue;

            // Piece options to grab direction.
            SpatialOrientation direction = door.GetComponent<RoomFixtureMono>().Direction;

            // Bounds.
            Bounds doorA = door.GetBounds();
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

            // We want to be at the door base.
            position -= new Vector3(0, 2, 0);

            this.CreateMap(position.RoundToInt(), true, pair);

            Cell rootCell = this.Maze.Grid[position.RoundToInt()];

            seen++;
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
        if (!Alleys) return;

        foreach (var cell in this.PreMappedCells.ToList())
        {
            // 40% chance to spawn an alley.
            if (Random.Range(0, 100) > AlleyChance * 100) continue;

            // How many alleys should we spawn?
            int distance = Random.Range(MinAlley, MaxAlley);

            // Current selected cell.
            HallwayMap currentMap = cell;

            for (int i = 0; i < distance; i++)
            {
                // Grab neighbors that are empty. Continue is there is none.
                List<Cell> neighbors = this.Maze.Grid.Neighbors(currentMap.Position, 1).Where(r => r.Type == CellType.None).ToList();
                if (neighbors.Count() == 0) continue;

                Cell chosenCell = neighbors.Random();
                currentMap = this.CreateMap(chosenCell.Position, false);
                currentMap.NameOverride = "ALLEY";
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
            Cell entranceCell = this.Maze.Grid[stairMap.Entrance];
            Cell exitCell = this.Maze.Grid[stairMap.Exit];

            // No double hallways.
            if (exitCell.Type == CellType.Stairway || entranceCell.Type == CellType.Stairway)
            {
                stairMap.RemoveFromGrid(this.Maze.Grid);
                removeStairs.Add(stairMap);
                continue;
            }

            if (entranceCell.Type == CellType.None || exitCell.Type == CellType.None)
            {
                stairMap.RemoveFromGrid(this.Maze.Grid);
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
            CellNeighborGroup neighbors = this.Maze.Grid.Neighbors(map.Position, 1);

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
             to getting what walls should be visible from an edge case.
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
            await bottom.Generate(new object[] { stairMap });
            await top.Generate(new object[] { stairMap });

            // Add to generated.
            GeneratedEntities.Add(bottom);
            GeneratedEntities.Add(top);
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
            GameObject inst = Instantiate(HallwayPrefab, map.Position, Quaternion.identity, this.transform);
            HallwayMono newHall = inst.GetComponent<HallwayMono>();

            // Root cells are connected to a door.
            if (map.IsRoot)
            {
                // Set a connection with the door.
                newHall.name = "Root";
                this.Maze.DoorRegistry.SetConnection(map.DoorPair.Door, newHall);
            }

            Cell hallCell = this.Maze.Grid[map.Position];

            if (!string.IsNullOrEmpty(map.NameOverride))
                newHall.name = map.NameOverride;

            hallCell.SetWallVisibility(SpatialOrientation.Up, map.UpV);
            hallCell.SetWallVisibility(SpatialOrientation.Right, map.RightV);
            hallCell.SetWallVisibility(SpatialOrientation.Left, map.LeftV);
            hallCell.SetWallVisibility(SpatialOrientation.Down, map.BottomV);

            if (!map.IsRoot)
            {
                if (this.Maze.Grid.Neighbor(hallCell, SpatialOrientation.Up).Type == CellType.Room)
                    hallCell.SetWallVisibility(SpatialOrientation.Up, true);
                if (this.Maze.Grid.Neighbor(hallCell, SpatialOrientation.Right).Type == CellType.Room)
                    hallCell.SetWallVisibility(SpatialOrientation.Right, true);
                if (this.Maze.Grid.Neighbor(hallCell, SpatialOrientation.Left).Type == CellType.Room)
                    hallCell.SetWallVisibility(SpatialOrientation.Left, true);
                if (this.Maze.Grid.Neighbor(hallCell, SpatialOrientation.Down).Type == CellType.Room)
                    hallCell.SetWallVisibility(SpatialOrientation.Down, true);
            }

            hallCell.Room = newHall;

            // Generate.
            await newHall.Generate(new object[] { map });

            // Add to generated for later processing.
            GeneratedEntities.Add(newHall);
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
        if ((Vector3.Distance(APOS, BPOS) > MaxPathRange * 4))
            return;

        // Get (possibly) best direction.
        Vector3 direction = DistanceHelper.DetermineDirectionBetweenPointsXZ(this.Maze.Grid, APOS, BPOS);

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
                    stairway.AddToGrid(this.Maze.Grid);
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
            if (CorrectHallways)
            {
                currentPosition = CreatePathBetweenCellsZ(APOS, A.Position, B.Position, stairways);
                CreatePathBetweenCellsX(currentPosition, A.Position, B.Position, stairways);
            }
            else
            {
                currentPosition = CreatePathBetweenCellsX(APOS, A.Position, B.Position, stairways);
                CreatePathBetweenCellsZ(currentPosition, A.Position, B.Position, stairways);
            }
        }
        else
        {
            if (CorrectHallways)
            {
                currentPosition = CreatePathBetweenCellsZ(APOS, A.Position, B.Position, stairways);
                CreatePathBetweenCellsX(currentPosition, A.Position, B.Position, stairways);
            }
            else
            {
                currentPosition = CreatePathBetweenCellsX(APOS, A.Position, B.Position, stairways);
                CreatePathBetweenCellsZ(currentPosition, A.Position, B.Position, stairways);
            }
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
            t1 = this.Maze.Grid[pos];
            t2 = this.Maze.Grid[pos + new Vector3Int(x, 0, 0)];

            // Left - Right (Bottom)
            b1 = this.Maze.Grid[pos + new Vector3Int(0, y, 0)];
            b2 = this.Maze.Grid[pos + new Vector3Int(x, y, 0)];

            // Assign locations.
            start = this.Maze.Grid[b1.Position + new Vector3Int(-x, 4, 0)];
            end = this.Maze.Grid[pos + new Vector3Int((x * 2), y, 0)];
        }
        else // We're doing Z.
        {
            // Left - Right (Top)
            t1 = this.Maze.Grid[pos];
            t2 = this.Maze.Grid[pos + new Vector3Int(0, 0, z)];

            // Left - Right (Bottom)
            b1 = this.Maze.Grid[pos + new Vector3Int(0, y, 0)];
            b2 = this.Maze.Grid[pos + new Vector3Int(0, y, z)];

            // Assign locations.
            start = this.Maze.Grid[b1.Position + new Vector3Int(0, 4, -z)];
            end = this.Maze.Grid[pos + new Vector3Int(0, y, (z * 2))];
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
        if (!this.Maze.Grid.IsValid(pos))
        {
            return null;
        }

        HallwayMap newMap = new HallwayMap(pos, isRoot);
        newMap.DoorPair = pair;

        // Set the value.
        this.Maze.Grid.Set(pos, CellType.Hallway);
        this.PreMappedCells.Add(newMap);

        return newMap;
    }

    private bool RemoveMap(HallwayMap map)
    {
        bool mapGrid = this.Maze.Grid.Clear(map.Position);
        bool premapped = this.PreMappedCells.Remove(map);

        if (!mapGrid)
            throw new InvalidOperationException("Cell does not exist in map.");

        if (!premapped)
            throw new InvalidOperationException("Cell does not exist in premap.");

        return true;
    }
}