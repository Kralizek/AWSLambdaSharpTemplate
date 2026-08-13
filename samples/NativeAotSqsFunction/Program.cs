using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;

namespace NativeAotSqsFunction;

internal static class Program
{
    private static async Task Main()
    {
        var function = new Function();
        var serializer = new SourceGeneratorLambdaJsonSerializer<LambdaJsonSerializerContext>();

        var bootstrap = LambdaBootstrapBuilder.Create<Amazon.Lambda.SQSEvents.SQSEvent, Amazon.Lambda.SQSEvents.SQSBatchResponse>(
            function.FunctionHandlerAsync,
            serializer);

        await bootstrap.Build().RunAsync().ConfigureAwait(false);
    }
}

[JsonSerializable(typeof(Amazon.Lambda.SQSEvents.SQSEvent))]
[JsonSerializable(typeof(Amazon.Lambda.SQSEvents.SQSBatchResponse))]
internal partial class LambdaJsonSerializerContext : JsonSerializerContext;
