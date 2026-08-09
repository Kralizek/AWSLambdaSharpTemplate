namespace Kralizek.Lambda;

/// <summary>
/// Base type for results produced while processing records.
/// </summary>
/// <remarks>
/// The <see cref="Value"/> shape intentionally matches the C# union runtime contract so record results can
/// implement <c>System.Runtime.CompilerServices.IUnion</c> when the library moves to a framework that exposes it.
/// </remarks>
public abstract class LambdaRecordResult
{
    /// <summary>
    /// Gets the value represented by this record result.
    /// </summary>
    public abstract object? Value { get; }
}
