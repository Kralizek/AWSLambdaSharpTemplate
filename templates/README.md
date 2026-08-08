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
dotnet new lambda-template-sns \
  --name MySnsFunction \
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

## Package compatibility

`Kralizek.Lambda.Templates`, `Kralizek.Lambda.Template`, and source-specific packages such as `Kralizek.Lambda.Template.Cognito`, `Kralizek.Lambda.Template.Sns`, and `Kralizek.Lambda.Template.Sqs` use the same package version for a given release. A generated project therefore targets the exact runtime version that corresponds to the installed template package.

For the runtime programming model and API documentation, see the `Kralizek.Lambda.Template` package.