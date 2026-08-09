using Amazon.Lambda.Core;

using Kralizek.Lambda;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace LambdaFunctionProject;

public sealed class Function : S3BatchFunction<S3BatchItemHandler>;
