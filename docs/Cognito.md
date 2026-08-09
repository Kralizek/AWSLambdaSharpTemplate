# Cognito

`Kralizek.Lambda.Template.Cognito` provides strongly typed specializations for Amazon Cognito user-pool triggers.

Each supported trigger has a dedicated function base and matching handler interface. The specialization fixes the AWS input/output event contract; application code supplies the handler.

```csharp
public sealed class Function
    : CognitoPreSignUpFunction<PreSignUpHandler>;
```

Supported trigger families include pre sign-up, post confirmation, pre/post authentication, custom authentication challenges, custom messages, user migration, custom email/SMS senders, and pre-token generation.

Pre-token-generation V1 and V2 use separate runtime bases because AWS defines different event contracts. The project template exposes them through a single `--version` option.
