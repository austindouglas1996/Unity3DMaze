using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using VHierarchy.Libs;

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
    /// Buffer locations are used for determining the spacing above/below each stair.
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
    [Tooltip("Basic prefab with 4 ways that can be distributed to create hallways.")]
    [SerializeField] private GameObject HallwayPrefab;

    [Tooltip("Basic prefab for 3D hallways. God help us all.")]
    [SerializeField] private GameObject StairwayPrefabT;
    [SerializeField] private GameObject StairwayPrefabB;

    private GridCellPathFinder pathFinder;

    private List<HallwayMono> HallwayCells = new List<HallwayMono>();
    private List<HallwayMap> PreMappedCells = new List<HallwayMap>();
    private List<HallwayStairMap> PreMappedStairCells = new List<HallwayStairMap>();

    protected override async Task OnGenerate(object[] args)
    {
        pathFinder = new GridCellPathFinder(base.Maze.Grid, base.Maze);

        MapInitialPath();
        await MapPathing();
        MapDetails();

        // Finally, commit our changes to the Maze.
        await this.CommitCells();

    }

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

    Dictionary<RoomMono, List<HallwayMap>> Connections = new Dictionary<RoomMono, List<HallwayMap>>();

    private void MapInitialPath()
    {
        var prim = new SimpleRoomPrimsAlgorithm();
        var rooms = this.Maze.Rooms.GeneratedEntities;
        var edges = prim.CalculateEdges(rooms);
        var mst = prim.FindMinimumSpanningTree(rooms, edges);
        var availableDoors = this.Maze.DoorRegistry.GetAvailable();

        foreach (var room in rooms)
        {
            Connections.Add(room, MapRootCells(this.Maze.DoorRegistry.GetAvailable(room)));
        }

        foreach (var edge in mst)
        {
            try
            {
                var a = this.Connections[edge.Room1].Random();
                var b = this.Connections[edge.Room2].Random();

                this.Connections[edge.Room1].Remove(a);
                this.Connections[edge.Room2].Remove(b);

                availableDoors.Remove(a.DoorPair);
                availableDoors.Remove(b.DoorPair);

                // Create path.
                ConnectTwoRoots(a, b);
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }
    }

    /// <summary>
    /// With a list of available vacant doors in the maze. Create hallway roots
    /// so we know where we can establish hallway cells. Roots are important because the position of
    /// each cell is not yet esatblished and stable. A root cell will make it so our cells are always stable.
    /// </summary>
    /// <param name="available"></param>
    /// <exception cref="System.Exception"></exception>
    private List<HallwayMap> MapRootCells(List<DoorPair> available)
    {
        List<HallwayMap> maps = new List<HallwayMap>();

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

            var map = this.CreateMap(position.RoundToInt(), true, pair);
            if (map != null)
                maps.Add(map);
        }

        return maps;
    }

    /// <summary>
    /// Connect two root hallways to one another. Roots are used as the foundation of a 
    /// hallway. We know the doors are open and safe. We're now wanting to make the path to them.
    /// </summary>
    /// <param name="A"></param>
    /// <param name="B"></param>
    private void ConnectTwoRoots(HallwayMap A, HallwayMap B)
    {
        Cell ACell = this.Maze.Grid[A.Position];
        Cell BCell = this.Maze.Grid[B.Position];

        var pathCells = pathFinder.FindHallwayPath(ACell, BCell);
        if (pathCells == null) return;

        foreach (Cell cell in pathCells.Item1)
        {
            CreateMap(cell.Position, false, A.DoorPair);
        }

        for (int i = 0; i < pathCells.Item2.Count; i += 6)
        {
            // Entrance & exit.
            var sEn = pathCells.Item2[i];
            var sEx = pathCells.Item2[i + 5];

            // Stairway cells.
            var s0 = pathCells.Item2[i + 2]; // Top 
            var s1 = pathCells.Item2[i + 1]; // Bottom
            var s2 = pathCells.Item2[i + 3]; // Buffer L
            var s3 = pathCells.Item2[i + 4]; // Buffer R

            // Determine if our position is going down. If so, we want to reverse the buffer.
            var descending = DistanceHelper.IsPositiveDirection(sEn.Position.y, sEx.Position.y);
            if (!descending)
            {
                s0 = pathCells.Item2[i + 3];
                s1 = pathCells.Item2[i + 4];
                s2 = pathCells.Item2[i + 2];
                s3 = pathCells.Item2[i + 1];
            }

            // Make a stair map.
            var stairMap = new HallwayStairMap(s0.Position, s1.Position, s2.Position, s3.Position);
            stairMap.Entrance = sEn.Position;
            stairMap.Exit = sEx.Position;

            MazeController.Instance.CreateDebugCube(stairMap.BottomStair, new Vector3(4, 4, 4), "stair", Color.red);
            MazeController.Instance.CreateDebugCube(stairMap.TopStair, new Vector3(4, 4, 4), "stair", Color.magenta);
            MazeController.Instance.CreateDebugCube(stairMap.BufferL, new Vector3(4, 4, 4), "stair", Color.yellow);
            MazeController.Instance.CreateDebugCube(stairMap.BufferR, new Vector3(4, 4, 4), "stair", Color.green);

            stairMap.AddToGrid(this.Maze.Grid);

            this.PreMappedStairCells.Add(stairMap);
        }
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

    /// <summary>
    /// Remove an existing <see cref="HallwayMap"/> from the collection.
    /// </summary>
    /// <param name="map"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
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
