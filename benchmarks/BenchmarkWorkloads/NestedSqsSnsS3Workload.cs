namespace BenchmarkWorkloads;

public static class NestedSqsSnsS3Workload
{
    public const string BucketName = "benchmark-bucket";
    public const string ObjectKey = "benchmark-key.txt";

    public const string S3EventJson = "{\"Records\":[{\"eventVersion\":\"2.1\",\"eventSource\":\"aws:s3\",\"awsRegion\":\"eu-north-1\",\"eventTime\":\"2026-08-18T00:00:00.000Z\",\"eventName\":\"ObjectCreated:Put\",\"userIdentity\":{\"principalId\":\"benchmark\"},\"requestParameters\":{\"sourceIPAddress\":\"127.0.0.1\"},\"responseElements\":{\"x-amz-request-id\":\"benchmark\",\"x-amz-id-2\":\"benchmark\"},\"s3\":{\"s3SchemaVersion\":\"1.0\",\"configurationId\":\"benchmark\",\"bucket\":{\"name\":\"benchmark-bucket\",\"ownerIdentity\":{\"principalId\":\"benchmark\"},\"arn\":\"arn:aws:s3:::benchmark-bucket\"},\"object\":{\"key\":\"benchmark-key.txt\",\"size\":1,\"eTag\":\"benchmark\",\"sequencer\":\"1\"}}}]}";

    public const string SnsEnvelopeJson = "{\"Message\":\"{\\\"Records\\\":[{\\\"eventVersion\\\":\\\"2.1\\\",\\\"eventSource\\\":\\\"aws:s3\\\",\\\"awsRegion\\\":\\\"eu-north-1\\\",\\\"eventTime\\\":\\\"2026-08-18T00:00:00.000Z\\\",\\\"eventName\\\":\\\"ObjectCreated:Put\\\",\\\"userIdentity\\\":{\\\"principalId\\\":\\\"benchmark\\\"},\\\"requestParameters\\\":{\\\"sourceIPAddress\\\":\\\"127.0.0.1\\\"},\\\"responseElements\\\":{\\\"x-amz-request-id\\\":\\\"benchmark\\\",\\\"x-amz-id-2\\\":\\\"benchmark\\\"},\\\"s3\\\":{\\\"s3SchemaVersion\\\":\\\"1.0\\\",\\\"configurationId\\\":\\\"benchmark\\\",\\\"bucket\\\":{\\\"name\\\":\\\"benchmark-bucket\\\",\\\"ownerIdentity\\\":{\\\"principalId\\\":\\\"benchmark\\\"},\\\"arn\\\":\\\"arn:aws:s3:::benchmark-bucket\\\"},\\\"object\\\":{\\\"key\\\":\\\"benchmark-key.txt\\\",\\\"size\\\":1,\\\"eTag\\\":\\\"benchmark\\\",\\\"sequencer\\\":\\\"1\\\"}}}]}\"}";

    public static string Execute(string bucketName, string objectKey) =>
        $"{bucketName}/{objectKey}".ToUpperInvariant();
}
