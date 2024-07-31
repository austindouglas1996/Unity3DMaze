using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;

public class GhostEnemy : MonoBehaviour
{
    public GameObject Player;
    public MazeController Maze;

    private GridCellPathFinder PFinder;
    [SerializeField] private CharacterCellController Controller;
    [SerializeField] private bool DeterminePath = false;

    private RoomMono FavoriteRoom;

    private async Task Start()
    {
        PFinder = new GridCellPathFinder(Maze.Grid, Maze);
        this.Controller.PathFinder = PFinder;
        this.Controller.ReachedDestination += Controller_ReachedDestination;

        await Task.Delay(100);

        this.DetermineNewPath();
    }

    private void Update()
    {
        if (DeterminePath)
        {
            DeterminePath = false;
            DetermineNewPath();
        }
    }

    private void DetermineNewPath()
    {
        while (true)
        {
            var newSpot = Maze.Grid.Cells.Random();
            bool result = this.Controller.MoveToCell(newSpot);
            if (result)
                break;
        }
    }

    private void GetFavoriteRoom()
    {
        FavoriteRoom = Maze.Rooms.Generated.Random();
    }

    private void Controller_ReachedDestination(object sender, System.EventArgs e)
    {
        this.DetermineNewPath();
    }
}