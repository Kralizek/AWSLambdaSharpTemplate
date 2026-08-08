# Kralizek.Lambda.Template.Cognito

Cognito user-pool trigger specializations for `Kralizek.Lambda.Template`.

Each supported Cognito event has a strongly typed function base and matching handler interface. The function fixes the AWS input/output event contract; application code supplies only the handler type.

```csharp
public sealed class Function : CognitoPreSignUpFunction<PreSignUpHandler>;

public sealed class PreSignUpHandler : ICognitoPreSignUpHandler
{
    public ValueTask<CognitoPreSignupEvent> HandleAsync(
        CognitoPreSignupEvent input,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(input);
    }
}
```

Supported triggers include pre sign-up, post confirmation, pre/post authentication, custom authentication challenge triggers, custom messages, user migration, custom email/SMS senders, and pre-token generation V1/V2.

The pre-token-generation trigger uses separate strongly typed runtime bases for V1 and V2 because AWS exposes different event contracts. The project template exposes those through a single `--version` option.
