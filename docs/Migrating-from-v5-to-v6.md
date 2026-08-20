# Migrating from v5 to v6

Version 6 is a programming-model redesign rather than a drop-in package update. Existing functions should be migrated deliberately.

The main conceptual changes are:

- Function classes select an Event, Request, or Record model while application logic lives in injected handlers.
- AWS-specific integrations use dedicated packages and source-specific handler/context contracts.
- Record handlers return source-specific `LambdaRecordResult` types instead of relying on implicit completion.
- Payload decoding is separated from record transport through string/binary decoder contracts.
- Framework contexts expose common Lambda metadata without requiring handlers to depend directly on `ILambdaContext`; the original AWS context remains available as an escape hatch.
- Record processing owns one DI scope per record, with parallel processing exposed only by integrations where it is appropriate.

V6 also separates the request/event handler programming model from the amount of hosting infrastructure used to invoke it. `EventFunction` and `RequestFunction` remain the default/full hosts. `MinimalEventFunction` and `MinimalRequestFunction` invoke the same v6 handlers through a leaner path that retains configuration, logging, function-local dependency injection, one invocation scope, the v6 context, cancellation, and asynchronous scope disposal.

This can be particularly relevant when migrating a simple v5 Event or Request/Response function. V5 already provided a function-local service provider and one DI scope per invocation with comparatively little runtime overhead. If the migrated function does not need v6 record processing, source-specific orchestration, or internal framework telemetry, consider the Minimal host after moving the application logic to the v6 handler contract.

For example, once a request handler has been migrated to v6, choosing the lean host is only a hosting change:

```csharp
public class Function : RequestFunction<string, string, MyHandler>;
```

becomes:

```csharp
public class Function : MinimalRequestFunction<string, string, MyHandler>;
```

The handler remains unchanged. See [Minimal Hosting](Minimal-Hosting.md) for the exact capability boundary.

For a migration, first identify the invocation semantics, select the matching v6 template/package, move application behavior into the new handler contract, and then restore any custom service registration, decoding, or source-specific failure behavior. Choose Minimal only after determining that the function does not rely on behavior deliberately owned by the full/source-specific host.

Because the redesign changes public base classes and handler contracts, migrate and test one Lambda integration at a time rather than attempting a mechanical namespace/package replacement.

## Performance

V6 also changes the execution model, so consumers should expect measurable runtime and allocation overhead compared with V5. The magnitude depends heavily on workload shape: synchronous microbenchmarks expose the framework floor, while genuinely asynchronous workloads give a more relevant view of async pipelines. V6 remains measurably more expensive in both, but exact ratios should not be generalized to every function.

That additional cost should be evaluated together with the semantics V6 provides: one independent DI scope per record, source-specific immutable contexts, raw/origin record access, partial-batch result handling, automatic exception translation, cancellation and disposal guarantees, telemetry seams, and reusable nested record processing.

For source-neutral request/event functions that do not need the richer processing path, Minimal hosting is the explicit v6 option for retaining the same handler/context/DI model while reducing framework involvement. It should be evaluated with the same workload-specific discipline rather than treated as raw-SDK compatibility.

Consumers with strict latency or allocation requirements should benchmark their own workload. See [the benchmark documentation](../benchmarks/README.md#controlled-v5-to-v6-reference) for the controlled-hardware methodology, repeated-run values, allocations, stability spread, and the distinction between synchronous framework-overhead and genuinely asynchronous workloads.
