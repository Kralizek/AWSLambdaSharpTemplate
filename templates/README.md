# Kralizek.Lambda.Templates

Project templates for building AWS Lambda functions with `Kralizek.Lambda.Template`.

The package contains `dotnet new` templates that start from the semantic function model provided by the runtime library. The template package and runtime packages are versioned together, and generated projects reference the matching runtime package version.

## Install

```bash
dotnet new install Kralizek.Lambda.Templates
```

List the installed templates with:

```bash
dotnet new list lambda-template
```

## Available templates

| Template | Short name | Use when |
| --- | --- | --- |
| Event Function | `lambda-template-event` | The Lambda handles an input and does not return an application result. |
| Request Function | `lambda-template-request` | The Lambda handles an input and returns an application result. |
| EventBridge Function | `lambda-template-eventbridge` | The Lambda is targeted by EventBridge and receives a strongly typed event detail inside the AWS event envelope. |
| DynamoDB Streams Function | `lambda-template-dynamodb-stream` | The Lambda consumes DynamoDB Streams records with source-specific context and partial-batch failure support. |
| S3 Function | `lambda-template-s3` | The Lambda reacts to native S3 event notifications using a synthetic object-event model. |
| S3 Batch Function | `lambda-template-s3-batch` | The Lambda processes S3 Batch Operations tasks using invocation schema 2.0. |
| SNS Function | `lambda-template-sns` | The Lambda is triggered by SNS and processes each decoded notification independently. |
| SQS Function | `lambda-template-sqs` | The Lambda is triggered by SQS and processes each decoded message independently with partial-batch failure support. |
| Cognito Pre Sign-up | `lambda-template-cognito-pre-signup` | The Lambda validates or modifies a user-pool sign-up request before Cognito creates the user. |
| Cognito Post Confirmation | `lambda-template-cognito-post-confirmation` | The Lambda reacts after a user confirms registration or a password recovery flow. |
| Cognito Pre Authentication | `lambda-template-cognito-pre-authentication` | The Lambda validates an authentication attempt before Cognito proceeds. |
| Cognito Post Authentication | `lambda-template-cognito-post-authentication` | The Lambda reacts after Cognito authenticates a user. |
| Cognito Define Auth Challenge | `lambda-template-cognito-define-auth-challenge` | The Lambda controls the state machine for a custom authentication flow. |
| Cognito Create Auth Challenge | `lambda-template-cognito-create-auth-challenge` | The Lambda creates a challenge for a custom authentication flow. |
| Cognito Verify Auth Challenge | `lambda-template-cognito-verify-auth-challenge` | The Lambda verifies the user's response to a custom authentication challenge. |
| Cognito Custom Message | `lambda-template-cognito-custom-message` | The Lambda customizes Cognito-generated email and SMS message content. |
| Cognito User Migration | `lambda-template-cognito-user-migration` | The Lambda migrates users from an existing identity store during sign-in or password reset. |
| Cognito Custom Email Sender | `lambda-template-cognito-custom-email-sender` | The Lambda delivers Cognito email messages through a custom sender. |
| Cognito Custom SMS Sender | `lambda-template-cognito-custom-sms-sender` | The Lambda delivers Cognito SMS messages through a custom sender. |
| Cognito Pre Token Generation | `lambda-template-cognito-pre-token-generation` | The Lambda customizes claims and scopes before Cognito issues tokens. Supports `--version v1|v2`. |

## Create a function

Create an event function:

```bash
dotnet new lambda-template-event --name MyEventFunction
```

Create a request function:

```bash
dotnet new lambda-template-request --name MyRequestFunction
```

Create an EventBridge function:

```bash
dotnet new lambda-template-eventbridge --name MyEventBridgeFunction
```

Create a DynamoDB Streams function:

```bash
dotnet new lambda-template-dynamodb-stream --name MyDynamoDbStreamFunction
```

Create an S3 notification function:

```bash
dotnet new lambda-template-s3 --name MyS3Function
```

Create an S3 Batch Operations function:

```bash
dotnet new lambda-template-s3-batch --name MyS3BatchFunction
```

Create an SNS function:

```bash
dotnet new lambda-template-sns --name MySnsFunction
```

Create an SQS function:

```bash
dotnet new lambda-template-sqs --name MySqsFunction
```

Create a Cognito pre sign-up function:

```bash
dotnet new lambda-template-cognito-pre-signup --name MyPreSignUpFunction
```

The pre-token-generation template supports both event contracts exposed by `Amazon.Lambda.CognitoEvents` 5.x:

```bash
dotnet new lambda-template-cognito-pre-token-generation --name MyTokenHook --version v1
dotnet new lambda-template-cognito-pre-token-generation --name MyTokenHook --version v2
```

