# Kralizek.Lambda.Template

Core runtime for the source-neutral Event, Request, and Record programming models.

```csharp
public sealed class Function : EventFunction<MyEvent, MyEventHandler>;
```

Source-neutral Event and Request handlers can also use the Minimal host when they need the same v6 handler/context/DI model without the full invocation processing path:

```csharp
public sealed class Function : MinimalEventFunction<MyEvent, MyEventHandler>;
```

Minimal hosting retains configuration, logging, function-local dependency injection, one invocation scope, context, cancellation, and async scope disposal. See [Minimal Hosting](https://github.com/Kralizek/AWSLambdaSharpTemplate/blob/HEAD/docs/Minimal-Hosting.md) for the capability boundary and OpenTelemetry behavior.

For usage, processing semantics, and examples, see [Programming Model](https://github.com/Kralizek/AWSLambdaSharpTemplate/blob/HEAD/docs/Programming-Model.md).

The complete library documentation is available in the [`docs/` directory](https://github.com/Kralizek/AWSLambdaSharpTemplate/blob/HEAD/docs/README.md).
