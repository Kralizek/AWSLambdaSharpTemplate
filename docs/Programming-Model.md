# Programming Model

The runtime is built around three semantic function roots rather than AWS-service-specific entry points.

- `EventFunction<TInput, THandler>` handles one input and completes.
- `RequestFunction<TInput, TOutput, THandler>` handles one input and returns an application result.
- `RecordFunction<...>` is the foundation for event sources that deliver multiple records in one invocation.

Application behavior lives in handler classes resolved through dependency injection. Function classes stay small and primarily select the programming model and provide configuration hooks.

Source-specific packages layer AWS behavior on top of these roots. They own envelope mapping, source metadata, payload decoding where applicable, record-result semantics, and AWS-specific retry/failure responses.

Continue with [Event Functions](Event-Functions.md), [Request Functions](Request-Functions.md), or [Record Functions](Record-Functions.md).
