# Native AOT SQS function

This sample is the Native AOT form of the typed SQS programming model. The Lambda boundary (`SQSEvent` / `SQSBatchResponse`) and the nested application payload (`OrderCreated`) use separate source-generated `JsonSerializerContext` types.

The framework still owns SQS record processing, handler registration, and partial-batch-response behavior. The application registers generated payload metadata through `ConfigureFrameworkServices`:

```csharp
services.AddSingleton(PayloadJsonSerializerContext.Default.OrderCreated);
```

`Program.cs` owns `LambdaJsonSerializerContext` for the Lambda boundary. `OrderCreatedHandler.cs` owns `PayloadJsonSerializerContext` for the application payload. When the payload type changes, update the payload context and its `JsonTypeInfo<T>` registration together.

Publish for Linux x64 with:

```bash
dotnet publish -c Release -r linux-x64 --self-contained true --warnaserror
```

`aws-lambda-tools-defaults.json` keeps the managed .NET 10 Lambda runtime, while the handler becomes the executable assembly name rather than a class/method handler string.
