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
//#if (preTokenV2)
        var bootstrap = LambdaBootstrapBuilder.Create<Amazon.Lambda.CognitoEvents.CognitoPreTokenGenerationV2Event, Amazon.Lambda.CognitoEvents.CognitoPreTokenGenerationV2Event>(function.FunctionHandlerAsync, serializer);
//#else
        var bootstrap = LambdaBootstrapBuilder.Create<Amazon.Lambda.CognitoEvents.CognitoPreTokenGenerationEvent, Amazon.Lambda.CognitoEvents.CognitoPreTokenGenerationEvent>(function.FunctionHandlerAsync, serializer);
//#endif
        await bootstrap.Build().RunAsync().ConfigureAwait(false);
    }
}

//#if (preTokenV2)
[JsonSerializable(typeof(Amazon.Lambda.CognitoEvents.CognitoPreTokenGenerationV2Event))]
//#else
[JsonSerializable(typeof(Amazon.Lambda.CognitoEvents.CognitoPreTokenGenerationEvent))]
//#endif
internal partial class LambdaJsonSerializerContext : JsonSerializerContext;
