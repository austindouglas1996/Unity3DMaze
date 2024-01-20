using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Extends <see cref="MazeController"/> to generator hallways connecting rooms throughout a maze to help create a more unique
/// maze than just connecting <see cref="RoomMono"/> together.
/// </summary>
[RequireComponent(typeof(MazeController))]
public class MazeHallwayGenerator : MonoBehaviour, IGenerator<HallwayMono>
{
    [Tooltip("Basic prefab with 4 ways that can be distributed to create hallways.")]
    [SerializeField] private GameObject HallwayPrefab;

    /// <summary>
    /// Contains a list of hallway roots that start out on the extra room doors.
    /// </summary>
    private List<HallwayMono> HallwayRoots = new List<HallwayMono>();

    /// <summary>
    /// Contains a list of hallway cells that connect back to the hallway roots. After generation
    /// this list should be empty, or the cells remaining are not connected to a root.
    /// </summary>
    private List<HallwayMono> HallwayCells = new List<HallwayMono>();

    /// <summary>
    /// Contains a list of <see cref="Vector3Int"/> of already merged cell positions to not try to merge them again.
    /// </summary>
    private List<Vector3Int> MergedCellPositions = new List<Vector3Int>();

    /// <summary>
    /// Gets the <see cref="Maze"/> instance to use for accessing important properties.
    /// </summary>
    private MazeController Maze;

    /// <summary>
    /// Tells whether hallways have finished generation to stop
    /// <see cref="FindPossibleBridges"/> from failing due to <see cref="GenerateHallways"/> from
    /// throwing a collection was modified exception.
    /// </summary>
    private bool hallwayGenerated = false;

    /// <summary>
    /// Returns whether <see cref="Generate"/> has been called.
    /// </summary>
    public bool GenerateCalled { get; private set; }

    /// <summary>
    /// Returns whether <see cref="Generate"/> has finished.
    /// </summary>
    public bool GenerateFinished { get; private set; }

    /// <summary>
    /// Gets a list of generated hallways from <see cref="MazeHallwayGenerator"/> instance.
    /// </summary>
    public List<HallwayMono> Generated
    {
        get;
        private set;
    }

    /// <summary>
    /// Generate the <see cref="HallwayMono"/> instances around the <see cref="MazeController"/> <see cref="RoomMono"/>s
    /// </summary>
    /// <returns></returns>
    public async Task Generate()
    {
        if (this.GenerateCalled) return;
        this.GenerateCalled = true;

        // Create the roots and cells.
        this.CreateHallwayRoots();
        await this.CreateHallwayCells();
        this.FindPossibleBridges();

        // Generate the hallways. The hallways will handle important things like
        // what walls to display, allowable props. This is important info to have
        // before we continue and combine cells.
        await GenerateHallways();

        // Find and merge nearby cells.
        await FindAdjacentHallwayCells();

        // Combine lists.
        Generated = HallwayRoots;

        this.GenerateFinished = true;
    }

    /// <summary>
    /// Called when the <see cref="MazeHallwayGenerator"/> class is initialized.
    /// </summary>
    private void Start()
    {
        Maze = this.GetComponent<MazeController>();
    }

    /// <summary>
    /// Create the hallway roots by locating available doors in maze rooms.
    /// </summary>
    private void CreateHallwayRoots()
    {
        foreach (RoomMono room in Maze.Rooms.Generated)
        {
            var pairs = Maze.DoorRegistry.GetAvailable(room);
            foreach (DoorPair pair in pairs)
            {
                bool result = CreateHallwayRoot(pair);
            }
        }
    }

    /// <summary>
    /// Create the hallway cells by placing cells on the X and Z until reaching the destination.
    /// </summary>
    private async Task CreateHallwayCells()
    {
        foreach (HallwayMono rootA in HallwayRoots)
        {
            foreach (HallwayMono rootB in HallwayRoots)
            {
                if (rootA == rootB) continue;
                CreateHallwayPath(rootA, rootB);
            }
        }

        await Task.Delay(500);

        this.hallwayGenerated = true;
    }

