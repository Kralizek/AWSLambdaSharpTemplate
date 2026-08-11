using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.S3;
using Kralizek.Lambda;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
#if (otel)
using OpenTelemetry;
using OpenTelemetry.Instrumentation.AWSLambda;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
#endif
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace LambdaFunctionProject;
public sealed class Function : S3Function<S3ObjectEventHandler>
{
#if (otel)
    private static readonly TracerProvider TracerProvider = ConfigureTracing();
    private static readonly MeterProvider MeterProvider = ConfigureMetrics();
#endif
    protected override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client());
    }
#if (otel)
    public override async Task<object?> FunctionHandlerAsync(Amazon.Lambda.S3Events.S3Event input, ILambdaContext context)
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
