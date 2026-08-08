# Kralizek.Lambda.Template.Sns

Source-specific support for AWS Lambda functions triggered by Amazon SNS.

## Typed notifications

Derive from `SnsFunction<TNotification, THandler>` when the SNS `Message` contains an application payload that should be decoded before dispatch:

```csharp
public sealed class Function : SnsFunction<OrderCreated, OrderCreatedHandler>;
```

The consumer handler implements `ISnsNotificationHandler<TNotification>`:

```csharp
public sealed class OrderCreatedHandler : ISnsNotificationHandler<OrderCreated>
{
    public ValueTask HandleAsync(
        OrderCreated notification,
        SnsNotificationContext context,
        CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
```

The default decoder is `JsonStringPayloadDecoder<TNotification>`, using `System.Text.Json` web defaults. Register another `IStringPayloadDecoder<TNotification>` in `ConfigureServices(...)` to preserve the legacy custom-serializer use case or to use source-generated JSON metadata.

## Raw SNS records

Use `SnsFunction<THandler>` with an `ISnsRecordHandler` when the handler needs the original `SNSEvent.SNSRecord`:

```csharp
public sealed class Function : SnsFunction<RawNotificationHandler>;
```

The raw record is also preserved for typed handlers and can be retrieved from `SnsNotificationContext` with `GetSnsRecord()`.

## Multiple notifications and scopes

SNS events may contain multiple records. The function dispatches one handler invocation per record and creates an independent dependency-injection scope for every record. Sequential processing is the default.

Use `ParallelSnsFunction<TNotification, THandler>` or `ParallelSnsFunction<THandler>` for bounded parallel processing. Override `MaxDegreeOfParallelism` to select the maximum number of records processed concurrently.

The default consumer-handler lifetime is scoped. Applications can register the concrete handler type explicitly in `ConfigureServices(...)` when another lifetime is required.

## Failure behavior

SNS does not support a partial-batch failure response. If decoding or handling any record fails, the exception is propagated and the entire Lambda invocation fails. This preserves SNS/Lambda retry semantics instead of acknowledging the other records independently.

## Context metadata

`SnsNotificationContext` exposes common Lambda invocation metadata together with SNS-specific values including message ID, topic ARN, subject, timestamp, message type and attributes, subscription ARN, event source and event version.