    /// <summary>
    /// Find possible 'bridge' connections where two hallways are two spots away.
    /// See: https://i.gyazo.com/92198e40ce001212525f9d83565f3b18.png
    /// <remarks>After testing I am not seeing the results I want, but sometimes it does make some cool
    /// designs but most of the time it just leaves me sad in for whatever reason not properly connecting
    /// roots. Might be something to come back to.</remarks>
    /// </summary>
    private void FindPossibleBridges()
    {
        List<Cell> connectedBridges = new List<Cell>();

        // Connect all cells.
        List<HallwayMono> combined = new List<HallwayMono>(HallwayRoots);
        List<HallwayMono> connected = new List<HallwayMono>();
        combined.AddRange(HallwayCells);

        foreach (HallwayMono hallway in combined)
        {
            if (connected.Contains(hallway))
                continue;

            // Hallways at this point only have one cell.
            Cell hCell = hallway.GridBounds[0];

            HallwayMono A = hCell.Room.GetComponent<HallwayMono>();

            // Find neighbor cells.
            List<Cell> bridgeNeighbors = GetNeighborHallwayBridgeCandidates(hCell);

            foreach (Cell bridgeCell in bridgeNeighbors)
            {
                // Make sure these two do not have an existing connection.
                if (Maze.DoorRegistry.HasConnection(hCell.Room, bridgeCell.Room)) continue;

                // Make sure not a child of the same room.
                if (hCell.Room == bridgeCell.Room) continue;

                // Create a bridge to connect them.
                // At this point we know both rooms are <see cref="Hallway"/> instances.
                HallwayMono B = bridgeCell.Room.GetComponent<HallwayMono>();
                
                // This cell is already known.
                if (connected.Contains(B))
                    continue;

                // Add this connection.
                connected.Add(B);

                HallwayMono newCell = CreateHallwayCell(A, B);
                if (newCell != null) break;
            }

            connected.Add(A);
        }
    }

    /// <summary>
    /// Loop through hallway roots and connect each root to one another. Each root requires at least one connection.
    /// Once complete we will allow the hallways to handle thier own logic on what walls and props to do.
    /// </summary>
    private async Task GenerateHallways()
    {
        foreach (HallwayMono root in HallwayRoots)
            await root.Generate(this.Maze);

        foreach (HallwayMono cell in HallwayCells)
            await cell.Generate(this.Maze);
    }

    /// <summary>
    /// Combine hallways into one "room". This will help decrease processing needs instead of having all hallways
    /// as one room we will now have one dynamic room.
    /// </summary>
    /// <returns></returns>
    private async Task FindAdjacentHallwayCells()
    {
        List<Cell> rootCells = Maze.Grid.Cells.Where(r => r.Type == CellType.Hallway).ToList();

        foreach (Cell root in rootCells)
        {
            MergedCellPositions.Add(root.Position);

            foreach (Cell neighbor in GetNeighborHallwayCells(root, CellType.Hallway))
            {
                await MergeHallwayCells(root, neighbor);
            }
        }
    }

    /// <summary>
    /// Recursively connects hallway cells to a root cell, forming a cohesive structure within a generated environment.
    /// </summary>
    /// <param name="root">The primary hallway cell to which other hallway cells will be connected.</param>
    /// <param name="neighbor">The adjacent hallway cell to be considered for connection.</param>
    /// <returns>An awaitable task that completes when the connection process is finished.</returns>
    private async Task MergeHallwayCells(Cell root, Cell neighbor)
    {
        if (!MergedCellPositions.Contains(neighbor.Position))
        {
            MergeHallwayRooms(root.Room, neighbor.Room);
            MergedCellPositions.Add(neighbor.Position);
        }

        List<Cell> neighbors = GetNeighborHallwayCells(neighbor, CellType.Hallway);
        foreach (Cell cell in neighbors)
        {
            if (cell == root) continue;
            if (MergedCellPositions.Contains(cell.Position)) continue;

            await MergeHallwayCells(root, cell);
        }
    }

    /// <summary>
    /// Merges the children of two Room GameObjects, transferring them from one to the other and destroying the original parent.
    /// </summary>
    /// <remarks>
    /// This method is primarily used to combine Room cells.
    /// </remarks>
    /// <param name="root">The Room GameObject that will become the new parent for the children of B.</param>
    /// <param name="B">The Room GameObject whose children will be transferred to the root GameObject.</param>
    private void MergeHallwayRooms(RoomMono root, RoomMono B, bool destroyOnMerge = true)
    {
        List<Transform> childrenToReparent = new List<Transform>();

        // Take all children objects. Unity does not play nice
        // if we don't load it into a list first.
        foreach (Transform piece in B.GetComponentInChildren<Transform>(true))
        {
            if (piece == B.transform) continue;
            childrenToReparent.Add(piece);
        }

        // Now we can take it's children.
        foreach (Transform piece in childrenToReparent)
        {
            piece.parent = root.transform;
        }

        // Does this cell have any doors?
        foreach (var pair in Maze.DoorRegistry.Get(B))
        {
            Maze.DoorRegistry.SetConnection(pair.Door, root);
        }

        // Does this cell have any children connections?
        foreach (var pair in Maze.DoorRegistry.GetConnections(B))
        {
            Maze.DoorRegistry.SetConnection(pair.Door, root);
        }

        // Change grid cells to use the root cell.
        foreach (Cell cells in B.GridBounds)
        {
            cells.Room = root;
        }

        // Destroy.
        if (destroyOnMerge)
            Destroy(B.gameObject);
    }

