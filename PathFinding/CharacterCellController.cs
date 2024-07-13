using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using VHierarchy.Libs;

[RequireComponent(typeof(CharacterControllerWithGravity))]
public class CharacterCellController : MonoBehaviour
{
    [Tooltip("Helps with determining paths for characters in the grid.")]
    [SerializeField] public GridCellPathFinder PathFinder;

    [Tooltip("Debug cell")]
    [SerializeField] private GameObject DebugCube;

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
    private List<GameObject> DebugCells = new List<GameObject>();

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

        if (this.RemainingMoveInstructions != null)
        {
            if (this.DebugCells.Count > 0)
            {
                foreach (GameObject go in this.DebugCells)
                {
                    go.Destroy();
                }

                this.DebugCells.Clear();
            }

            foreach (Cell dCell in this.RemainingMoveInstructions)
            {
                GameObject go = Instantiate(DebugCube, dCell.Position, Quaternion.identity, this.transform.parent);
                this.DebugCells.Add(go);
            }
        }

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
            var distance = Vector3.Distance(this.transform.position, (MovingTo.Position - new Vector3Int(0,2,0)));
            if (distance < 0.8f)
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
        return this.PathFinder.Grid.Find(this.transform.position.RoundToInt(), new Vector3(2f, 4f, 2f));
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

        this.Controller.MoveTo(this.MovingTo.Position - new Vector3Int(0,2,0));

        this.IsMoving = true;
    }
}