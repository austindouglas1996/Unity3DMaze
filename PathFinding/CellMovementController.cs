using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VHierarchy.Libs;

/// <summary>
/// A helper class for running through the move instructions provided by <see cref="GridCellPathFinder.FindPath(Cell, Cell)"/>. This class will
/// help with moving through the moving cells until reaching the destination cell. Debug cubes are also provided.
/// </summary>
public abstract class CellMovementController : MonoBehaviour
{
    /// <summary>
    /// Helps with pathfinding around the maze.
    /// </summary>
    private GridCellPathFinder PathFinder;

    /// <summary>
    /// Current instructions for cell movement.
    /// </summary>
    private List<Cell> MoveInstructions = new List<Cell>();
    private List<GameObject> DebugCells = new List<GameObject>();

    [SerializeField] public bool ShowDebugCells = true;
    public bool IsMoving { get { return MoveInstructions.Count > 0; } }
    public Cell MovingTo {  get { return MoveInstructions[0]; } }

    /// <summary>
    /// Event invoked when we reach the destination cell.
    /// </summary>
    public event EventHandler Finished;

    /// <summary>
    /// Event invoked when the last action was cancelled.
    /// </summary>
    public event EventHandler Cancelled;

    /// <summary>
    /// Cancel the current pathfinding.
    /// </summary>
    public void Cancel()
    {
        this.MoveInstructions.Clear();
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Set the destination to a new cell.
    /// </summary>
    /// <param name="pos"></param>
    public void MoveTo(Vector3Int pos)
    {
        Cell chosenCell = this.PathFinder.Grid.Find(pos, new Vector3(2f, 4f, 2f));
        if (chosenCell == null)
            throw new ArgumentNullException("CellMovement.MoveTo failed to find cell.");

        this.MoveTo(chosenCell);
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

        UpdateDebugCells();
    }

    /// <summary>
    /// Initializes the findPathfinding variable.
    /// </summary>
    protected virtual async Task Start()
    {
        this.PathFinder = new GridCellPathFinder(MazeController.Instance.Grid, MazeController.Instance);
    }

    /// <summary>
    /// Invokes the <see cref="UpdateMovement(Cell, Cell)"/> method. If returned true will choose a new cell.
    /// </summary>
    protected virtual async Task Update()
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
    /// Add debug cells for each path we would be taking.
    /// </summary>
    private void UpdateDebugCells()
    {
        if (!this.ShowDebugCells && DebugCells.Count == 0)
            return;

        foreach (var cell in DebugCells)
        {
            cell.Destroy();
        }
        DebugCells.Clear();

        if (!this.ShowDebugCells)
            return;

        foreach (var cell in MoveInstructions)
        {
            GameObject go = Instantiate(MazeResourceManager.Instance.DebugCube, cell.Position, Quaternion.identity, this.transform.parent);
            this.DebugCells.Add(go);
        }
    }

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