    /// <summary>
    /// Generate the <see cref="HallwayMono"/> root instances from available <see cref="RoomMono"/> <see cref="DoorPair"/> instances.
    /// </summary>
    /// <param name="pair"></param>
    /// <returns></returns>
    /// <exception cref="System.Exception"></exception>
    private bool CreateHallwayRoot(DoorPair pair)
    {
        GameObject door = pair.Door;

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

        GameObject newRoot = InstantiateHallway(position.RoundToInt(), quaternion.identity, this.transform);

        // Was there a collision?
        if (newRoot == null)
            return false;

        // Add to hallways and register the new door.
        HallwayMono hallway = newRoot.GetComponent<HallwayMono>();
        hallway.Direction = direction.Reverse();
        HallwayRoots.Add(hallway);
        Maze.DoorRegistry.SetConnection(pair.Door, hallway);

        return true;
    }

    /// <summary>
    /// Generate a hallway path from two different <see cref="HallwayMono"/> instances.
    /// </summary>
    /// <param name="A"></param>
    /// <param name="B"></param>
    private void CreateHallwayPath(HallwayMono A, HallwayMono B, bool ignoreCollision = false)
    {
        // Bounds of both roots.
        Bounds AB = A.transform.BoundingBox();
        Bounds BB = B.transform.BoundingBox();

        // Position of each root.
        Vector3 APOS = A.transform.position;
        Vector3 BPOS = B.transform.position;

        // Don't allow hallway paths that are outside a small range.
        if (Vector3.Distance(APOS, BPOS) > 30)
            return;

        // Only connect roots on same X/Z
        if (APOS.y != BPOS.y)
            return;

        // Get (possibly) best direction.
        Vector3 direction = GetBestDirection(APOS.RoundToInt(), BPOS.RoundToInt());
        
        if (direction.x == 1)
        {
            Vector3 currentPosition = CreateHallwayCellX(APOS, A, B, ignoreCollision);
            CreateHallwayCellZ(currentPosition, A, B);
        }
        else
        {
            Vector3 currentPosition = CreateHallwayCellZ(APOS, A, B, ignoreCollision);
            CreateHallwayCellX(currentPosition, A, B);
        }
    }

    /// <summary>
    /// Determine what is mostly the best possible place to start with creating the hallway. X or Z?
    /// </summary>
    /// <param name="APOS"></param>
    /// <param name="BPOS"></param>
    /// <returns>A <see cref="Vector3"/> with a 1 in the place of the variable you should start with. Default is Z.</returns>
    private Vector3 GetBestDirection(Vector3Int APOS, Vector3Int BPOS)
    {
        Vector3Int TempA = APOS;
        Vector3Int TempB = APOS;

        // Determine if we need to go RIGHT or LEFT.
        if (APOS.z - BPOS.z < 0)
            TempA.z += 4;
        else
            TempA.z -= 4;

        // Determine if we need to go UP or DOWN.
        if (APOS.x - BPOS.x < 0)
            TempB.x += 4;
        else
            TempB.x -= 4;

        CellType X = Maze.Grid[TempA].Type;
        CellType Z = Maze.Grid[TempB].Type;

        if (X == CellType.None)
            return new Vector3(1, 0, 0);

        return new Vector3(0, 0, 1);
    }

