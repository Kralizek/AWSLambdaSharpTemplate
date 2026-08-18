# Migrating from v5 to v6

Version 6 is a programming-model redesign rather than a drop-in package update. Existing functions should be migrated deliberately.

The main conceptual changes are:

- Function classes select an Event, Request, or Record model while application logic lives in injected handlers.
- AWS-specific integrations use dedicated packages and source-specific handler/context contracts.
- Record handlers return source-specific `LambdaRecordResult` types instead of relying on implicit completion.
- Payload decoding is separated from record transport through string/binary decoder contracts.
- Framework contexts expose common Lambda metadata without requiring handlers to depend directly on `ILambdaContext`; the original AWS context remains available as an escape hatch.
- Record processing owns one DI scope per record, with parallel processing exposed only by integrations where it is appropriate.

For a migration, first identify the invocation semantics, select the matching v6 template/package, move application behavior into the new handler contract, and then restore any custom service registration, decoding, or source-specific failure behavior.

Because the redesign changes public base classes and handler contracts, migrate and test one Lambda integration at a time rather than attempting a mechanical namespace/package replacement.

## Performance

V6 also changes the execution model, so consumers should expect additional framework overhead compared with V5. Controlled benchmarks currently show the clearest difference in small or synchronously completed workloads, where framework work dominates the application work. The gap remains measurable when handlers genuinely suspend, but it becomes smaller in more representative nested processing where V6 also takes ownership of decoding, record processing, context propagation, failure translation, and other event plumbing that application code owns in V5.

That additional cost should be evaluated together with the semantics V6 provides: one independent DI scope per record, source-specific immutable contexts, raw/origin record access, partial-batch result handling, automatic exception translation, cancellation and disposal guarantees, telemetry seams, and reusable nested record processing.

The current controlled numbers are still provisional while repeated reference-machine runs are collected. See [the benchmark documentation](../benchmarks/README.md#controlled-v5-to-v6-reference) for the methodology, provisional measurements, allocation data, and the distinction between synchronous framework-overhead and genuinely asynchronous workloads.
