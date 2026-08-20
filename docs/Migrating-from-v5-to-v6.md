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

This can be particularly relevant when migrating a simple v5 Event or Request/Response function. V5 already provided a function-local service provider and one DI scope per invocation with comparatively little runtime overhead. If the migrated function does not need the full Request/Event host's internal invocation telemetry or any source-specific record-processing behavior, consider the Minimal host after moving the application logic to the v6 handler contract.

For example, once a request handler has been migrated to v6, choosing the lean host is only a hosting change:

```csharp
public class Function : RequestFunction<string, string, MyHandler>;
```

becomes:

```csharp
public class Function : MinimalRequestFunction<string, string, MyHandler>;
```

The handler remains unchanged. See [Minimal Hosting](Minimal-Hosting.md) for the exact capability boundary.

For a migration, first identify the invocation semantics, select the matching v6 template/package, move application behavior into the new handler contract, and then restore any custom service registration, decoding, or source-specific failure behavior. Choose Minimal only after determining that the function does not rely on behavior deliberately owned by the full Request/Event host or by a source-specific Record host.

Because the redesign changes public base classes and handler contracts, migrate and test one Lambda integration at a time rather than attempting a mechanical namespace/package replacement.

## Performance

The controlled reference published for beta 4 measured V5 against the **default/full V6 hosting model**. It established that full V6 has measurable runtime and allocation overhead compared with V5. The magnitude depends heavily on workload shape: synchronous microbenchmarks expose the framework floor, while genuinely asynchronous workloads give a more relevant view of async pipelines. Exact ratios should not be generalized to every function.

That beta-4 comparison should be read together with the semantics the measured V6 paths provide. For record-oriented integrations those include one independent DI scope per record, source-specific immutable contexts, raw/origin record access, partial-batch result handling, automatic exception translation, cancellation and disposal guarantees, telemetry seams, and reusable nested record processing. The additional work is therefore not simply implementation waste, and not every additional allocation is a defect.

Minimal hosting does not invalidate that reference or replace the default host. It adds a second hosting choice for source-neutral Request/Event functions. When a function does not need the full host's internal invocation telemetry or source-specific Record processing, Minimal retains the same v6 handler/context/DI model while deliberately reducing framework involvement. It should be evaluated as a smaller capability set, not as a compatibility mode or a claim that the full V6 model was incorrect.

The original beta-4 figures remain the controlled V5-to-full-V6 baseline. New measurements that include Minimal should be treated as an additional comparison showing how much of the Request/Event overhead belongs to the full hosting path versus the v6 handler/context model retained by Minimal.

Consumers with strict latency or allocation requirements should benchmark their own workload. See [the benchmark documentation](../benchmarks/README.md#controlled-v5-to-v6-reference) for the controlled-hardware methodology, repeated-run values, allocations, stability spread, and the distinction between synchronous framework-overhead and genuinely asynchronous workloads.