    /// <summary>
    /// Generate hallway cells on the X-axis.
    /// </summary>
    /// <param name="currentPosition"></param>
    /// <param name="A"></param>
    /// <param name="B"></param>
    /// <returns></returns>
    private Vector3 CreateHallwayCellX(Vector3 currentPosition, HallwayMono A, HallwayMono B, bool ignoreCollision = false)
    {
        // Bounds of both roots.
        Bounds AB = A.transform.BoundingBox();
        Bounds BB = B.transform.BoundingBox();

        // Position of each root.
        Vector3 APOS = currentPosition;
        Vector3 BPOS = B.transform.position;

        // Determine if we're going up or down.
        bool positive = DetermineIfPositiveOrNegative(APOS.x, BPOS.x);

        while (positive && APOS.x - BPOS.x < 0 || !positive && APOS.x - BPOS.x > 0)
        {
            // Determine if we need to go RIGHT or LEFT.
            if (positive)
                APOS.x += 4;
            else
                APOS.x -= 4;

            // Default value for a cell is to return CellType.None. This just means the grid does not know about this value.
            Cell foundCell = Maze.Grid[APOS.RoundToInt()];

            // Add a new hallway?
            if (foundCell.Type != CellType.None)
            {
                continue;
            }

            if (ignoreCollision)
            {
                GameObject newHallwayCell = InstantiateHallway(APOS.RoundToInt(), quaternion.identity, this.transform, false);

                if (newHallwayCell != null)
                    HallwayCells.Add(newHallwayCell.GetComponent<HallwayMono>());
            }
            else
            {
                Cell up = Maze.Grid.Neighbor(foundCell, SpatialOrientation.Up);
                Cell down = Maze.Grid.Neighbor(foundCell, SpatialOrientation.Down);
                if (up.Type != CellType.Hallway || down.Type != CellType.Hallway)
                {
                    GameObject newHallwayCell = InstantiateHallway(APOS.RoundToInt(), quaternion.identity, this.transform);

                    if (newHallwayCell != null)
                        HallwayCells.Add(newHallwayCell.GetComponent<HallwayMono>());
                }
            }
        }

        return APOS;
    }

    /// <summary>
    /// Create a new singular hallway cell. Helper method for <see cref="FindPossibleBridges"/> function.
    /// </summary>
    /// <param name="A"></param>
    /// <param name="B"></param>
    /// <returns></returns>
    private HallwayMono CreateHallwayCell(HallwayMono A, HallwayMono B)
    {
        // Position of each root.
        Vector3 APOS = A.transform.position;
        Vector3 BPOS = B.transform.position;

        // Determine if we're going up or down or left & right.
        bool positiveX = DetermineIfPositiveOrNegative(APOS.x, BPOS.x);
        bool positiveZ = DetermineIfPositiveOrNegative(APOS.z, BPOS.z);

        if (APOS.x != BPOS.x)
        {
            if (positiveX)
                APOS.x += 4;
            else
                APOS.x -= 4;
        }

        if (APOS.z != BPOS.z)
        {
            if (positiveZ)
                APOS.z += 4;
            else
                APOS.z -= 4;
        }

        if (Maze.Grid[APOS.RoundToInt()].Type != CellType.None) return null;

        GameObject newHallwayCell = InstantiateHallway(APOS.RoundToInt(), quaternion.identity, this.transform, false);
        HallwayMono newHallway = newHallwayCell.GetComponent<HallwayMono>();

        if (newHallwayCell != null)
            HallwayCells.Add(newHallway);

        return newHallway;
    }

    /// <summary>
    /// Generate hallway cells on the Z-axis.
    /// </summary>
    /// <param name="currentPosition"></param>
    /// <param name="A"></param>
    /// <param name="B"></param>
    /// <returns></returns>
    private Vector3 CreateHallwayCellZ(Vector3 currentPosition, HallwayMono A, HallwayMono B, bool ignoreCollision = false)
    {
        // Bounds of both roots.
        Bounds AB = A.transform.BoundingBox();
        Bounds BB = B.transform.BoundingBox();

        // Position of each root.
        Vector3 APOS = currentPosition;
        Vector3 BPOS = B.transform.position;

        // Determine if we're going up or down.
        bool positive = DetermineIfPositiveOrNegative(APOS.z, BPOS.z);

        while (positive && APOS.z - BPOS.z < 0 || !positive && APOS.z - BPOS.z > 0)
        {
            // Determine if we need to go RIGHT or LEFT.
            if (positive)
                APOS.z += 4;
            else
                APOS.z -= 4;

            // Default value for a cell is to return CellType.None. This just means the grid does not know about this value.
            Cell foundCell = Maze.Grid[APOS.RoundToInt()];

            // Add a new hallway?
            if (foundCell.Type != CellType.None)
            {
                continue;
            }

            if (ignoreCollision)
            {
                GameObject newHallwayCell = InstantiateHallway(APOS.RoundToInt(), quaternion.identity, this.transform, false);

                if (newHallwayCell != null)
                    HallwayCells.Add(newHallwayCell.GetComponent<HallwayMono>());
            }
            else
            {
                // For the Z we do things a bit differently. To reduce clutter, we check if there is already
                // a hallway nearby so we can try to avoid the two lane hallways when possible. When I tried
                // doing this for the X too, it broke in intersections and was quite weird. We may be able to 
                // do the same for X.
                Cell left = Maze.Grid.Neighbor(foundCell, SpatialOrientation.Left);
                Cell right = Maze.Grid.Neighbor(foundCell, SpatialOrientation.Right);
                if (left.Type != CellType.Hallway || right.Type != CellType.Hallway)
                {
                    GameObject newHallwayCell = InstantiateHallway(APOS.RoundToInt(), quaternion.identity, this.transform);

                    if (newHallwayCell != null)
                        HallwayCells.Add(newHallwayCell.GetComponent<HallwayMono>());
                }
            }
        }

        return APOS;
    }

