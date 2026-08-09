using Amazon.Lambda.Core;
using Amazon.S3;

using Kralizek.Lambda;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace LambdaFunctionProject;

public sealed class Function : S3Function<S3ObjectEventHandler>
{
    protected override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client());
    }
}
