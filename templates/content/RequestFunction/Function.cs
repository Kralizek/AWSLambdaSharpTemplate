using System.Threading.Tasks;

using Amazon.Lambda.Core;

using Kralizek.Lambda;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#if (otel)
using OpenTelemetry;
using OpenTelemetry.Instrumentation.AWSLambda;
#if (!minimal)
using OpenTelemetry.Metrics;
#endif
using OpenTelemetry.Trace;
#endif

#if (!aot)
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
#endif

namespace LambdaFunctionProject;

#if (minimal)
public class Function : MinimalRequestFunction<string, string, ToUpperStringRequestHandler>
#else
public class Function : RequestFunction<string, string, ToUpperStringRequestHandler>
#endif
{
#if (otel)
    private static readonly TracerProvider TracerProvider = ConfigureTracing();
#if (!minimal)
    private static readonly MeterProvider MeterProvider = ConfigureMetrics();
#endif
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
#if (minimal)
        return await AWSLambdaWrapper.TraceAsync(
            TracerProvider,
            base.FunctionHandlerAsync,
            input,
            context).ConfigureAwait(false);
#else
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
#endif
    }

    private static TracerProvider ConfigureTracing()
    {
        var builder = Sdk.CreateTracerProviderBuilder();
#if (!minimal)
        builder.AddSource(LambdaTelemetry.ActivitySourceName);
#endif

        return builder
            .AddAWSLambdaConfigurations(options =>
            {
                // Uncomment when X-Ray is not used and its Lambda context prevents OpenTelemetry spans from being recorded.
                // options.DisableAwsXRayContextExtraction = true;
            })
            .AddOtlpExporter()
            .Build();
    }

#if (!minimal)
    private static MeterProvider ConfigureMetrics() =>
        Sdk.CreateMeterProviderBuilder()
            .AddMeter(LambdaTelemetry.MeterName)
            .AddOtlpExporter()
            .Build();
#endif
#endif
}
