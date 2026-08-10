# Cognito pre-sign-up sample

Use this sample when you want to see a concrete Cognito trigger specialization rather than the generic event/request programming model.

```text
Cognito user pool
  → pre-sign-up trigger
  → Lambda
  → CognitoPreSignupEvent
  → CognitoPreSignUpFunction<PreSignUpHandler>
  → modified CognitoPreSignupEvent
  → Cognito
```

## Minimal infrastructure

```hcl
resource "aws_cognito_user_pool" "users" {
  name = "users"

  lambda_config {
    pre_sign_up = aws_lambda_function.sample.arn
  }
}

resource "aws_lambda_permission" "cognito" {
  statement_id  = "AllowExecutionFromCognito"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.sample.function_name
  principal     = "cognito-idp.amazonaws.com"
  source_arn    = aws_cognito_user_pool.users.arn
}
```

The Lambda function, IAM role, and deployment package are omitted.

## Example Lambda input

A trimmed pre-sign-up request looks like this:

```json
{
  "version": "1",
  "triggerSource": "PreSignUp_SignUp",
  "region": "eu-north-1",
  "userPoolId": "eu-north-1_example",
  "userName": "user@example.com",
  "request": {
    "userAttributes": {
      "email": "user@example.com"
    }
  },
  "response": {
    "autoConfirmUser": false,
    "autoVerifyEmail": false,
    "autoVerifyPhone": false
  }
}
```

Unlike fire-and-forget event sources, Cognito expects the trigger event back. The handler can inspect the request and mutate the response section to influence Cognito's behavior.

`Function` derives from `CognitoPreSignUpFunction<PreSignUpHandler>`. `PreSignUpHandler` receives the strongly typed `CognitoPreSignupEvent`, ensures a response object exists, sets `AutoConfirmUser`, and returns the modified event.

## Look at

- `Function` for the pre-sign-up specialization.
- `PreSignUpHandler` for reading the trigger request and mutating `CognitoPreSignupResponse`.

The Cognito package contains dedicated specializations for the other supported trigger contracts; this sample focuses on one trigger so the request/response shape remains easy to see.
