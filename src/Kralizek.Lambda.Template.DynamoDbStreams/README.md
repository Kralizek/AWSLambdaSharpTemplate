# Kralizek.Lambda.Template.DynamoDbStreams

Amazon DynamoDB Streams specialization with synthetic item changes, source-specific results, and partial-batch failure support.

```csharp
public sealed class Function : DynamoDbStreamFunction<OrderChangeHandler>;
```

For usage, processing semantics, and examples, see [DynamoDB Streams](https://github.com/Kralizek/AWSLambdaSharpTemplate/blob/HEAD/docs/DynamoDB-Streams.md).

The complete library documentation is available in the [`docs/` directory](https://github.com/Kralizek/AWSLambdaSharpTemplate/blob/HEAD/docs/README.md).
