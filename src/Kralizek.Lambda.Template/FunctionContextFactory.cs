using System;
using System.Collections.Generic;

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
        return new DefaultEventContext(CreateMetadata(lambdaContext), CreateProperties(lambdaContext));
    }

    public static RequestContext CreateRequestContext(ILambdaContext lambdaContext)
    {
        ArgumentNullException.ThrowIfNull(lambdaContext);
        return new DefaultRequestContext(CreateMetadata(lambdaContext), CreateProperties(lambdaContext));
    }

    public static RecordContext CreateRecordContext(ILambdaContext lambdaContext)
    {
        ArgumentNullException.ThrowIfNull(lambdaContext);
        return new DefaultRecordContext(CreateMetadata(lambdaContext), CreateProperties(lambdaContext));
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

    private sealed class DefaultEventContext(
        FunctionContextMetadata metadata,
        IReadOnlyDictionary<string, object?> properties)
        : EventContext(metadata, properties);

    private sealed class DefaultRequestContext(
        FunctionContextMetadata metadata,
        IReadOnlyDictionary<string, object?> properties)
        : RequestContext(metadata, properties);

    private sealed class DefaultRecordContext(
        FunctionContextMetadata metadata,
        IReadOnlyDictionary<string, object?> properties)
        : RecordContext(metadata, properties);
}