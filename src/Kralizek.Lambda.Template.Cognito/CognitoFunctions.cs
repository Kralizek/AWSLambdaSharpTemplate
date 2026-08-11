using System.Diagnostics;

using Amazon.Lambda.CognitoEvents;
using Amazon.Lambda.Core;

namespace Kralizek.Lambda;

public interface ICognitoCreateAuthChallengeHandler : IRequestHandler<CognitoCreateAuthChallengeEvent, CognitoCreateAuthChallengeEvent>;
public abstract class CognitoCreateAuthChallengeFunction<THandler> : RequestFunction<CognitoCreateAuthChallengeEvent, CognitoCreateAuthChallengeEvent, THandler>
    where THandler : class, ICognitoCreateAuthChallengeHandler
{
    protected override void EnrichInvocationActivity(Activity activity, CognitoCreateAuthChallengeEvent input, ILambdaContext context) => CognitoTelemetry.Enrich(activity, input);
}

public interface ICognitoCustomEmailSenderHandler : IRequestHandler<CognitoCustomEmailSenderEvent, CognitoCustomEmailSenderEvent>;
public abstract class CognitoCustomEmailSenderFunction<THandler> : RequestFunction<CognitoCustomEmailSenderEvent, CognitoCustomEmailSenderEvent, THandler>
    where THandler : class, ICognitoCustomEmailSenderHandler
{
    protected override void EnrichInvocationActivity(Activity activity, CognitoCustomEmailSenderEvent input, ILambdaContext context) => CognitoTelemetry.Enrich(activity, input);
}

public interface ICognitoCustomMessageHandler : IRequestHandler<CognitoCustomMessageEvent, CognitoCustomMessageEvent>;
public abstract class CognitoCustomMessageFunction<THandler> : RequestFunction<CognitoCustomMessageEvent, CognitoCustomMessageEvent, THandler>
    where THandler : class, ICognitoCustomMessageHandler
{
    protected override void EnrichInvocationActivity(Activity activity, CognitoCustomMessageEvent input, ILambdaContext context) => CognitoTelemetry.Enrich(activity, input);
}

public interface ICognitoCustomSmsSenderHandler : IRequestHandler<CognitoCustomSmsSenderEvent, CognitoCustomSmsSenderEvent>;
public abstract class CognitoCustomSmsSenderFunction<THandler> : RequestFunction<CognitoCustomSmsSenderEvent, CognitoCustomSmsSenderEvent, THandler>
    where THandler : class, ICognitoCustomSmsSenderHandler
{
    protected override void EnrichInvocationActivity(Activity activity, CognitoCustomSmsSenderEvent input, ILambdaContext context) => CognitoTelemetry.Enrich(activity, input);
}

public interface ICognitoDefineAuthChallengeHandler : IRequestHandler<CognitoDefineAuthChallengeEvent, CognitoDefineAuthChallengeEvent>;
public abstract class CognitoDefineAuthChallengeFunction<THandler> : RequestFunction<CognitoDefineAuthChallengeEvent, CognitoDefineAuthChallengeEvent, THandler>
    where THandler : class, ICognitoDefineAuthChallengeHandler
{
    protected override void EnrichInvocationActivity(Activity activity, CognitoDefineAuthChallengeEvent input, ILambdaContext context) => CognitoTelemetry.Enrich(activity, input);
}

public interface ICognitoUserMigrationHandler : IRequestHandler<CognitoMigrateUserEvent, CognitoMigrateUserEvent>;
public abstract class CognitoUserMigrationFunction<THandler> : RequestFunction<CognitoMigrateUserEvent, CognitoMigrateUserEvent, THandler>
    where THandler : class, ICognitoUserMigrationHandler
{
    protected override void EnrichInvocationActivity(Activity activity, CognitoMigrateUserEvent input, ILambdaContext context) => CognitoTelemetry.Enrich(activity, input);
}

public interface ICognitoPostAuthenticationHandler : IRequestHandler<CognitoPostAuthenticationEvent, CognitoPostAuthenticationEvent>;
public abstract class CognitoPostAuthenticationFunction<THandler> : RequestFunction<CognitoPostAuthenticationEvent, CognitoPostAuthenticationEvent, THandler>
    where THandler : class, ICognitoPostAuthenticationHandler
{
    protected override void EnrichInvocationActivity(Activity activity, CognitoPostAuthenticationEvent input, ILambdaContext context) => CognitoTelemetry.Enrich(activity, input);
}

