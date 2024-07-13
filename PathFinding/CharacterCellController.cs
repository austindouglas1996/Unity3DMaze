using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterControllerWithGravity))]
public class CharacterCellController : MonoBehaviour
{
    [Tooltip("Helps with determining paths for characters in the grid.")]
    [SerializeField] private GridCellPathFinder PathFinder;

    /// <summary>
    /// Helps with controlling where this character will go.
    /// </summary>
    private CharacterControllerWithGravity Controller;

    /// <summary>
    /// Tells whether we're currently in processing of moving to a cell.
    /// </summary>
    private bool IsMoving = false;

    /// <summary>
    /// A list of cell moving instructions.
    /// </summary>
    private List<Cell> RemainingMoveInstructions = new List<Cell>();

    /// <summary>
    /// The current cell we're working on moving to.
    /// </summary>
    private Cell MovingTo;

    /// <summary>
    /// Navigate from our current position to a new cell. Use pathfinding to find our way.
    /// </summary>
    /// <param name="cell"></param>
    public void MoveToCell(Cell cell)
    {
        this.RemainingMoveInstructions = PathFinder.FindPath(GetCurrentCell(), cell);
        this.GetNextCell();
    }

    /// <summary>
    /// Retrieve important components.
    /// </summary>
    private void Start()
    {
        this.Controller = GetComponent<CharacterControllerWithGravity>();
    }

    /// <summary>
    /// Check to see if we have reached the destination yet.
    /// </summary>
    private void Update()
    {
        if (IsMoving)
        {
            // Have we reached our destination?
            if (Vector3.Distance(this.transform.position, MovingTo.Position) > 0.001f)
            {
                GetNextCell();
            }
        }
    }

    /// <summary>
    /// Get the current cell this character is standing on.
    /// </summary>
    /// <returns></returns>
    private Cell GetCurrentCell()
    {
        return this.PathFinder.Grid.Find(this.transform.position.RoundToInt(), new Vector3(4f, 4f, 4f));
    }

    /// <summary>
    /// Get the next cell this character will walk towards on their goal.
    /// </summary>
    private void GetNextCell()
    {
        if (this.RemainingMoveInstructions.Count == 0)
        {
            this.IsMoving = false;
            return;
        }

        this.MovingTo = this.RemainingMoveInstructions[0];
        this.RemainingMoveInstructions.RemoveAt(0);

        this.Controller.MoveTo(this.MovingTo.Position);
    }
}