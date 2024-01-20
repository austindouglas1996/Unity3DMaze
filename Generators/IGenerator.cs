using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Defines the constrains to work with <see cref="MazeController"/> to add a generator. I have seen issues
/// with some generators like room & hallway where some other function is trying to call their output
/// before they have finished. Seems Unity runs functions async by default.
/// </summary>
/// <typeparam name="TOutput"></typeparam>
public interface IGenerator<TOutput> : IGenerate
    where TOutput : class
{
    /// <summary>
    /// Returns the output of <see cref="Generate"/>.
    /// </summary>
    public List<TOutput> Generated { get; }

    /// <summary>
    /// Generate the contents of the generator whatever that may be.
    /// </summary>
    /// <returns></returns>
    public Task Generate();
}