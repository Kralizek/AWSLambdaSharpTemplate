using System.Threading.Tasks;

using Amazon.Lambda.Core;

using Kralizek.Lambda;

#if (aot && !raw)
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
#endif

#if (otel)
using OpenTelemetry;
using OpenTelemetry.Instrumentation.AWSLambda;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
#endif

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace LambdaFunctionProject;

#if (raw)
public sealed class Function : SnsFunction<RawSnsRecordHandler>
#else
public sealed class Function : SnsFunction<OrderCreated, OrderCreatedHandler>
#endif
{
#if (aot && !raw)
    protected override void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        base.ConfigureServices(services, configuration);

        services.AddSingleton<IStringPayloadDecoder<OrderCreated>>(
            new JsonStringPayloadDecoder<OrderCreated>(LambdaJsonSerializerContext.Default.OrderCreated));
    }
#endif

#if (otel)
    private static readonly TracerProvider TracerProvider = ConfigureTracing();
    private static readonly MeterProvider MeterProvider = ConfigureMetrics();

    public override async Task<object?> FunctionHandlerAsync(
        Amazon.Lambda.SNSEvents.SNSEvent input,
        ILambdaContext context)
    {
        try
        {
            return await AWSLambdaWrapper.TraceAsync(
                TracerProvider,
                base.FunctionHandlerAsync,
                input,
                context).ConfigureAwait(false);
        }
        finally
        {
            MeterProvider.ForceFlush();
        }
    }

    private static TracerProvider ConfigureTracing() =>
        Sdk.CreateTracerProviderBuilder()
            .AddSource(LambdaTelemetry.ActivitySourceName)
            .AddAWSLambdaConfigurations(options =>
            {
                // Uncomment when X-Ray is not used and its Lambda context prevents OpenTelemetry spans from being recorded.
                // options.DisableAwsXRayContextExtraction = true;
            })
            .AddOtlpExporter()
            .Build();

    private static MeterProvider ConfigureMetrics() =>
        Sdk.CreateMeterProviderBuilder()
            .AddMeter(LambdaTelemetry.MeterName)
            .AddOtlpExporter()
            .Build();
#endif
}
