# Cognito pre-sign-up sample

Use this sample when you want to see a concrete Cognito trigger specialization rather than the generic event/request programming model.

`Function` derives from `CognitoPreSignUpFunction<PreSignUpHandler>`. The handler receives the strongly typed `CognitoPreSignupEvent`, mutates its response to auto-confirm the user, and returns the event back to Cognito.

This demonstrates the common Cognito pattern: AWS sends a typed trigger request, your handler changes the response section, and the function returns the modified trigger event.

## Look at

- `Function` for the pre-sign-up specialization.
- `PreSignUpHandler` for reading the trigger request and mutating `CognitoPreSignupResponse`.

The Cognito package contains dedicated specializations for the other supported trigger contracts; this sample focuses on one trigger to keep the example small.
