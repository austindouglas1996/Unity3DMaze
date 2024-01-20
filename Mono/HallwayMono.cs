using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using VHierarchy.Libs;

/// <summary>
/// Represents a hallway within a maze. A one block tile handles it's walls based on what's around it.
/// </summary>
public class HallwayMono : RoomMono
{
    /// <summary>
    /// The original facing direction of the room. Helper for <see cref="HallwayMono"/>.
    /// </summary>
    public SpatialOrientation Direction = SpatialOrientation.None;

    /// <summary>
    /// Generate the hallway properties like prop information, and wall visiblity.
    /// </summary>
    /// <returns></returns>
    public async Task Generate(MazeController maze)
    {
        if (maze == null)
            throw new System.ArgumentNullException(nameof(maze));

        while (!this.GenerateFinished)
        {
            await Task.Delay(10);
        }

        this.GenerateFinished = false;

        // Simple bool to know what is visible.
        bool up = true, right = true, down = true, left = true;

        MazeGrid Grid = maze.Grid;
        Cell Cell = GridBounds[0];

        Cell upC = Grid.Neighbor(Cell, SpatialOrientation.Up);
        Cell downC = Grid.Neighbor(Cell, SpatialOrientation.Down);

        Cell rightC = Grid.Neighbor(Cell, SpatialOrientation.Right);
        Cell leftC = Grid.Neighbor(Cell, SpatialOrientation.Left);

        up = await HandleWallVisibility(upC, maze, SpatialOrientation.Up);
        right = await HandleWallVisibility(rightC, maze, SpatialOrientation.Right);
        down = await HandleWallVisibility(downC, maze, SpatialOrientation.Down);
        left = await HandleWallVisibility(leftC, maze, SpatialOrientation.Left);

        // Asign the sizes of props allowed depending on some rules.
        AssignPropSizes(up, right, down, left);

        await this.GenerateProps();

        this.GenerateFinished = true;
    }

