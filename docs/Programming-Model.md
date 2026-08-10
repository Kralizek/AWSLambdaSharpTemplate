# Programming Model

The runtime is built around three semantic function roots rather than AWS-service-specific entry points.

- `EventFunction<TInput, THandler>` handles one input and completes.
- `RequestFunction<TInput, TOutput, THandler>` handles one input and returns an application result.
- `RecordFunction<...>` is the foundation for event sources that deliver multiple records in one invocation.

Application behavior lives in handler classes resolved through dependency injection. Function classes stay small and primarily select the programming model and provide configuration hooks.

Source-specific packages layer AWS behavior on top of these roots. They own envelope mapping, source metadata, payload decoding where applicable, record-result semantics, and AWS-specific retry/failure responses.

Record processing has one additional reusable primitive: `IRecordProcessor<TRecord, TRecordResult, TContext>`. A record processor executes exactly one record using the same per-record dependency-injection lifetime semantics used by `RecordFunction`: it creates an independent record scope, resolves the configured record handler, invokes it, and disposes the scope.

Normal applications do not need to use a record processor directly. It exists for advanced composition scenarios where one delivered record contains another record-oriented envelope and the inner records should retain the framework's normal scope semantics. It does not own envelope iteration, parallelism, retry/checkpoint behavior, or the final Lambda response; those remain responsibilities of the source-specific function pipeline.

Continue with [Event Functions](Event-Functions.md), [Request Functions](Request-Functions.md), or [Record Functions](Record-Functions.md).
