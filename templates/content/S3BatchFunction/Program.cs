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

        var bootstrap = LambdaBootstrapBuilder.Create<Kralizek.Lambda.S3BatchEvent, Kralizek.Lambda.S3BatchResponse>(
            function.FunctionHandlerAsync,
            serializer);

        await bootstrap.Build().RunAsync().ConfigureAwait(false);
    }
}

[JsonSerializable(typeof(Kralizek.Lambda.S3BatchEvent))]
[JsonSerializable(typeof(Kralizek.Lambda.S3BatchResponse))]
internal partial class LambdaJsonSerializerContext : JsonSerializerContext;
