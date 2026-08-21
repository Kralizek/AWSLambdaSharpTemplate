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

V6 has two different performance profiles because it has two different hosting choices for source-neutral Request/Event functions.

The **default/full host** provides the complete V6 Request/Event invocation infrastructure, including the framework's internal invocation telemetry path. The **Minimal host** runs the same V6 handler and context contracts through a shorter path when that infrastructure is not needed. Source-specific Record functions continue to use the full source-specific hosts because their record-processing semantics are part of the model rather than optional hosting decoration.

The beta-5 release benchmark snapshot is the most useful current comparison. On its GitHub-hosted runner, the Request benchmark measured:

| Model | Mean | Allocated |
| --- | ---: | ---: |
| Raw AWS SDK | 32.2 ns | 128 B |
| V5 | 196.7 ns | 352 B |
| V6 Minimal | 299.6 ns | 584 B |
| V6 full | 443.0 ns | 712 B |

The Request workload intentionally does almost no application work, so it magnifies framework overhead. In that run, Minimal reduced the V6 hosting cost substantially: it was about 32% faster than the full V6 host and allocated 128 B less per invocation. Compared with V5, Minimal still had a measurable floor: roughly 103 ns and 232 B per invocation on this synthetic workload.

A nested SQS -> SNS -> S3 workload gives a different view because event-processing work dominates more of the invocation. The beta-5 snapshot measured:

| Model | Mean | Allocated |
| --- | ---: | ---: |
| Raw AWS SDK | 5.60 us | 4,168 B |
| V5 | 6.01 us | 4,280 B |
| V6 Minimal comparison | 5.88 us | 4,624 B |
| V6 full/source-specific | 7.93 us | 6,088 B |

The Minimal contender in record-oriented benchmark suites is a comparison fixture rather than a `MinimalSqsFunction` or other source-specific Minimal host. It deliberately keeps the Minimal Request/Event hosting model while moving AWS-specific iteration, decoding, failure translation, and nested envelope processing back into application code. This makes the benchmark useful for showing the boundary between lean V6 hosting and framework-owned AWS orchestration, but it is not a second source-specific hosting API.

The beta-5 release also includes the root `FunctionContext` allocation optimization. Across independent benchmark paths it removed a fixed 520 B per invocation from both Minimal and full V6 hosts, which is a strong allocation-level confirmation that the optimization removed root-context bookkeeping rather than shifting work elsewhere.

These measurements should be interpreted as architectural comparisons rather than universal throughput ratios. GitHub-hosted runners are useful for release-to-release trends but are not controlled benchmark hardware, and very small timing measurements are especially sensitive to run-to-run variation. Allocation differences are much more stable. For latency- or allocation-sensitive functions, benchmark the actual workload.

The practical migration guidance is therefore:

- choose the full V6 host when the function benefits from the complete V6 invocation infrastructure or source-specific Record semantics;
- choose Minimal for source-neutral Request/Event functions that want the V6 handler/context/DI programming model with less hosting overhead;
- do not treat Minimal as a compatibility mode or as evidence that the full V6 model is incorrect;
- expect the relative overhead to shrink as real application work becomes more significant than the framework floor.

See [the benchmark documentation](../benchmarks/README.md) for methodology, historical controlled measurements, release benchmark history, and the broader benchmark matrix.
