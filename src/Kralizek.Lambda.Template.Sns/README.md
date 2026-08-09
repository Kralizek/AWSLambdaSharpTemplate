# Kralizek.Lambda.Template.Sns

Amazon SNS specialization with raw or decoded notifications and sequential or bounded-parallel processing.

```csharp
public sealed class Function : SnsFunction<OrderCreated, OrderCreatedHandler>;
```

For usage, processing semantics, and examples, see [SNS](https://github.com/Kralizek/AWSLambdaSharpTemplate/blob/master/docs/SNS.md).

The complete library documentation is available in the [`docs/` directory](https://github.com/Kralizek/AWSLambdaSharpTemplate/blob/master/docs/README.md).