    /// <summary>
    /// Assign the wall properties depending on a few factors.
    /// </summary>
    /// <param name="up"></param>
    /// <param name="right"></param>
    /// <param name="down"></param>
    /// <param name="left"></param>+
    private void AssignPropSizes(bool up, bool right, bool down, bool left)
    {
        Transform floor = this.transform.Find("Floor");
        if (floor == null)
            throw new System.ArgumentNullException("Floor does not exist.");

        // if the floor is a trap, don't spawn anything.
        if (floor.GetComponent<RoomFixtureMono>().Behavior == RoomFixtureBehaviorType.Trap)
        {
            SetPropSize(SpatialOrientation.Right, PropSize.SuperSmall);
            SetPropSize(SpatialOrientation.Left, PropSize.SuperSmall); 
            SetPropSize(SpatialOrientation.Up, PropSize.SuperSmall);
            SetPropSize(SpatialOrientation.Down, PropSize.SuperSmall);
            return;
        }

        // Simple don't allow two large sizes.
        if (up && down)
        {
            // Up
            if (RandomHelper.Chance(50))
            {
                SetPropSize(SpatialOrientation.Up, PropSize.Medium);
                SetPropSize(SpatialOrientation.Down, PropSize.SuperSmall);
            }
            else
            {
                SetPropSize(SpatialOrientation.Up, PropSize.SuperSmall);
                SetPropSize(SpatialOrientation.Down, PropSize.Medium);
            }
        }
        else if (right && left)
        {
            // Right
            if (RandomHelper.Chance(50))
            {
                SetPropSize(SpatialOrientation.Right, PropSize.Medium);
                SetPropSize(SpatialOrientation.Left, PropSize.SuperSmall);
            }
            else
            {
                SetPropSize(SpatialOrientation.Right, PropSize.SuperSmall);
                SetPropSize(SpatialOrientation.Left, PropSize.Medium);
            }
        }

        // Corner pieces.
        if (left && up || right & up || left & down || right & down)
        {
            SetPropSize(SpatialOrientation.Right, PropSize.SuperSmall);
            SetPropSize(SpatialOrientation.Left, PropSize.SuperSmall);
            SetPropSize(SpatialOrientation.Up, PropSize.SuperSmall);
            SetPropSize(SpatialOrientation.Down, PropSize.SuperSmall);
        }

        // Special prop:
        int wallsShown = 0;
        if (up)
            wallsShown++;
        if (right)
            wallsShown++;
        if (left)
            wallsShown++;
        if (down) 
            wallsShown++;

        // Allow a special prop.
        if (wallsShown == 3)
        {
            if (!up)
            {
                SetPropSize(SpatialOrientation.Down, PropSize.SuperSmall);
                SetPropSize(SpatialOrientation.Right, PropSize.SuperSmall);
                SetPropSize(SpatialOrientation.Left, PropSize.SuperSmall);

                SetPropSize(SpatialOrientation.Up, PropSize.Special); 
                SetFacingDirection(SpatialOrientation.Up, SpatialOrientation.Down);
            }
            if (!right)
            {
                SetPropSize(SpatialOrientation.Up, PropSize.SuperSmall);
                SetPropSize(SpatialOrientation.Down, PropSize.SuperSmall);
                SetPropSize(SpatialOrientation.Left, PropSize.SuperSmall);

                SetPropSize(SpatialOrientation.Right, PropSize.Special); SetFacingDirection(SpatialOrientation.Right, SpatialOrientation.Left);
            }
            if (!left)
            {
                SetPropSize(SpatialOrientation.Up, PropSize.SuperSmall);
                SetPropSize(SpatialOrientation.Down, PropSize.SuperSmall);
                SetPropSize(SpatialOrientation.Right, PropSize.SuperSmall);

                SetPropSize(SpatialOrientation.Left, PropSize.Special); SetFacingDirection(SpatialOrientation.Left, SpatialOrientation.Right);
            }
            if (!down)
            {
                SetPropSize(SpatialOrientation.Up, PropSize.SuperSmall);
                SetPropSize(SpatialOrientation.Right, PropSize.SuperSmall);
                SetPropSize(SpatialOrientation.Left, PropSize.SuperSmall);

                SetPropSize(SpatialOrientation.Down, PropSize.Special); SetFacingDirection(SpatialOrientation.Down, SpatialOrientation.Up);
            }
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
    /// Handle the wall visibility along with other options like destroyable walls.
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="direction"></param>
    /// <returns></returns>
    private async Task<bool> HandleWallVisibility(Cell cell, MazeController maze, SpatialOrientation direction)
    {
        if (cell.Type == CellType.None && direction != Direction) return true;

        bool value = true;

        // Hide the hallway.
        value = SetHallwayWallVisible(direction, false);

        // Check if there is a removable wall.
        if (cell.Type == CellType.Room && cell.Room != null)
        {
            List<Transform> neighbors = cell.Room.FindNeighborPieces(cell.Position, RoomFixtureIdentityType.Any, RoomFixtureBehaviorType.HallwayDoor);
            foreach (Transform t in neighbors)
            {
                GameObject newDoor = await cell.Room.TurnWallIntoDoor(t.gameObject, direction, cell.Room);
                maze.DoorRegistry.Add(newDoor, cell.Room, this);
                newDoor.name = "CHANGED";
            }
        }

        return false;
    }

    /// <summary>
    /// Set the prop size of one of the objects.
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="size"></param>
    private void SetPropSize(SpatialOrientation direction, PropSize size)
    {
        Transform go = Get(direction);
        if (go == null)
        {
            throw new System.ArgumentNullException("Unable to find fixture at direction " + direction);
        }

        RoomFixtureMono piece = go.GetComponent<RoomFixtureMono>();
        piece.Size = size;
    }

    /// <summary>
    /// Set the direction a prop should be facing.
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="propFacingDirection"></param>
    private void SetFacingDirection(SpatialOrientation direction, SpatialOrientation propFacingDirection)
    {
        Transform go = Get(direction);
        RoomFixtureMono piece = go.GetComponent<RoomFixtureMono>();
        //piece.Direction = propFacingDirection;
        Debug.LogError("HallwayMono line 252 tried set direction.");
    }

    /// <summary>
    /// This will change the visibility of walls in the HallwayPrefab.
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="visible"></param>
    /// <exception cref="System.Exception"></exception>
    private bool SetHallwayWallVisible(SpatialOrientation direction, bool visible)
    {
        Transform obj = Get(direction);
        if (obj != null)
        {
            obj.gameObject.Destroy();
        }

        return visible;
    }
}