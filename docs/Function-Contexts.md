# Function Contexts

Handlers receive framework contexts rather than depending directly on `Amazon.Lambda.Core.ILambdaContext`.

The source-neutral contexts are:

- `EventContext`
- `RequestContext`
- `RecordContext`

They expose common Lambda invocation metadata through CLR properties. Source-specific integrations derive richer contexts for delivery metadata, such as `SqsMessageContext`, `SnsNotificationContext`, `DynamoDbStreamRecordContext`, and `KinesisStreamRecordContext`.

The original AWS Lambda context is preserved as an escape hatch and can be retrieved with `GetLambdaContext()`. Source-specific contexts similarly preserve the original AWS record where useful, for example `GetSqsMessage()`, `GetSnsRecord()`, and `GetDynamoDbStreamRecord()`.
