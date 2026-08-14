using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Kralizek.Lambda;
#if (otel)
using OpenTelemetry;
using OpenTelemetry.Instrumentation.AWSLambda;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
#endif
#if (!aot)
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
#endif
namespace LambdaFunctionProject;
#if (preTokenV2)
public sealed class Function : CognitoPreTokenGenerationV2Function<PreTokenGenerationHandler>
{
#if (otel)
    private static readonly TracerProvider TracerProvider = ConfigureTracing();
    private static readonly MeterProvider MeterProvider = ConfigureMetrics();
    public override async Task<Amazon.Lambda.CognitoEvents.CognitoPreTokenGenerationV2Event> FunctionHandlerAsync(Amazon.Lambda.CognitoEvents.CognitoPreTokenGenerationV2Event input, ILambdaContext context)
    {
        try
        {
            return await AWSLambdaWrapper.TraceAsync(TracerProvider, base.FunctionHandlerAsync, input, context).ConfigureAwait(false);
        }
        finally
        {
            MeterProvider.ForceFlush();
        }
    }
    private static TracerProvider ConfigureTracing() => Sdk.CreateTracerProviderBuilder()
        .AddSource(LambdaTelemetry.ActivitySourceName)
        .AddAWSLambdaConfigurations(options =>
        {
            // Uncomment when X-Ray is not used and its Lambda context prevents OpenTelemetry spans from being recorded.
            // options.DisableAwsXRayContextExtraction = true;
        })
        .AddOtlpExporter()
        .Build();
    private static MeterProvider ConfigureMetrics() => Sdk.CreateMeterProviderBuilder()
        .AddMeter(LambdaTelemetry.MeterName)
        .AddOtlpExporter()
        .Build();
#endif
}
#else
public sealed class Function : CognitoPreTokenGenerationFunction<PreTokenGenerationHandler>
{
#if (otel)
    private static readonly TracerProvider TracerProvider = ConfigureTracing();
    private static readonly MeterProvider MeterProvider = ConfigureMetrics();
    public override async Task<Amazon.Lambda.CognitoEvents.CognitoPreTokenGenerationEvent> FunctionHandlerAsync(Amazon.Lambda.CognitoEvents.CognitoPreTokenGenerationEvent input, ILambdaContext context)
    {
        try
        {
            return await AWSLambdaWrapper.TraceAsync(TracerProvider, base.FunctionHandlerAsync, input, context).ConfigureAwait(false);
        }
        finally
        {
            MeterProvider.ForceFlush();
        }
    }
    private static TracerProvider ConfigureTracing() => Sdk.CreateTracerProviderBuilder()
        .AddSource(LambdaTelemetry.ActivitySourceName)
        .AddAWSLambdaConfigurations(options =>
        {
            // Uncomment when X-Ray is not used and its Lambda context prevents OpenTelemetry spans from being recorded.
            // options.DisableAwsXRayContextExtraction = true;
        })
        .AddOtlpExporter()
        .Build();
    private static MeterProvider ConfigureMetrics() => Sdk.CreateMeterProviderBuilder()
        .AddMeter(LambdaTelemetry.MeterName)
        .AddOtlpExporter()
        .Build();
#endif
}
#endif
