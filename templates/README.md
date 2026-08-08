# Kralizek.Lambda.Templates

Project templates for building AWS Lambda functions with `Kralizek.Lambda.Template`.

Install with:

```bash
dotnet new install Kralizek.Lambda.Templates
```

List all templates with:

```bash
dotnet new list lambda-template
```

Alongside the generic Event/Request, SNS, and SQS templates, the package ships one Cognito template per supported trigger: pre sign-up, post confirmation, pre/post authentication, define/create/verify auth challenge, custom message, user migration, custom email sender, custom SMS sender, and pre-token generation.

The pre-token-generation template supports both event contracts exposed by `Amazon.Lambda.CognitoEvents` 5.x:

```bash
dotnet new lambda-template-cognito-pre-token-generation --name MyTokenHook --version v1
dotnet new lambda-template-cognito-pre-token-generation --name MyTokenHook --version v2
```

Every Cognito template uses the split function/handler model. For example:

```csharp
public sealed class Function : CognitoPreSignUpFunction<PreSignUpHandler>;

public sealed class PreSignUpHandler : ICognitoPreSignUpHandler
{
    // application logic
}
```

All templates support `--profile`, `--region`, and `--role`. The generated project includes `aws-lambda-tools-defaults.json` and references the matching version of the relevant runtime package.
