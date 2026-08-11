using System.Threading.Tasks;

using Amazon.Lambda.Core;

using Kralizek.Lambda;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#if (otel)
using OpenTelemetry;
using OpenTelemetry.Instrumentation.AWSLambda;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
#endif

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace LambdaFunctionProject;

public class Function : RequestFunction<string, string, ToUpperStringRequestHandler>
{
#if (otel)
    private static readonly TracerProvider TracerProvider = ConfigureTracing();
    private static readonly MeterProvider MeterProvider = ConfigureMetrics();
#endif

    protected override void ConfigureConfiguration(IConfigurationBuilder configuration)
    {
        // Add application configuration sources here.
        base.ConfigureConfiguration(configuration);
    }

    protected override void ConfigureLogging(ILoggingBuilder logging)
    {
        // Add or customize application logging here. Lambda logging is configured by the framework.
        base.ConfigureLogging(logging);
    }

    protected override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register application services here. The primary handler is registered by the framework.
        base.ConfigureServices(services, configuration);
    }

#if (otel)
    public override async Task<string> FunctionHandlerAsync(string input, ILambdaContext context)
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
