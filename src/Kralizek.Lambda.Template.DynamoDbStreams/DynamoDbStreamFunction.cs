using Microsoft.Extensions.DependencyInjection;

namespace Kralizek.Lambda;

/// <summary>
/// A function base class for Lambda functions triggered by Amazon DynamoDB Streams.
/// </summary>
/// <typeparam name="THandler">The concrete handler type that processes each stream record.</typeparam>
public abstract class DynamoDbStreamFunction<THandler>
    : DynamoDbStreamFunctionBase<RawDynamoDbStreamRecordHandler<THandler>>
    where THandler : class, IDynamoDbStreamRecordHandler
{
    protected override void ConfigureFrameworkServices(IServiceCollection services)
    {
        base.ConfigureFrameworkServices(services);
        DynamoDbStreamServiceRegistration.AddHandler<THandler>(services);
    }
}

/// <summary>
/// A DynamoDB Streams function that processes records with bounded parallelism.
/// </summary>
/// <typeparam name="THandler">The concrete handler type that processes each stream record.</typeparam>
public abstract class ParallelDynamoDbStreamFunction<THandler>
    : ParallelDynamoDbStreamFunctionBase<RawDynamoDbStreamRecordHandler<THandler>>
    where THandler : class, IDynamoDbStreamRecordHandler
{
    protected override void ConfigureFrameworkServices(IServiceCollection services)
    {
        base.ConfigureFrameworkServices(services);
        DynamoDbStreamServiceRegistration.AddHandler<THandler>(services);
    }
}