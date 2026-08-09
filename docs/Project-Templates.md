# Project Templates

Install the template package with:

```bash
dotnet new install Kralizek.Lambda.Templates
```

Then run:

```bash
dotnet new list lambda-template
```

Current templates include generic Event and Request functions plus source-specific templates for EventBridge, DynamoDB Streams, Kinesis Streams, S3 event notifications, S3 Batch Operations, SNS, SQS, and Cognito triggers.

Cognito provides templates for pre sign-up, post confirmation, pre/post authentication, define/create/verify auth challenge, custom message, user migration, custom email/SMS sender, and pre-token generation. The pre-token-generation template accepts `--version v1|v2`.

Generated projects target .NET 10 and include `aws-lambda-tools-defaults.json` for the standard AWS Lambda .NET tooling.