All templates support the AWS Lambda deployment settings exposed by the template package:

- `--profile` for the AWS credential profile used by the Lambda tools.
- `--region` for the AWS region.
- `--role` for the Lambda execution role.

For example:

```bash
dotnet new lambda-template-dynamodb-stream \
  --name MyDynamoDbStreamFunction \
  --profile my-profile \
  --region eu-north-1 \
  --role my-lambda-role
```

The generated project includes `aws-lambda-tools-defaults.json`, so it can be packaged and deployed with the standard `dotnet lambda` tooling.

## Programming model

An Event Function derives from `EventFunction<TInput, THandler>` and delegates application logic to an `IEventHandler<TInput>`:

```csharp
public class Function : EventFunction<string, StringEventHandler>
{
}
```

A Request Function derives from `RequestFunction<TInput, TOutput, THandler>` and delegates application logic to an `IRequestHandler<TInput, TOutput>`:

```csharp
public class Function : RequestFunction<string, string, ToUpperStringRequestHandler>
{
}
```

An EventBridge Function derives from `EventBridgeFunction<TDetail, THandler>` and delegates the complete AWS `CloudWatchEvent<TDetail>` envelope to an `IEventBridgeHandler<TDetail>`:

```csharp
public sealed class Function : EventBridgeFunction<OrderCreated, OrderCreatedHandler>;
```

The Lambda serializer materializes `CloudWatchEvent<TDetail>.Detail` directly as `TDetail`, so EventBridge does not use a separate payload-decoder abstraction. The AWS envelope remains available to application code for fields such as `Source`, `DetailType`, `Id`, and `Resources`.

A DynamoDB Streams Function derives from `DynamoDbStreamFunction<THandler>` and invokes an `IDynamoDbStreamRecordHandler` once per stream record:

```csharp
public sealed class Function : DynamoDbStreamFunction<OrderChangeHandler>;
```

The handler receives a `DynamoDbStreamItem` containing keys, old/new images, sequence number and stream metadata together with `DynamoDbStreamRecordContext` for the outer event metadata. The original AWS stream record remains available through `context.GetDynamoDbStreamRecord()`. DynamoDB images remain in AWS's `DynamoDBEvent.AttributeValue` model rather than being treated as JSON. Record failures are translated into `StreamsEventResponse` partial-batch failures; the event-source mapping must enable `ReportBatchItemFailures` for Lambda to honor them. Records are processed sequentially within each invocation; applications that need more throughput should configure `ParallelizationFactor` on the DynamoDB Streams event-source mapping.

An S3 Function derives from `S3Function<THandler>` and invokes an `IS3ObjectEventHandler` for each native S3 notification record:

```csharp
public sealed class Function : S3Function<ObjectUploadedHandler>;
```

The handler receives an `S3ObjectEvent` containing the decoded `S3ObjectReference`, the typed-but-forward-compatible `S3EventName`, event time, and sequencer. The original AWS notification record remains available through `context.GetS3EventRecord()`. Native S3 notification records are processed sequentially.

An S3 Batch Function derives from `S3BatchFunction<THandler>` and invokes an `IS3BatchItemHandler` for each Batch Operations task:

```csharp
public sealed class Function : S3BatchFunction<BatchItemHandler>;
```

The Batch integration supports invocation schema 2.0. The handler receives an `S3BatchItem` with an extensible `S3BatchTaskKey`; the initial key type is `S3BatchObjectKey`, which wraps the shared `S3ObjectReference`. Return `S3BatchResult.Succeeded()`, `TemporaryFailure()`, or `PermanentFailure()` to control the task result. Batch tasks are processed sequentially within each Lambda invocation.

An SNS Function derives from `SnsFunction<TNotification, THandler>`. The framework decodes each SNS `Message` to `TNotification`, creates an `SnsNotificationContext`, and invokes the consumer's `ISnsNotificationHandler<TNotification>`:

```csharp
public sealed class Function : SnsFunction<OrderCreated, OrderCreatedHandler>;
```

SNS processes records sequentially by default and fails the whole Lambda invocation if any notification fails. Applications that need bounded concurrency can derive from `ParallelSnsFunction<...>` instead. The default SNS payload decoder uses `System.Text.Json` and can be replaced through `IStringPayloadDecoder<TNotification>`.

An SQS Function derives from `SqsFunction<TMessage, THandler>`. The framework decodes each SQS body to `TMessage`, creates an `SqsMessageContext`, and invokes the consumer's `ISqsMessageHandler<TMessage>`:

```csharp
public sealed class Function : SqsFunction<OrderCreated, OrderCreatedHandler>;
```

