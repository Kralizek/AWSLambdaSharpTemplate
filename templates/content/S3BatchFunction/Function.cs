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

public sealed class Function : S3BatchFunction<S3BatchItemHandler>
{
#if (otel)
    private static readonly TracerProvider TracerProvider = ConfigureTracing();
    private static readonly MeterProvider MeterProvider = ConfigureMetrics();

    public override async Task<Amazon.Lambda.S3Events.S3BatchResponse> FunctionHandlerAsync(
        Amazon.Lambda.S3Events.S3BatchEvent input,
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
