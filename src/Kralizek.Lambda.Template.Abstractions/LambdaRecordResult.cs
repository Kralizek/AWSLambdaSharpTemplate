namespace Kralizek.Lambda;

/// <summary>
/// Base type for source-specific results produced by record handlers.
/// </summary>
/// <remarks>
/// The <see cref="Value"/> property intentionally matches the shape of the future
/// <c>System.Runtime.CompilerServices.IUnion.Value</c> contract so result types can
/// evolve to C# union types without changing the public abstraction.
/// </remarks>
public abstract class LambdaRecordResult
{
    /// <summary>
    /// Gets the value represented by this result.
    /// </summary>
    public abstract object? Value { get; }
}