The default SQS payload decoder uses `System.Text.Json`. Applications can replace `IStringPayloadDecoder<TMessage>` through the normal dependency-injection customization hook when a different payload representation or source-generated JSON metadata is required.

A Cognito Function derives from the function base for its user-pool trigger and delegates application logic to the matching trigger-specific handler interface:

```csharp
public sealed class Function : CognitoPreSignUpFunction<PreSignUpHandler>;

public sealed class PreSignUpHandler : ICognitoPreSignUpHandler
{
    // application logic
}
```

Cognito trigger bases specialize the request-function model with the corresponding AWS event contract. Pre-token-generation V1 and V2 use separate strongly typed runtime bases; the template's `--version` option selects the appropriate one.

## Package family

`Kralizek.Lambda.Templates` is the easiest entry point when starting a new Lambda function. The generated project references the runtime package appropriate for the selected template.

| Package | Stable | Latest | Downloads | Purpose |
| --- | --- | --- | --- | --- |
| [`Kralizek.Lambda.Templates`](https://www.nuget.org/packages/Kralizek.Lambda.Templates) | [![stable](https://img.shields.io/nuget/v/Kralizek.Lambda.Templates?label=stable)](https://www.nuget.org/packages/Kralizek.Lambda.Templates) | [![latest](https://img.shields.io/nuget/vpre/Kralizek.Lambda.Templates?label=latest)](https://www.nuget.org/packages/Kralizek.Lambda.Templates) | [![downloads](https://img.shields.io/nuget/dt/Kralizek.Lambda.Templates?label=downloads)](https://www.nuget.org/packages/Kralizek.Lambda.Templates) | `dotnet new` templates for the supported Lambda function models and AWS event sources. |
| [`Kralizek.Lambda.Template.Abstractions`](https://www.nuget.org/packages/Kralizek.Lambda.Template.Abstractions) | [![stable](https://img.shields.io/nuget/v/Kralizek.Lambda.Template.Abstractions?label=stable)](https://www.nuget.org/packages/Kralizek.Lambda.Template.Abstractions) | [![latest](https://img.shields.io/nuget/vpre/Kralizek.Lambda.Template.Abstractions?label=latest)](https://www.nuget.org/packages/Kralizek.Lambda.Template.Abstractions) | [![downloads](https://img.shields.io/nuget/dt/Kralizek.Lambda.Template.Abstractions?label=downloads)](https://www.nuget.org/packages/Kralizek.Lambda.Template.Abstractions) | Runtime-independent handler contracts, contexts, record results and payload-decoder abstractions. |
| [`Kralizek.Lambda.Template`](https://www.nuget.org/packages/Kralizek.Lambda.Template) | [![stable](https://img.shields.io/nuget/v/Kralizek.Lambda.Template?label=stable)](https://www.nuget.org/packages/Kralizek.Lambda.Template) | [![latest](https://img.shields.io/nuget/vpre/Kralizek.Lambda.Template?label=latest)](https://www.nuget.org/packages/Kralizek.Lambda.Template) | [![downloads](https://img.shields.io/nuget/dt/Kralizek.Lambda.Template?label=downloads)](https://www.nuget.org/packages/Kralizek.Lambda.Template) | Core Lambda runtime, including Event, Request and Record function models, lifecycle, dependency injection and framework telemetry. |
| [`Kralizek.Lambda.Template.Cognito`](https://www.nuget.org/packages/Kralizek.Lambda.Template.Cognito) | [![stable](https://img.shields.io/nuget/v/Kralizek.Lambda.Template.Cognito?label=stable)](https://www.nuget.org/packages/Kralizek.Lambda.Template.Cognito) | [![latest](https://img.shields.io/nuget/vpre/Kralizek.Lambda.Template.Cognito?label=latest)](https://www.nuget.org/packages/Kralizek.Lambda.Template.Cognito) | [![downloads](https://img.shields.io/nuget/dt/Kralizek.Lambda.Template.Cognito?label=downloads)](https://www.nuget.org/packages/Kralizek.Lambda.Template.Cognito) | Cognito User Pool trigger specializations and handler contracts. |
| [`Kralizek.Lambda.Template.DynamoDbStreams`](https://www.nuget.org/packages/Kralizek.Lambda.Template.DynamoDbStreams) | [![stable](https://img.shields.io/nuget/v/Kralizek.Lambda.Template.DynamoDbStreams?label=stable)](https://www.nuget.org/packages/Kralizek.Lambda.Template.DynamoDbStreams) | [![latest](https://img.shields.io/nuget/vpre/Kralizek.Lambda.Template.DynamoDbStreams?label=latest)](https://www.nuget.org/packages/Kralizek.Lambda.Template.DynamoDbStreams) | [![downloads](https://img.shields.io/nuget/dt/Kralizek.Lambda.Template.DynamoDbStreams?label=downloads)](https://www.nuget.org/packages/Kralizek.Lambda.Template.DynamoDbStreams) | DynamoDB Streams record processing and partial-batch response support. |
| [`Kralizek.Lambda.Template.EventBridge`](https://www.nuget.org/packages/Kralizek.Lambda.Template.EventBridge) | [![stable](https://img.shields.io/nuget/v/Kralizek.Lambda.Template.EventBridge?label=stable)](https://www.nuget.org/packages/Kralizek.Lambda.Template.EventBridge) | [![latest](https://img.shields.io/nuget/vpre/Kralizek.Lambda.Template.EventBridge?label=latest)](https://www.nuget.org/packages/Kralizek.Lambda.Template.EventBridge) | [![downloads](https://img.shields.io/nuget/dt/Kralizek.Lambda.Template.EventBridge?label=downloads)](https://www.nuget.org/packages/Kralizek.Lambda.Template.EventBridge) | EventBridge event-envelope specialization and source-specific context. |
| [`Kralizek.Lambda.Template.KinesisStreams`](https://www.nuget.org/packages/Kralizek.Lambda.Template.KinesisStreams) | [![stable](https://img.shields.io/nuget/v/Kralizek.Lambda.Template.KinesisStreams?label=stable)](https://www.nuget.org/packages/Kralizek.Lambda.Template.KinesisStreams) | [![latest](https://img.shields.io/nuget/vpre/Kralizek.Lambda.Template.KinesisStreams?label=latest)](https://www.nuget.org/packages/Kralizek.Lambda.Template.KinesisStreams) | [![downloads](https://img.shields.io/nuget/dt/Kralizek.Lambda.Template.KinesisStreams?label=downloads)](https://www.nuget.org/packages/Kralizek.Lambda.Template.KinesisStreams) | Kinesis Streams record processing, payload decoding and checkpoint/partial-batch behavior. |
| [`Kralizek.Lambda.Template.S3`](https://www.nuget.org/packages/Kralizek.Lambda.Template.S3) | [![stable](https://img.shields.io/nuget/v/Kralizek.Lambda.Template.S3?label=stable)](https://www.nuget.org/packages/Kralizek.Lambda.Template.S3) | [![latest](https://img.shields.io/nuget/vpre/Kralizek.Lambda.Template.S3?label=latest)](https://www.nuget.org/packages/Kralizek.Lambda.Template.S3) | [![downloads](https://img.shields.io/nuget/dt/Kralizek.Lambda.Template.S3?label=downloads)](https://www.nuget.org/packages/Kralizek.Lambda.Template.S3) | Native S3 event notifications and S3 Batch Operations. |
| [`Kralizek.Lambda.Template.Sns`](https://www.nuget.org/packages/Kralizek.Lambda.Template.Sns) | [![stable](https://img.shields.io/nuget/v/Kralizek.Lambda.Template.Sns?label=stable)](https://www.nuget.org/packages/Kralizek.Lambda.Template.Sns) | [![latest](https://img.shields.io/nuget/vpre/Kralizek.Lambda.Template.Sns?label=latest)](https://www.nuget.org/packages/Kralizek.Lambda.Template.Sns) | [![downloads](https://img.shields.io/nuget/dt/Kralizek.Lambda.Template.Sns?label=downloads)](https://www.nuget.org/packages/Kralizek.Lambda.Template.Sns) | SNS notification processing and payload decoding. |
| [`Kralizek.Lambda.Template.Sqs`](https://www.nuget.org/packages/Kralizek.Lambda.Template.Sqs) | [![stable](https://img.shields.io/nuget/v/Kralizek.Lambda.Template.Sqs?label=stable)](https://www.nuget.org/packages/Kralizek.Lambda.Template.Sqs) | [![latest](https://img.shields.io/nuget/vpre/Kralizek.Lambda.Template.Sqs?label=latest)](https://www.nuget.org/packages/Kralizek.Lambda.Template.Sqs) | [![downloads](https://img.shields.io/nuget/dt/Kralizek.Lambda.Template.Sqs?label=downloads)](https://www.nuget.org/packages/Kralizek.Lambda.Template.Sqs) | SQS message processing, payload decoding and partial-batch response support. |

## Package compatibility

The template package and all runtime packages use the same package version for a given release. A generated project therefore targets the exact runtime version that corresponds to the installed template package.

For most applications, install `Kralizek.Lambda.Templates` and let the selected template choose the runtime package. Reference individual runtime packages directly when building a function without the templates or when sharing application code against the abstractions package.