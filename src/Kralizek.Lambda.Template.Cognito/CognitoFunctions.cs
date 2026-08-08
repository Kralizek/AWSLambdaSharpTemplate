using Amazon.Lambda.CognitoEvents;

namespace Kralizek.Lambda;

public interface ICognitoCreateAuthChallengeHandler : IRequestHandler<CognitoCreateAuthChallengeEvent, CognitoCreateAuthChallengeEvent>;
public abstract class CognitoCreateAuthChallengeFunction<THandler> : RequestFunction<CognitoCreateAuthChallengeEvent, CognitoCreateAuthChallengeEvent, THandler>
    where THandler : class, ICognitoCreateAuthChallengeHandler;

public interface ICognitoCustomEmailSenderHandler : IRequestHandler<CognitoCustomEmailSenderEvent, CognitoCustomEmailSenderEvent>;
public abstract class CognitoCustomEmailSenderFunction<THandler> : RequestFunction<CognitoCustomEmailSenderEvent, CognitoCustomEmailSenderEvent, THandler>
    where THandler : class, ICognitoCustomEmailSenderHandler;

public interface ICognitoCustomMessageHandler : IRequestHandler<CognitoCustomMessageEvent, CognitoCustomMessageEvent>;
public abstract class CognitoCustomMessageFunction<THandler> : RequestFunction<CognitoCustomMessageEvent, CognitoCustomMessageEvent, THandler>
    where THandler : class, ICognitoCustomMessageHandler;

public interface ICognitoCustomSmsSenderHandler : IRequestHandler<CognitoCustomSmsSenderEvent, CognitoCustomSmsSenderEvent>;
public abstract class CognitoCustomSmsSenderFunction<THandler> : RequestFunction<CognitoCustomSmsSenderEvent, CognitoCustomSmsSenderEvent, THandler>
    where THandler : class, ICognitoCustomSmsSenderHandler;

public interface ICognitoDefineAuthChallengeHandler : IRequestHandler<CognitoDefineAuthChallengeEvent, CognitoDefineAuthChallengeEvent>;
public abstract class CognitoDefineAuthChallengeFunction<THandler> : RequestFunction<CognitoDefineAuthChallengeEvent, CognitoDefineAuthChallengeEvent, THandler>
    where THandler : class, ICognitoDefineAuthChallengeHandler;

public interface ICognitoUserMigrationHandler : IRequestHandler<CognitoMigrateUserEvent, CognitoMigrateUserEvent>;
public abstract class CognitoUserMigrationFunction<THandler> : RequestFunction<CognitoMigrateUserEvent, CognitoMigrateUserEvent, THandler>
    where THandler : class, ICognitoUserMigrationHandler;

public interface ICognitoPostAuthenticationHandler : IRequestHandler<CognitoPostAuthenticationEvent, CognitoPostAuthenticationEvent>;
public abstract class CognitoPostAuthenticationFunction<THandler> : RequestFunction<CognitoPostAuthenticationEvent, CognitoPostAuthenticationEvent, THandler>
    where THandler : class, ICognitoPostAuthenticationHandler;

public interface ICognitoPostConfirmationHandler : IRequestHandler<CognitoPostConfirmationEvent, CognitoPostConfirmationEvent>;
public abstract class CognitoPostConfirmationFunction<THandler> : RequestFunction<CognitoPostConfirmationEvent, CognitoPostConfirmationEvent, THandler>
    where THandler : class, ICognitoPostConfirmationHandler;

public interface ICognitoPreAuthenticationHandler : IRequestHandler<CognitoPreAuthenticationEvent, CognitoPreAuthenticationEvent>;
public abstract class CognitoPreAuthenticationFunction<THandler> : RequestFunction<CognitoPreAuthenticationEvent, CognitoPreAuthenticationEvent, THandler>
    where THandler : class, ICognitoPreAuthenticationHandler;

public interface ICognitoPreSignUpHandler : IRequestHandler<CognitoPreSignupEvent, CognitoPreSignupEvent>;
public abstract class CognitoPreSignUpFunction<THandler> : RequestFunction<CognitoPreSignupEvent, CognitoPreSignupEvent, THandler>
    where THandler : class, ICognitoPreSignUpHandler;

public interface ICognitoPreTokenGenerationHandler : IRequestHandler<CognitoPreTokenGenerationEvent, CognitoPreTokenGenerationEvent>;
public abstract class CognitoPreTokenGenerationFunction<THandler> : RequestFunction<CognitoPreTokenGenerationEvent, CognitoPreTokenGenerationEvent, THandler>
    where THandler : class, ICognitoPreTokenGenerationHandler;

public interface ICognitoPreTokenGenerationV2Handler : IRequestHandler<CognitoPreTokenGenerationV2Event, CognitoPreTokenGenerationV2Event>;
public abstract class CognitoPreTokenGenerationV2Function<THandler> : RequestFunction<CognitoPreTokenGenerationV2Event, CognitoPreTokenGenerationV2Event, THandler>
    where THandler : class, ICognitoPreTokenGenerationV2Handler;

public interface ICognitoVerifyAuthChallengeHandler : IRequestHandler<CognitoVerifyAuthChallengeEvent, CognitoVerifyAuthChallengeEvent>;
public abstract class CognitoVerifyAuthChallengeFunction<THandler> : RequestFunction<CognitoVerifyAuthChallengeEvent, CognitoVerifyAuthChallengeEvent, THandler>
    where THandler : class, ICognitoVerifyAuthChallengeHandler;
