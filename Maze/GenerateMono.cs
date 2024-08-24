using System.Threading.Tasks;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Represents an object to help with controlling entities with generation. Issues occuring with RNG objects that have not finished their generation
/// cause some checks to go unnoticed. Like room fixtures that operate as doors may not be seen until generation is done.
/// </summary>
public abstract class GenerateMono : MonoBehaviour
{
    /// <summary>
    /// Helpful for sometimes we can <see cref="Doors"/> before <see cref="GenerateDoors"/> called.
    /// </summary>
    [HideInInspector] public bool GenerateCalled = false;

    /// <summary>
    /// Helpful to know if the room has been fully generated.
    /// </summary>
    [HideInInspector] public bool GenerateFinished = false;

    /// <summary>
    /// Generate the entity.
    /// </summary>
    /// <returns></returns>
    public async virtual Task Generate(object[] args)
    {
        if (GenerateCalled || GenerateFinished)
            return;
        else
            GenerateCalled = true;

        await OnGenerate(args);

        this.GenerateFinished = true;
    }

    /// <summary>
    /// Regenerate the entity. Used by entities like <see cref="RoomFixtureMono"/>.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public async virtual Task ReGenerate(object[] args)
    {
        this.GenerateCalled = false;
        this.GenerateFinished = false;
        await Generate(args);
    }

    /// <summary>
    /// Executed on <see cref="Generate"/> helps with controlling generation.
    /// </summary>
    /// <returns></returns>
    protected abstract Task OnGenerate(object[] args);
}