    /// <summary>
    /// Initialize a new <see cref="HallwayMono"/> object with collision checking, and grid additions.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="parent"></param>
    /// <param name="checkForCollision"></param>
    /// <returns></returns>
    private GameObject InstantiateHallway(Vector3Int position, Quaternion rotation, Transform parent, bool checkForCollision = true)
    {
        GameObject newHallway = Instantiate(HallwayPrefab, position, rotation, parent); 
        Bounds newRootBounds = newHallway.transform.BoundingBox();

        if (checkForCollision)
        {
            bool roomCollision = CheckForRoomCollision(newRootBounds, position);
            bool rootCollision = CheckForHallwayRootCollision(newHallway.GetComponent<HallwayMono>());
            if (roomCollision || rootCollision)
            {
                Destroy(newHallway);
                return null;
            }
        }

        // Grab the hallway.
        HallwayMono hallwayProp = newHallway.GetComponent<HallwayMono>();

        // Add bounds to grid.
        Maze.Grid.AddBounds(hallwayProp, newRootBounds, position, CellType.Hallway);

        return newHallway;
    }

    /// <summary>
    /// Check if a <see cref="Bounds"/> collides with a <see cref="RoomMono"/> in the <see cref="Maze"/>
    /// </summary>
    /// <param name="bounds"></param>
    /// <returns></returns>
    private bool CheckForRoomCollision(Bounds bounds, Vector3 position)
    {
        foreach (RoomMono room in Maze.Rooms.Generated)
        {
            if (RoomMono.CheckForIntersection(room, bounds, position) || RoomMono.CheckForContains(room, bounds, position))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Check if a <see cref="Bounds"/> collides with a <see cref="HallwayMono"/> root.
    /// </summary>
    /// <param name="bounds"></param>
    /// <returns></returns>
    private bool CheckForHallwayRootCollision(HallwayMono initialHallway)
    {
        foreach (HallwayMono root in HallwayRoots)
        {
            if (root == initialHallway) continue;

            Bounds hallwayBounds = root.transform.BoundingBox();

            if (RoomMono.CheckForIntersection(initialHallway.transform.BoundingBox(), hallwayBounds, initialHallway.transform.position, root.transform.position))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Get a list of neighbor <see cref="HallwayMono"/> cells.
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    private List<Cell> GetNeighborHallwayCells(Cell cell, CellType type, int distance = 1)
    {
        List<Cell> neighbors = new List<Cell>();

        Cell up = Maze.Grid.Neighbor(cell, SpatialOrientation.Up, distance);
        Cell right = Maze.Grid.Neighbor(cell, SpatialOrientation.Right, distance);
        Cell down = Maze.Grid.Neighbor(cell, SpatialOrientation.Down, distance);
        Cell left = Maze.Grid.Neighbor(cell, SpatialOrientation.Left, distance);

        if (up.Type == type)
            neighbors.Add(up);
        if (right.Type == type)
            neighbors.Add(right);
        if (down.Type == type)
            neighbors.Add(down);
        if (left.Type == type)
            neighbors.Add(left);

        return neighbors;
    }

    /// <summary>
    /// Gets a list of neighbor <see cref="HallwayMono"/> cells that are 2 cells away with the first being an empty cell.
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    private List<Cell> GetNeighborHallwayBridgeCandidates(Cell cell)
    {
        List<Cell> neighbors = new List<Cell>();

        foreach (Cell ncell in GetNeighborHallwayCells(cell, CellType.None, 1))
        {
            foreach (Cell ncell1 in GetNeighborHallwayCells(ncell, CellType.Hallway, 1))
            {
                neighbors.Add(ncell1);
            }
        }
        return neighbors;
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
}