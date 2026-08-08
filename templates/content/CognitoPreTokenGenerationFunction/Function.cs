using Amazon.Lambda.Core;
using Kralizek.Lambda;
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace LambdaFunctionProject;
#if (preTokenV2)
public sealed class Function : CognitoPreTokenGenerationV2Function<PreTokenGenerationHandler>;
#else
public sealed class Function : CognitoPreTokenGenerationFunction<PreTokenGenerationHandler>;
#endif
