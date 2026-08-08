using Amazon.Lambda.Core;

using Kralizek.Lambda;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace RawSqsFunction;

public sealed class Function : SqsFunction<RawSqsRecordHandler>;