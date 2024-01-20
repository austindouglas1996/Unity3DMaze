public interface IGenerate
{
    /// <summary>
    /// Returns true if <see cref="Generate"/> has been called.
    /// </summary>
    public bool GenerateCalled { get; }

    /// <summary>
    /// Returns true if <see cref="Generate"/> has finished it's function.
    /// </summary>
    public bool GenerateFinished { get; }

}