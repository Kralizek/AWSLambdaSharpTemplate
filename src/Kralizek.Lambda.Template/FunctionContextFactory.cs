using System;
using System.Collections.Generic;
using System.Threading;

using Amazon.Lambda.Core;

namespace Kralizek.Lambda;

/// <summary>
/// Creates source-neutral function contexts from the AWS Lambda runtime context.
/// </summary>
public static class FunctionContextFactory
{
    private const string LambdaContextPropertyName = "Kralizek.Lambda.Template.LambdaContext";

    public static EventContext CreateEventContext(ILambdaContext lambdaContext)
    {
        ArgumentNullException.ThrowIfNull(lambdaContext);
        return new DefaultEventContext(CreateMetadata(lambdaContext), LambdaContextPropertyName, lambdaContext);
    }

    public static RequestContext CreateRequestContext(ILambdaContext lambdaContext)
    {
        ArgumentNullException.ThrowIfNull(lambdaContext);
        return new DefaultRequestContext(CreateMetadata(lambdaContext), LambdaContextPropertyName, lambdaContext);
    }

    public static RecordContext CreateRecordContext(ILambdaContext lambdaContext)
    {
        ArgumentNullException.ThrowIfNull(lambdaContext);
        return new DefaultRecordContext(CreateMetadata(lambdaContext), LambdaContextPropertyName, lambdaContext);
    }

    /// <summary>
    /// Maps the AWS Lambda runtime context to source-neutral invocation metadata.
    /// </summary>
    public static FunctionContextMetadata CreateMetadata(ILambdaContext lambdaContext)
    {
        ArgumentNullException.ThrowIfNull(lambdaContext);

        return new FunctionContextMetadata(
            lambdaContext.AwsRequestId,
            lambdaContext.FunctionName,
            lambdaContext.FunctionVersion,
            lambdaContext.InvokedFunctionArn,
            lambdaContext.MemoryLimitInMB,
            lambdaContext.RemainingTime,
            lambdaContext.LogGroupName,
            lambdaContext.LogStreamName);
    }

    /// <summary>
    /// Creates a mutable runtime-specific property bag that source integrations can extend before constructing a context.
    /// </summary>
    public static Dictionary<string, object?> CreateProperties(ILambdaContext lambdaContext)
    {
        ArgumentNullException.ThrowIfNull(lambdaContext);

        return new Dictionary<string, object?>
        {
            [LambdaContextPropertyName] = lambdaContext
        };
    }

    /// <summary>
    /// Gets the original AWS Lambda runtime context preserved in the context property bag.
    /// </summary>
    public static ILambdaContext GetLambdaContext(this FunctionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Properties.TryGetValue(LambdaContextPropertyName, out var value) && value is ILambdaContext lambdaContext)
        {
            return lambdaContext;
        }

        throw new InvalidOperationException("The function context does not contain an AWS Lambda runtime context.");
    }

    /// <summary>
    /// Creates a cancellation token source that is cancelled when the current Lambda invocation reaches its remaining-time deadline.
    /// </summary>
    /// <remarks>
    /// The caller owns the returned cancellation token source and must dispose it.
    /// </remarks>
    public static CancellationTokenSource CreateDeadlineCancellationTokenSource(this FunctionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return LambdaInvocationLifetime.CreateCancellationTokenSource(context.GetLambdaContext());
    }

    private sealed class DefaultEventContext(
        FunctionContextMetadata metadata,
        string propertyName,
        object? propertyValue)
        : EventContext(metadata, propertyName, propertyValue);

    private sealed class DefaultRequestContext(
        FunctionContextMetadata metadata,
        string propertyName,
        object? propertyValue)
        : RequestContext(metadata, propertyName, propertyValue);

    private sealed class DefaultRecordContext(
        FunctionContextMetadata metadata,
        string propertyName,
        object? propertyValue)
        : RecordContext(metadata, propertyName, propertyValue);
}
