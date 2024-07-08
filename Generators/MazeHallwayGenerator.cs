using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
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

    public bool LeftV = true;
    public bool RightV = true;
    public bool UpV = true;
    public bool BottomV = true;

    public DoorPair DoorPair = null;
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
    [SerializeField] private GameObject StairwayPrefab;

    /// <summary>
    /// Gets the <see cref="Maze"/> instance to use for accessing important properties.
    /// </summary>
    private MazeController Maze;

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

    private MazeGrid MapGrid = new MazeGrid();
    private List<HallwayMono> HallwayCells = new List<HallwayMono>();
    private List<HallwayMap> PreMappedCells = new List<HallwayMap>();

    /// <summary>
    /// Called when the generator is initialized.
    /// </summary>
    private void Start()
    {
        Maze = this.GetComponent<MazeController>();
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
        await this.MapPathCells();

        // Find possible gaps between cells to create unique bridges.
        this.MapPathBridges();

        // Create 'noise' in the maze.
        this.MapPathAlleys();

        // Create the pathing for each maze cell.
        // This is like a cleanup.
        await this.MapPathing();
        await this.MapPathing();

        // Final touchups before deploying.
        this.MapDetails();

        // Finally, commit our changes to the Maze.
        await this.CommitCells();

        this.GenerateFinished = true;
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

            HallwayMap newMap = this.CreateMap(position.RoundToInt(), true, pair);
            if (newMap == null)
                Debug.LogWarning("Root hallway was not created.");
        }
    }

    /// <summary>
    /// Go through each root cell and try to connect them with the others. This creates pathing throughout the maze.
    /// </summary>
    /// <returns></returns>
    private async Task MapPathCells()
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

        await Task.Delay(100);
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
                List<Cell> neighbors = GetBestNeighbors(cell.Position,1).Where(r => r.Type == CellType.None).ToList();
                if (neighbors.Count() == 0) continue;

                Cell chosenCell = neighbors.Random();

                this.CreateMap(chosenCell.Position, false);
            }
        }
    }

    /// <summary>
    /// Determines map walls and if they should be open, closed, or destroyed.
    /// </summary>
    private async Task MapPathing()
    {
        foreach (var map in PreMappedCells.ToList())
        {
            CellDirectionalGroup neighbors = GetBestNeighborsAll(map.Position, 1);

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

            if (neighbors.Up.Type != CellType.None)
                map.UpV = false;

            if (neighbors.Left.Type != CellType.None)
                map.LeftV = false;

            if (neighbors.Right.Type != CellType.None)
                map.RightV = false;

            if (neighbors.Down.Type != CellType.None)
                map.BottomV = false;
        }
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
        // Take our map and create into objects.
        foreach (var map in this.PreMappedCells)
        {
            GameObject inst = Instantiate(HallwayPrefab, map.Position, quaternion.identity, this.transform);
            HallwayMono newHall = inst.GetComponent<HallwayMono>();

            // Root cells are connected to a door.
            if (map.IsRoot)
            {
                // Set a connection with the door.
                this.Maze.DoorRegistry.SetConnection(map.DoorPair.Door, newHall);
            }

            // Generate.
            await newHall.Generate(map);

            // Add to generated for later processing.
            Generated.Add(newHall);
        }

        // Take our temporary grid and put into the maze grid.
        foreach (var cell in MapGrid.Cells)
        {
            this.Maze.Grid.Add(cell.Position, CellType.Hallway);
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

        // Don't allow hallway paths that are outside a small range.
        if (Vector3.Distance(APOS, BPOS) > 30)
            return;

        // Only connect hallway roots on same X/Z
        // Let the record show we started here on 7/2.
        // This If had a return in case we forget from the PTSD.
        // If I'm not back 7/12 send help.
        //
        // 7/7 bonjour, SOS.
        if (APOS.y != BPOS.y)
            return;

        // Get (possibly) best direction.
        Vector3 direction = DetermineDirectionBetweenPoints(APOS, BPOS);

        if (direction.x == 1)
        {
            Vector3Int currentPosition = CreatePathBetweenCellsX(APOS, A.Position, B.Position);
            CreatePathBetweenCellsZ(currentPosition, A.Position, B.Position);
        }
        else
        {
            Vector3Int currentPosition = CreatePathBetweenCellsX(APOS, A.Position, B.Position);
            CreatePathBetweenCellsX(currentPosition, A.Position, B.Position);
        }
    }

    /// <summary>
    /// Create cell instances along a path from one direction to another along the Z axis.
    /// </summary>
    /// <param name="curr"></param>
    /// <param name="A"></param>
    /// <param name="B"></param>
    /// <returns></returns>
    private Vector3Int CreatePathBetweenCellsX(Vector3Int curr, Vector3Int A, Vector3Int B)
    {
        // Determine if we're going up or down.
        bool positive = DetermineIfPositiveOrNegative(curr.x, B.x);

        while (positive && curr.x - B.x < 0 || !positive && curr.x - B.x > 0)
        {
            if (positive)
                curr.x += 4;
            else
                curr.x -= 4;

            this.CreateMap(curr, false);
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
    private Vector3Int CreatePathBetweenCellsZ(Vector3Int curr, Vector3Int A, Vector3Int B)
    {
        // Determine if we're going up or down.
        bool positive = DetermineIfPositiveOrNegative(curr.z, B.z);

        while (positive && curr.z - B.z < 0 || !positive && curr.z - B.z > 0)
        {
            if (positive)
                curr.z += 4;
            else
                curr.z -= 4;

            this.CreateMap(curr, false);
        }

        return curr;
    }

    /// <summary>
    /// Retrieves a list of bridge candidates. Two hallways that are
    /// two cells from one another with a gap.
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    private List<Cell> GetNeighborBridgeCandidates(Vector3Int pos)
    {
        List<Cell> neighbors = new List<Cell>();

        foreach (Cell ncell in GetNeighborHallways(this.MapGrid, pos, 1))
        {
            foreach (Cell ncell1 in GetNeighborHallways(this.MapGrid, pos, 1))
            {
                neighbors.Add(ncell1);
            }
        }

        return neighbors;
    }

    /// <summary>
    /// Look through the <see cref="MapGrid"/> and <see cref="Maze.Grid"/> to find the most suitable
    /// neighbor. This is needed when deteriming for hallway cells if they are next to a room.
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private List<Cell> GetBestNeighbors(Vector3Int pos, int distance)
    {
        List<Cell> localNeighbors = this.MapGrid.DirectNeighbors(this.MapGrid[pos], distance);
        List<Cell> globalNeighbors = this.Maze.Grid.DirectNeighbors(this.Maze.Grid[pos], distance);
        List<Cell> cells = new List<Cell>();

        if (localNeighbors.Count != globalNeighbors.Count)
            throw new ArgumentException("Count must be the same");

        for (int i = 0; i < localNeighbors.Count; i++)
        {
            cells.Add(globalNeighbors[i].Type == CellType.None ? localNeighbors[i] : globalNeighbors[i]);
        }

        return cells;
    }

    /// <summary>
    /// Look through the <see cref="MapGrid"/> and <see cref="Maze.Grid"/> to find the most suitable
    /// neighbor. This is needed when deteriming for hallway cells if they are next to a room.
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private CellDirectionalGroup GetBestNeighborsAll(Vector3Int pos, int distance)
    {
        var localNeighbors = this.MapGrid.AllNeighbors(this.MapGrid[pos], distance);
        var globalNeighbors = this.Maze.Grid.AllNeighbors(this.Maze.Grid[pos], distance);
        List<Cell> cells = new List<Cell>();

        for (int i = 0; i < localNeighbors.Group.Count; i++)
        {
            cells.Add(globalNeighbors.Group[i].Type == CellType.None ? localNeighbors.Group[i] : globalNeighbors.Group[i]);
        }

        return new CellDirectionalGroup(cells);
    }

    /// <summary>
    /// Retrieves a list of neighbor cells that are also hallways.
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="pos"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    private List<Cell> GetNeighborHallways(MazeGrid grid, Vector3Int pos, int distance = 1)
    {
        List<Cell> neighbors = grid.DirectNeighbors(grid[pos], distance);
        return neighbors.Where(r => r.Type == CellType.Hallway).ToList();
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
        if (!this.CheckIsValid(pos))
        {
            return null;
        }

        HallwayMap newMap = new HallwayMap(pos, isRoot);
        newMap.DoorPair = pair;

        // Set the value.
        this.MapGrid.Add(pos, CellType.Hallway);
        this.PreMappedCells.Add(newMap);

        return newMap;
    }

    private bool RemoveMap(HallwayMap map)
    {
        this.MapGrid.Remove(map.Position);
        this.PreMappedCells.Remove(map);
        return true;
    }

    /// <summary>
    /// Check if a given tile position is valid for a new hallway.
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    private bool CheckIsValid(Vector3Int position)
    {
        var mazeCell = Maze.Grid[position];
        var localCell = this.MapGrid[position];
        var localMap = this.PreMappedCells.Where(r => r.Position == position).ToList();

        // Maze already has a cell here.
        if (mazeCell.Type != CellType.None 
            || localCell.Type != CellType.None
            || localMap.Count != 0)
            return false;

        return true;
    }

    /// <summary>
    /// Returns whether we need to go positive or negative.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    private bool DetermineIfPositiveOrNegative(float a, float b)
    {
        return (a - b < 0);
    }

    /// <summary>
    /// Determine the best direction to move from one position to another. Helps with
    /// navigating cell positions from left to right, or up and down.
    /// </summary>
    /// <param name="APOS"></param>
    /// <param name="BPOS"></param>
    /// <returns></returns>
    private Vector3 DetermineDirectionBetweenPoints(Vector3Int APOS, Vector3Int BPOS)
    {
        // Create temporary positions for checking the direction.
        Vector3Int tempA = new Vector3Int(APOS.x, APOS.y, APOS.z + (APOS.z < BPOS.z ? 4 : -4));
        Vector3Int tempB = new Vector3Int(APOS.x + (APOS.x < BPOS.x ? 4 : -4), APOS.y, APOS.z);

        // Check if moving in the Z direction is possible.
        if (!CheckIsValid(tempA))
        {
            return new Vector3(1, 0, 0); // Move right/left
        }

        // If not, move in the X direction.
        return new Vector3(0, 0, 1); // Move up/down
    }
}