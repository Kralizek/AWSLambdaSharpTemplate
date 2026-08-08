using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.CognitoEvents;

using Kralizek.Lambda;

using NUnit.Framework;

namespace Tests.Lambda.Cognito;

[TestFixture]
public class CognitoFunctionTests
{
    [Test]
    public async Task PreSignUp_dispatches_the_typed_event_and_returns_handler_result()
    {
        PreSignUpHandler.ReceivedEvent = null;
        PreSignUpHandler.ReceivedContext = null;

        var input = new CognitoPreSignupEvent();
        var sut = new PreSignUpFunction();
        var lambdaContext = TestLambdaContexts.Create();

        var result = await sut.FunctionHandlerAsync(input, lambdaContext);

        Assert.That(result, Is.SameAs(input));
        Assert.That(PreSignUpHandler.ReceivedEvent, Is.SameAs(input));
        Assert.That(PreSignUpHandler.ReceivedContext?.GetLambdaContext(), Is.SameAs(lambdaContext));
    }

    private sealed class PreSignUpFunction : CognitoPreSignUpFunction<PreSignUpHandler>;

    private sealed class PreSignUpHandler : ICognitoPreSignUpHandler
    {
        public static CognitoPreSignupEvent? ReceivedEvent { get; set; }
        public static RequestContext? ReceivedContext { get; set; }

        public ValueTask<CognitoPreSignupEvent> HandleAsync(
            CognitoPreSignupEvent input,
            RequestContext context,
            CancellationToken cancellationToken)
        {
            ReceivedEvent = input;
            ReceivedContext = context;
            return ValueTask.FromResult(input);
        }
    }

    private sealed class CreateAuthChallengeFunction : CognitoCreateAuthChallengeFunction<CreateAuthChallengeHandler>;
    private sealed class CreateAuthChallengeHandler : ICognitoCreateAuthChallengeHandler
    {
        public ValueTask<CognitoCreateAuthChallengeEvent> HandleAsync(CognitoCreateAuthChallengeEvent input, RequestContext context, CancellationToken cancellationToken) => ValueTask.FromResult(input);
    }

    private sealed class CustomEmailSenderFunction : CognitoCustomEmailSenderFunction<CustomEmailSenderHandler>;
    private sealed class CustomEmailSenderHandler : ICognitoCustomEmailSenderHandler
    {
        public ValueTask<CognitoCustomEmailSenderEvent> HandleAsync(CognitoCustomEmailSenderEvent input, RequestContext context, CancellationToken cancellationToken) => ValueTask.FromResult(input);
    }

    private sealed class CustomMessageFunction : CognitoCustomMessageFunction<CustomMessageHandler>;
    private sealed class CustomMessageHandler : ICognitoCustomMessageHandler
    {
        public ValueTask<CognitoCustomMessageEvent> HandleAsync(CognitoCustomMessageEvent input, RequestContext context, CancellationToken cancellationToken) => ValueTask.FromResult(input);
    }

    private sealed class CustomSmsSenderFunction : CognitoCustomSmsSenderFunction<CustomSmsSenderHandler>;
    private sealed class CustomSmsSenderHandler : ICognitoCustomSmsSenderHandler
    {
        public ValueTask<CognitoCustomSmsSenderEvent> HandleAsync(CognitoCustomSmsSenderEvent input, RequestContext context, CancellationToken cancellationToken) => ValueTask.FromResult(input);
    }

    private sealed class DefineAuthChallengeFunction : CognitoDefineAuthChallengeFunction<DefineAuthChallengeHandler>;
    private sealed class DefineAuthChallengeHandler : ICognitoDefineAuthChallengeHandler
    {
        public ValueTask<CognitoDefineAuthChallengeEvent> HandleAsync(CognitoDefineAuthChallengeEvent input, RequestContext context, CancellationToken cancellationToken) => ValueTask.FromResult(input);
    }

    private sealed class UserMigrationFunction : CognitoUserMigrationFunction<UserMigrationHandler>;
    private sealed class UserMigrationHandler : ICognitoUserMigrationHandler
    {
        public ValueTask<CognitoMigrateUserEvent> HandleAsync(CognitoMigrateUserEvent input, RequestContext context, CancellationToken cancellationToken) => ValueTask.FromResult(input);
    }

    private sealed class PostAuthenticationFunction : CognitoPostAuthenticationFunction<PostAuthenticationHandler>;
    private sealed class PostAuthenticationHandler : ICognitoPostAuthenticationHandler
    {
        public ValueTask<CognitoPostAuthenticationEvent> HandleAsync(CognitoPostAuthenticationEvent input, RequestContext context, CancellationToken cancellationToken) => ValueTask.FromResult(input);
    }

    private sealed class PostConfirmationFunction : CognitoPostConfirmationFunction<PostConfirmationHandler>;
    private sealed class PostConfirmationHandler : ICognitoPostConfirmationHandler
    {
        public ValueTask<CognitoPostConfirmationEvent> HandleAsync(CognitoPostConfirmationEvent input, RequestContext context, CancellationToken cancellationToken) => ValueTask.FromResult(input);
    }

    private sealed class PreAuthenticationFunction : CognitoPreAuthenticationFunction<PreAuthenticationHandler>;
    private sealed class PreAuthenticationHandler : ICognitoPreAuthenticationHandler
    {
        public ValueTask<CognitoPreAuthenticationEvent> HandleAsync(CognitoPreAuthenticationEvent input, RequestContext context, CancellationToken cancellationToken) => ValueTask.FromResult(input);
    }

    private sealed class PreTokenGenerationFunction : CognitoPreTokenGenerationFunction<PreTokenGenerationHandler>;
    private sealed class PreTokenGenerationHandler : ICognitoPreTokenGenerationHandler
    {
        public ValueTask<CognitoPreTokenGenerationEvent> HandleAsync(CognitoPreTokenGenerationEvent input, RequestContext context, CancellationToken cancellationToken) => ValueTask.FromResult(input);
    }

    private sealed class PreTokenGenerationV2Function : CognitoPreTokenGenerationV2Function<PreTokenGenerationV2Handler>;
    private sealed class PreTokenGenerationV2Handler : ICognitoPreTokenGenerationV2Handler
    {
        public ValueTask<CognitoPreTokenGenerationV2Event> HandleAsync(CognitoPreTokenGenerationV2Event input, RequestContext context, CancellationToken cancellationToken) => ValueTask.FromResult(input);
    }

    private sealed class VerifyAuthChallengeFunction : CognitoVerifyAuthChallengeFunction<VerifyAuthChallengeHandler>;
    private sealed class VerifyAuthChallengeHandler : ICognitoVerifyAuthChallengeHandler
    {
        public ValueTask<CognitoVerifyAuthChallengeEvent> HandleAsync(CognitoVerifyAuthChallengeEvent input, RequestContext context, CancellationToken cancellationToken) => ValueTask.FromResult(input);
    }
}
