# Kralizek.Lambda.Template.Sqs

Amazon SQS specialization with raw or decoded messages, source-specific results, partial-batch failures, and optional bounded parallelism.

```csharp
public sealed class Function : SqsFunction<OrderCreated, OrderCreatedHandler>;
```

For usage, processing semantics, and examples, see [SQS](https://github.com/Kralizek/AWSLambdaSharpTemplate/blob/master/docs/SQS.md).

The complete library documentation is available in the [`docs/` directory](https://github.com/Kralizek/AWSLambdaSharpTemplate/blob/master/docs/README.md).