public interface ICognitoPostConfirmationHandler : IRequestHandler<CognitoPostConfirmationEvent, CognitoPostConfirmationEvent>;
public abstract class CognitoPostConfirmationFunction<THandler> : RequestFunction<CognitoPostConfirmationEvent, CognitoPostConfirmationEvent, THandler>
    where THandler : class, ICognitoPostConfirmationHandler
{
    protected override void EnrichInvocationActivity(Activity activity, CognitoPostConfirmationEvent input, ILambdaContext context) => CognitoTelemetry.Enrich(activity, input);
}

public interface ICognitoPreAuthenticationHandler : IRequestHandler<CognitoPreAuthenticationEvent, CognitoPreAuthenticationEvent>;
public abstract class CognitoPreAuthenticationFunction<THandler> : RequestFunction<CognitoPreAuthenticationEvent, CognitoPreAuthenticationEvent, THandler>
    where THandler : class, ICognitoPreAuthenticationHandler
{
    protected override void EnrichInvocationActivity(Activity activity, CognitoPreAuthenticationEvent input, ILambdaContext context) => CognitoTelemetry.Enrich(activity, input);
}

public interface ICognitoPreSignUpHandler : IRequestHandler<CognitoPreSignupEvent, CognitoPreSignupEvent>;
public abstract class CognitoPreSignUpFunction<THandler> : RequestFunction<CognitoPreSignupEvent, CognitoPreSignupEvent, THandler>
    where THandler : class, ICognitoPreSignUpHandler
{
    protected override void EnrichInvocationActivity(Activity activity, CognitoPreSignupEvent input, ILambdaContext context) => CognitoTelemetry.Enrich(activity, input);
}

public interface ICognitoPreTokenGenerationHandler : IRequestHandler<CognitoPreTokenGenerationEvent, CognitoPreTokenGenerationEvent>;
public abstract class CognitoPreTokenGenerationFunction<THandler> : RequestFunction<CognitoPreTokenGenerationEvent, CognitoPreTokenGenerationEvent, THandler>
    where THandler : class, ICognitoPreTokenGenerationHandler
{
    protected override void EnrichInvocationActivity(Activity activity, CognitoPreTokenGenerationEvent input, ILambdaContext context) => CognitoTelemetry.Enrich(activity, input);
}

public interface ICognitoPreTokenGenerationV2Handler : IRequestHandler<CognitoPreTokenGenerationV2Event, CognitoPreTokenGenerationV2Event>;
public abstract class CognitoPreTokenGenerationV2Function<THandler> : RequestFunction<CognitoPreTokenGenerationV2Event, CognitoPreTokenGenerationV2Event, THandler>
    where THandler : class, ICognitoPreTokenGenerationV2Handler
{
    protected override void EnrichInvocationActivity(Activity activity, CognitoPreTokenGenerationV2Event input, ILambdaContext context) => CognitoTelemetry.Enrich(activity, input);
}

public interface ICognitoVerifyAuthChallengeHandler : IRequestHandler<CognitoVerifyAuthChallengeEvent, CognitoVerifyAuthChallengeEvent>;
public abstract class CognitoVerifyAuthChallengeFunction<THandler> : RequestFunction<CognitoVerifyAuthChallengeEvent, CognitoVerifyAuthChallengeEvent, THandler>
    where THandler : class, ICognitoVerifyAuthChallengeHandler
{
    protected override void EnrichInvocationActivity(Activity activity, CognitoVerifyAuthChallengeEvent input, ILambdaContext context) => CognitoTelemetry.Enrich(activity, input);
}

internal static class CognitoTelemetry
{
    public static void Enrich<TRequest, TResponse>(
        Activity activity,
        CognitoTriggerEvent<TRequest, TResponse> input)
        where TRequest : CognitoTriggerRequest, new()
        where TResponse : CognitoTriggerResponse, new()
    {
        activity.SetTag("kralizek.aws.cognito.trigger_source", input.TriggerSource);
        activity.SetTag("kralizek.aws.cognito.user_pool_id", input.UserPoolId);
        activity.SetTag("kralizek.aws.cognito.user_name", input.UserName);
        activity.SetTag("cloud.region", input.Region);
    }
}
