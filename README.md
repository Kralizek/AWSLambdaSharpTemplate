# AWS Lambda Sharp Template

AWS Lambda templates and runtime libraries for building .NET Lambda functions around explicit programming models instead of wiring every invocation from scratch.

The current programming model is organized around three semantic function roots:

- `EventFunction<TInput, THandler>` for one-way event handlers.
- `RequestFunction<TInput, TOutput, THandler>` for request/response handlers.
- `RecordFunction<...>` for integrations that process multiple independent records per invocation.

Source-specific packages build on those roots. SQS and SNS specialize record processing, while `Kralizek.Lambda.Template.Cognito` provides strongly typed request-function specializations for Amazon Cognito user-pool Lambda triggers.

## Packages

- `Kralizek.Lambda.Template.Abstractions` contains the source-neutral handler, context, and payload-decoder contracts.
- `Kralizek.Lambda.Template` contains the runtime implementation and generic function roots.
- `Kralizek.Lambda.Template.Cognito` contains the Cognito trigger specializations.
- `Kralizek.Lambda.Template.Sns` contains the SNS specialization.
- `Kralizek.Lambda.Template.Sqs` contains the SQS specialization.
- `Kralizek.Lambda.Templates` contains the `dotnet new` project templates.

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

| Template | Short name |
| --- | --- |
| Event Function | `lambda-template-event` |
| Request Function | `lambda-template-request` |
| SNS Function | `lambda-template-sns` |
| SQS Function | `lambda-template-sqs` |
| Cognito Pre Sign-up | `lambda-template-cognito-pre-signup` |
| Cognito Post Confirmation | `lambda-template-cognito-post-confirmation` |
| Cognito Pre Authentication | `lambda-template-cognito-pre-authentication` |
| Cognito Post Authentication | `lambda-template-cognito-post-authentication` |
| Cognito Define Auth Challenge | `lambda-template-cognito-define-auth-challenge` |
| Cognito Create Auth Challenge | `lambda-template-cognito-create-auth-challenge` |
| Cognito Verify Auth Challenge | `lambda-template-cognito-verify-auth-challenge` |
| Cognito Custom Message | `lambda-template-cognito-custom-message` |
| Cognito User Migration | `lambda-template-cognito-user-migration` |
| Cognito Custom Email Sender | `lambda-template-cognito-custom-email-sender` |
| Cognito Custom SMS Sender | `lambda-template-cognito-custom-sms-sender` |
| Cognito Pre Token Generation | `lambda-template-cognito-pre-token-generation` (`--version v1|v2`) |

Run `dotnet new list lambda-template` to list the installed templates. Generated projects target .NET 10 and include `aws-lambda-tools-defaults.json` for use with the standard AWS Lambda .NET tooling.
