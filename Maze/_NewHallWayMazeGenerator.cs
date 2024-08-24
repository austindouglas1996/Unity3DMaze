using System;
using System.Threading.Tasks;
using UnityEngine;

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

public class NewHallwayMazeGenerator : MazeGenerator<HallwayMono>
{
    [Tooltip("Basic prefab with 4 ways that can be distributed to create hallways.")]
    [SerializeField] private GameObject HallwayPrefab;

    [Tooltip("Basic prefab for 3D hallways. God help us all.")]
    [SerializeField] private GameObject StairwayPrefabT;
    [SerializeField] private GameObject StairwayPrefabB;

    protected override Task OnGenerate(object[] args)
    {
        throw new System.NotImplementedException();
    }

    protected override Task OnResetGenerator()
    {
        throw new System.NotImplementedException();
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