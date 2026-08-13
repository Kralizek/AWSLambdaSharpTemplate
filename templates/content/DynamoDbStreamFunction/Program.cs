using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;

namespace LambdaFunctionProject;

internal static class LambdaAot
{
    private static async Task Main()
    {
        var function = new Function();
        var serializer = new SourceGeneratorLambdaJsonSerializer<LambdaJsonSerializerContext>();

        var bootstrap = LambdaBootstrapBuilder.Create<Amazon.Lambda.DynamoDBEvents.DynamoDBEvent, Amazon.Lambda.DynamoDBEvents.StreamsEventResponse>(
            function.FunctionHandlerAsync,
            serializer);

        await bootstrap.Build().RunAsync().ConfigureAwait(false);
    }
}

[JsonSerializable(typeof(Amazon.Lambda.DynamoDBEvents.DynamoDBEvent))]
[JsonSerializable(typeof(Amazon.Lambda.DynamoDBEvents.StreamsEventResponse))]
internal partial class LambdaJsonSerializerContext : JsonSerializerContext;
