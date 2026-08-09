using Microsoft.Extensions.DependencyInjection;

namespace Kralizek.Lambda;

/// <summary>
/// A function base class for Lambda functions triggered by Kinesis Streams that process raw records.
/// </summary>
public abstract class KinesisStreamFunction<THandler>
    : KinesisStreamFunctionBase<RawKinesisStreamRecordHandler<THandler>>
    where THandler : class, IKinesisStreamRecordHandler
{
    protected override void ConfigureFrameworkServices(IServiceCollection services)
    {
        base.ConfigureFrameworkServices(services);
        KinesisStreamServiceRegistration.AddRawHandler<THandler>(services);
    }
}

/// <summary>
/// A function base class for Lambda functions triggered by Kinesis Streams that decode record data into application contracts.
/// </summary>
public abstract class KinesisStreamFunction<TPayload, THandler>
    : KinesisStreamFunctionBase<KinesisStreamRecordHandler<TPayload, THandler>>
    where THandler : class, IKinesisStreamRecordHandler<TPayload>
{
    protected override void ConfigureFrameworkServices(IServiceCollection services)
    {
        base.ConfigureFrameworkServices(services);
        KinesisStreamServiceRegistration.AddDecodedHandler<TPayload, THandler>(services);
    }
}