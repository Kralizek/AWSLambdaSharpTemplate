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
        var bootstrap = LambdaBootstrapBuilder.Create<Amazon.Lambda.CognitoEvents.CognitoPreSignupEvent, Amazon.Lambda.CognitoEvents.CognitoPreSignupEvent>(function.FunctionHandlerAsync, serializer);
        await bootstrap.Build().RunAsync().ConfigureAwait(false);
    }
}

[JsonSerializable(typeof(Amazon.Lambda.CognitoEvents.CognitoPreSignupEvent))]
internal partial class LambdaJsonSerializerContext : JsonSerializerContext;
