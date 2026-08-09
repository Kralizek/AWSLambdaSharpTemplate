# Kralizek.Lambda.Template.KinesisStreams

Amazon Kinesis Data Streams specialization with raw or decoded binary records and partial-batch failure support.

```csharp
public sealed class Function : KinesisStreamFunction<OrderCreated, OrderCreatedHandler>;
```

For usage, processing semantics, and examples, see [Kinesis Streams](https://github.com/Kralizek/AWSLambdaSharpTemplate/blob/HEAD/docs/Kinesis-Streams.md).

The complete library documentation is available in the [`docs/` directory](https://github.com/Kralizek/AWSLambdaSharpTemplate/blob/HEAD/docs/README.md).
