using System;
using System.Threading.Tasks;
using UnityEngine;
using VHierarchy.Libs;

/// <summary>
/// Represents a single room cell that can be combined to form hallways throughout the maze. The difference between this object and a
/// <see cref="Room"/> is that a hallway is one cell. Groups of this object will be created to help with making a hallway.
/// </summary>
public class HallwayMono : RoomMono
{
    [SerializeField] private bool IsHallway = false;

    /// <summary>
    /// The original facing direction of the room. Helper for <see cref="HallwayMono"/>.
    /// </summary>
    public SpatialOrientation Direction = SpatialOrientation.None;

    /// <summary>
    /// Generate the hallway walls and items.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentNullException"></exception>
    protected override async Task OnGenerate(object[] args)
    {
        await base.OnGenerate(args);

        if (!IsHallway)
        {
            HallwayMap map = (HallwayMap)args[0];
            if (map == null) throw new System.ArgumentNullException("HallwayMap must be included.");

            SetHallwayWallVisible(SpatialOrientation.Up, map.UpV);
            SetHallwayWallVisible(SpatialOrientation.Right, map.RightV);
            SetHallwayWallVisible(SpatialOrientation.Left, map.LeftV);
            SetHallwayWallVisible(SpatialOrientation.Down, map.BottomV);
        }
    }

    /// <summary>
    /// Retrieve the game object in a certain direction.
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    /// <exception cref="System.Exception"></exception>
    private Transform Get(SpatialOrientation direction)
    {
        switch (direction)
        {
            case SpatialOrientation.Up:
                return this.transform.Find("UP");
            case SpatialOrientation.Right:
                return this.transform.Find("RIGHT");
            case SpatialOrientation.Down:
                return this.transform.Find("DOWN");
            case SpatialOrientation.Left:
                return this.transform.Find("LEFT");
            default:
                throw new System.Exception("Not here");
        }
    }

    /// <summary>
    /// This will change the visibility of walls in the HallwayPrefab.
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="visible"></param>
    /// <exception cref="System.Exception"></exception>
    private void SetHallwayWallVisible(SpatialOrientation direction, bool visible)
    {
        if (visible)
        {
            return;
        }

        Transform obj = Get(direction);
        obj.gameObject.Destroy();
    }
}