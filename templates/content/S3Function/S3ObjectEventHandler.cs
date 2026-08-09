using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Amazon.S3;
using Amazon.S3.Model;

using Kralizek.Lambda;

namespace LambdaFunctionProject;

public sealed class S3ObjectEventHandler(IAmazonS3 s3) : IS3ObjectEventHandler
{
    public async ValueTask HandleAsync(
        S3ObjectEvent item,
        S3RecordContext context,
        CancellationToken cancellationToken)
    {
        if (!item.EventName.IsObjectCreated)
        {
            return;
        }

        using var response = await s3.GetObjectAsync(
            new GetObjectRequest
            {
                BucketName = item.Object.Bucket,
                Key = item.Object.Key,
                VersionId = item.Object.VersionId
            },
            cancellationToken);

        using var reader = new StreamReader(response.ResponseStream);
        var content = await reader.ReadToEndAsync(cancellationToken);

        // Process the uploaded object's content here.
        _ = content;
    }
}
