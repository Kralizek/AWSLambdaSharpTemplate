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

        var bootstrap = LambdaBootstrapBuilder.Create<Amazon.Lambda.S3Events.S3Event>(
            async (input, context) =>
            {
                await function.FunctionHandlerAsync(input, context).ConfigureAwait(false);
            },
            serializer);

        await bootstrap.Build().RunAsync().ConfigureAwait(false);
    }
}

[JsonSerializable(typeof(Amazon.Lambda.S3Events.S3Event))]
internal partial class LambdaJsonSerializerContext : JsonSerializerContext;
