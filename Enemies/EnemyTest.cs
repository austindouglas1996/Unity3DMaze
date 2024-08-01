using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;

[RequireComponent(typeof(CharacterMovementController))]
public class EnemyTest : MonoBehaviour
{
    public GameObject Player;
    public MazeController Maze;
    public GameObject Marker1;

    private CharacterMovementController Controller;

    private void Start()
    {
        Controller = GetComponent<CharacterMovementController>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            DeterminePath();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            this.transform.position = Player.transform.position;
        }
    }

    private void DeterminePath()
    {
        Cell playerPos = GetPlayerCell();

        // This happened, maybe fixed for good?
        if (playerPos.Type != CellType.None && playerPos.Room == null)
        {
            Debug.Log("Pathfinding: Player cell room is null, but cell not set to null.");
            return;
        }

        if (playerPos.Room == null)
        {
            Debug.Log("Can't find player tile");
            return;
        }

        this.Controller.MoveTo(playerPos);
    }

    private Cell GetPlayerCell()
    {
        Vector3Int eg = this.Player.GetComponent<PathTrigger>().Position;
        Cell egg = this.Maze.Grid[eg];

        return egg;
    }
}