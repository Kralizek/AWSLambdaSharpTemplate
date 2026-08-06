using System;

using Amazon.Lambda.Core;

namespace Kralizek.Lambda;

/// <summary>
/// Provides metadata about the current Lambda invocation.
/// </summary>
public abstract class FunctionContext
{
    protected internal FunctionContext(ILambdaContext lambdaContext)
    {
        LambdaContext = lambdaContext ?? throw new ArgumentNullException(nameof(lambdaContext));
    }

    /// <summary>
    /// The underlying AWS Lambda invocation context.
    /// </summary>
    public ILambdaContext LambdaContext { get; }

    public string AwsRequestId => LambdaContext.AwsRequestId;

    public string FunctionName => LambdaContext.FunctionName;

    public string FunctionVersion => LambdaContext.FunctionVersion;

    public string InvokedFunctionArn => LambdaContext.InvokedFunctionArn;

    public int MemoryLimitInMB => LambdaContext.MemoryLimitInMB;

    public TimeSpan RemainingTime => LambdaContext.RemainingTime;

    public string LogGroupName => LambdaContext.LogGroupName;

    public string LogStreamName => LambdaContext.LogStreamName;
}

/// <summary>
/// Invocation context for completion-only event functions.
/// </summary>
public class EventContext : FunctionContext
{
    protected internal EventContext(ILambdaContext lambdaContext)
        : base(lambdaContext) { }
}

/// <summary>
/// Invocation context for request/response functions.
/// </summary>
public class RequestContext : FunctionContext
{
    protected internal RequestContext(ILambdaContext lambdaContext)
        : base(lambdaContext) { }
}

/// <summary>
/// Invocation context shared by record-oriented functions.
/// </summary>
public class RecordContext : FunctionContext
{
    protected internal RecordContext(ILambdaContext lambdaContext)
        : base(lambdaContext) { }
}
