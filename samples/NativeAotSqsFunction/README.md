# Native AOT SQS function

This sample is the Native AOT form of the typed SQS programming model. The Lambda boundary (`SQSEvent` / `SQSBatchResponse`) and the application payload (`OrderCreated`) are all registered with a source-generated `JsonSerializerContext`.

The framework still owns SQS record processing and partial-batch-response behavior. The application only replaces the default JSON payload decoder with one backed by generated metadata:

```csharp
services.AddSingleton<IStringPayloadDecoder<OrderCreated>>(
    new JsonStringPayloadDecoder<OrderCreated>(LambdaJsonSerializerContext.Default.OrderCreated));
```

When the payload type changes, add the new application type to `LambdaJsonSerializerContext` as well. AWS/framework boundary types required by the selected template are already represented by the generated AOT host.

Publish for Linux x64 with:

```bash
dotnet publish -c Release -r linux-x64 --self-contained true
```

`aws-lambda-tools-defaults.json` keeps the managed .NET 10 Lambda runtime, while the handler becomes the executable assembly name rather than a class/method handler string.
