using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kralizek.Lambda;

/// <summary>
/// A function base class for handlers that process a single input and return a meaningful response.
/// </summary>
/// <typeparam name="TInput">The type of the incoming request.</typeparam>
/// <typeparam name="TOutput">The type of the response.</typeparam>
/// <typeparam name="TContext">The context type passed to the handler.</typeparam>
/// <typeparam name="THandler">The concrete handler type that processes the request.</typeparam>
#pragma warning disable S2436 // The generic roles are intentional and make the request contract explicit.
public abstract class RequestFunction<TInput, TOutput, TContext, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler> : LambdaFunction
#pragma warning restore S2436
    where TContext : RequestContext
    where THandler : class, IRequestHandler<TInput, TOutput, TContext>
{
    protected sealed override void RegisterFrameworkServices(IServiceCollection services)
    {
        base.RegisterFrameworkServices(services);
        services.TryAddScoped<THandler>();
        ConfigureFrameworkServices(services);
    }

    /// <summary>
    /// Registers replaceable services for this request-function specialization.
    /// </summary>
    protected virtual void ConfigureFrameworkServices(IServiceCollection services)
    {
    }

    protected abstract TContext CreateContext(TInput input, ILambdaContext context);

    protected virtual void EnrichInvocationActivity(Activity activity, TInput input, ILambdaContext context)
    {
    }

    public virtual async Task<TOutput> FunctionHandlerAsync(TInput input, ILambdaContext context)
    {
        LambdaTelemetry.EnrichInvocation("request");
        if (Activity.Current is { } activity)
        {
            EnrichInvocationActivity(activity, input, context);
        }

        var requestContext = CreateContext(input, context);

        try
        {
            await using var invocationScope = ServiceProvider.CreateAsyncScope();

            return await ExecuteHandlerAsync<THandler, TOutput>(
                invocationScope.ServiceProvider,
                CancellationToken.None,
                (handler, cancellationToken) => handler.HandleAsync(input, requestContext, cancellationToken)).ConfigureAwait(false);
        }
        finally
        {
            FunctionContextFactory.DisposeDeadlineCancellationToken(requestContext);
        }
    }
}

#pragma warning disable S2436 // The three public roles intentionally hide the standard context type.
public abstract class RequestFunction<TInput, TOutput, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler> : RequestFunction<TInput, TOutput, RequestContext, THandler>
#pragma warning restore S2436
    where THandler : class, IRequestHandler<TInput, TOutput, RequestContext>
{
    protected override RequestContext CreateContext(TInput input, ILambdaContext context) =>
        FunctionContextFactory.CreateRequestContext(context);
}
