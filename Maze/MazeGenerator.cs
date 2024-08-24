using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(MazeController))]
public abstract class MazeGenerator<TOutput> : GenerateMono
{
    /// <summary>
    /// Helps with controlling whether we're allowed to generate without annoyingly needing to remove.
    /// </summary>
    [SerializeField] public bool Enabled = true;

    /// <summary>
    /// A list of created objects from this generator.
    /// </summary>
    [HideInInspector] public List<TOutput> GeneratedEntities = new List<TOutput>();

    /// <summary>
    /// Gets the <see cref="Maze"/> instance to use for accessing important properties.
    /// </summary>
    public MazeController Maze
    {
        get
        {
            if (_Maze == null)
                _Maze = this.GetComponent<MazeController>();

            return _Maze;
        }
    }
    private MazeController _Maze;

    /// <summary>
    /// Start the generation process for this generator.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public override Task Generate(object[] args)
    {
        if (!Enabled)
        {
            return Task.CompletedTask;
        }

        return base.Generate(args);
    }

    /// <summary>
    /// Reset the generator, removing all created items.
    /// </summary>
    /// <returns></returns>
    public async Task ResetGenerator()
    {
        await OnResetGenerator();
        GenerateCalled = false;
        GenerateFinished = false;
    }

    /// <summary>
    /// Executed on <see cref="Reset"/> call.
    /// </summary>
    /// <returns></returns>
    protected abstract Task OnResetGenerator();
}