# Architecture

The library separates three concerns:

1. The semantic invocation model: event, request/response, or records.
2. The AWS integration: mapping the AWS event source into that model.
3. Application behavior: a handler resolved through dependency injection.

This avoids a single universal Lambda base class with source-specific switches. Source packages remain responsible for the semantics that genuinely differ: envelope mapping, metadata, decoding, ordering, retry behavior, and partial failure responses.

The common runtime provides configuration, dependency injection, logging, cancellation, Lambda context mapping, scopes, and handler dispatch. The abstractions package contains source-neutral handler, context, decoder, and record-processing contracts without depending on `Amazon.Lambda.Core`.

For record-oriented integrations, `RecordFunction<...>` owns the AWS invocation pipeline: envelope extraction, record scheduling, source-specific failure translation, result collection, and response generation. Execution of one record is delegated to `IRecordProcessor<TRecord, TRecordResult, TContext>`, which creates an independent record scope, resolves the registered record handler in that scope, invokes it, and disposes the scope.

That split makes the framework's per-record lifetime semantics reusable when an application needs to compose nested record envelopes, without turning the processor into a second function pipeline. Scheduling, acknowledgement, checkpointing, and retry semantics remain owned by the outer source-specific function.

Original AWS context and record objects are retained as explicit escape hatches rather than leaking into every application handler contract.
