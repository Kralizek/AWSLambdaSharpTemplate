# AWS Lambda Sharp Template

AWS Lambda templates and runtime libraries for building .NET Lambda functions around explicit programming models instead of wiring every invocation from scratch.

The current programming model is organized around three semantic function roots:

- `EventFunction<TInput, THandler>` for one-way event handlers.
- `RequestFunction<TInput, TOutput, THandler>` for request/response handlers.
- `RecordFunction<...>` for integrations that process multiple independent records per invocation.

Source-specific packages build on those roots. EventBridge specializes one-way event processing, DynamoDB Streams, SQS and SNS specialize record processing, while `Kralizek.Lambda.Template.Cognito` provides strongly typed request-function specializations for Amazon Cognito user-pool Lambda triggers.

## Packages

- `Kralizek.Lambda.Template.Abstractions` contains the source-neutral handler, context, and payload-decoder contracts.
- `Kralizek.Lambda.Template` contains the runtime implementation and generic function roots.
- `Kralizek.Lambda.Template.Cognito` contains the Cognito trigger specializations.
- `Kralizek.Lambda.Template.DynamoDb` contains the DynamoDB Streams specialization.
- `Kralizek.Lambda.Template.EventBridge` contains the EventBridge specialization.
- `Kralizek.Lambda.Template.Sns` contains the SNS specialization.
- `Kralizek.Lambda.Template.Sqs` contains the SQS specialization.
- `Kralizek.Lambda.Templates` contains the `dotnet new` project templates.

## DynamoDB Streams function

```csharp
public sealed class Function : DynamoDbStreamFunction<OrderChangeHandler>;
```

The DynamoDB package builds on `RecordFunction`, invokes one handler per stream record, creates one dependency-injection scope per record, and returns `StreamsEventResponse` partial-batch failures using DynamoDB sequence numbers. `Keys`, `NewImage`, and `OldImage` stay in AWS's `DynamoDBEvent.AttributeValue` representation rather than being treated as generic JSON. Sequential processing is the default; `ParallelDynamoDbStreamFunction<THandler>` is an explicit bounded-parallel alternative.

## EventBridge function

```csharp
public sealed class Function : EventBridgeFunction<OrderCreated, OrderCreatedHandler>;
```

The EventBridge package builds on `EventFunction` and uses AWS's `CloudWatchEvent<TDetail>` envelope from `Amazon.Lambda.CloudWatchEvents`. `Detail` is deserialized directly by the Lambda serializer, so EventBridge does not add a second payload-decoder layer.

## Cognito function

```csharp
public sealed class Function : CognitoPreSignUpFunction<PreSignUpHandler>;
```

Each Cognito trigger has a dedicated function base and handler interface so the AWS event contract is fixed by the glue package and application code supplies only the handler. Pre-token-generation V1 and V2 are exposed as separate runtime bases and a single template with a `--version` option.

## Project templates

Install the template package with:

```bash
dotnet new install Kralizek.Lambda.Templates
```

### Available templates

| Template | Short name | Use when |
| --- | --- | --- |
| Event Function | `lambda-template-event` | The Lambda handles an input and does not return an application result. |
| Request Function | `lambda-template-request` | The Lambda handles an input and returns an application result. |
| EventBridge Function | `lambda-template-eventbridge` | The Lambda is targeted by EventBridge and receives a strongly typed event detail inside the AWS event envelope. |
| DynamoDB Streams Function | `lambda-template-dynamodb-stream` | The Lambda consumes DynamoDB Streams records with source-specific context and partial-batch failure support. |
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

Run `dotnet new list lambda-template` to list the installed templates. Generated projects target .NET 10 and include `aws-lambda-tools-defaults.json` for use with the standard AWS Lambda .NET tooling.
