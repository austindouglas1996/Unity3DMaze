using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class PathFindRunner : MonoBehaviour
{
    [Tooltip("Helps with determining paths for characters in the grid.")]
    [SerializeField] public GridCellPathFinder PathFinder;

    /// <summary>
    /// Current instructions for cell movement.
    /// </summary>
    private List<Cell> MoveInstructions = new List<Cell>();

    /// <summary>
    /// Event invoked when we reach the destination cell.
    /// </summary>
    public event EventHandler Finished;

    /// <summary>
    /// Cancel the current pathfinding.
    /// </summary>
    public void Cancel()
    {
        this.MoveInstructions.Clear();
    }

    /// <summary>
    /// Set the destination to a new cell.
    /// </summary>
    /// <param name="pos"></param>
    public void MoveTo(Vector3Int pos)
    {
        this.PathFinder.Grid.Find(pos, new Vector3(2f, 4f, 2f));
    }

    /// <summary>
    /// Set the destination to a new cell.
    /// </summary>
    /// <param name="pos"></param>
    public void MoveTo(Cell pos)
    {
        var result = this.PathFinder.FindPath(this.GetCurrentCell(), pos);
        if (result != null)
            MoveInstructions = result;
    }

    /// <summary>
    /// Invokes the <see cref="UpdateMovement(Cell, Cell)"/> method. If returned true will choose a new cell.
    /// </summary>
    private void Update()
    {
        if (MoveInstructions != null && MoveInstructions.Count > 0)
        {
            if (UpdateMovement(GetCurrentCell(), MoveInstructions[0]))
            {
                SetNextStep();
            }
        }
    }

    /// <summary>
    /// Update the movement of the current process. Return true if we have reached the destination.
    /// </summary>
    /// <param name="currentCell"></param>
    /// <param name="destinationCell"></param>
    /// <returns></returns>
    protected abstract bool UpdateMovement(Cell currentCell, Cell destinationCell);

    /// <summary>
    /// Return the current cell this enity is near.
    /// </summary>
    /// <returns></returns>
    protected virtual Cell GetCurrentCell()
    {
        return this.PathFinder.Grid.Find(this.transform.position.RoundToInt(), new Vector3(2f, 4f, 2f));
    }

    /// <summary>
    /// Set the next cell for the destination to be set to.
    /// </summary>
    protected virtual void SetNextStep()
    {
        this.MoveInstructions.RemoveAt(0);

        if (this.MoveInstructions.Count == 0)
        {
            this.Finished?.Invoke(this, EventArgs.Empty);
        }
    }
}