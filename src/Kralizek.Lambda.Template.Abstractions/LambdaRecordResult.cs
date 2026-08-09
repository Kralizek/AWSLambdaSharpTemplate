using System.Diagnostics.CodeAnalysis;

namespace Kralizek.Lambda;

/// <summary>
/// Base type for source-specific results produced by record handlers.
/// </summary>
/// <remarks>
/// The <see cref="Value"/> property intentionally matches the shape of the future
/// <c>System.Runtime.CompilerServices.IUnion.Value</c> contract so result types can
/// evolve to C# union types without changing the public abstraction.
/// </remarks>
[SuppressMessage(
    "Design",
    "S1694:An abstract class should have both abstract and concrete methods",
    Justification = "This base class intentionally reserves single inheritance for source-specific result types so it can evolve toward the C# IUnion contract.")]
public abstract class LambdaRecordResult
{
    /// <summary>
    /// Gets the case value represented by this result.
    /// </summary>
    public abstract object? Value { get; }
}