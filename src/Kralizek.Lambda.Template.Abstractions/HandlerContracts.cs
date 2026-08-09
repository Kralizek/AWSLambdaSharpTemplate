using System.Threading;
using System.Threading.Tasks;

namespace Kralizek.Lambda;

/// <summary>
/// The contract for strongly typed event handlers.
/// </summary>
public interface IEventHandler<in TInput, in TContext>
    where TContext : EventContext
{
    ValueTask HandleAsync(TInput input, TContext context, CancellationToken cancellationToken);
}

/// <summary>
/// The contract for event handlers that use the standard <see cref="EventContext"/>.
/// </summary>
public interface IEventHandler<in TInput> : IEventHandler<TInput, EventContext>
{
}

/// <summary>
/// The contract for strongly typed request handlers.
/// </summary>
#pragma warning disable S2436 // The generic roles are intentional and make the request handler contract explicit.
public interface IRequestHandler<in TInput, TOutput, in TContext>
#pragma warning restore S2436
    where TContext : RequestContext
{
    ValueTask<TOutput> HandleAsync(TInput input, TContext context, CancellationToken cancellationToken);
}

/// <summary>
/// The contract for request handlers that use the standard <see cref="RequestContext"/>.
/// </summary>
public interface IRequestHandler<in TInput, TOutput> : IRequestHandler<TInput, TOutput, RequestContext>
{
}

/// <summary>
/// The contract for handlers invoked by record-oriented functions.
/// </summary>
/// <typeparam name="TRecord">The individual record type to handle.</typeparam>
/// <typeparam name="TRecordResult">The result produced from processing the record.</typeparam>
/// <typeparam name="TContext">The context available while processing a record.</typeparam>
#pragma warning disable S2436 // Record, result, and strongly typed context are distinct handler roles by design.
public interface IRecordHandler<in TRecord, TRecordResult, in TContext>
#pragma warning restore S2436
    where TRecordResult : LambdaRecordResult
    where TContext : RecordContext
{
    ValueTask<TRecordResult> HandleAsync(TRecord record, TContext context, CancellationToken cancellationToken);
}