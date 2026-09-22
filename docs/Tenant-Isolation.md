# Tenant Isolation

AWS Lambda tenant isolation mode assigns execution environments to individual tenants. When a function is invoked with a tenant identifier, Lambda guarantees that an execution environment used for that tenant is not later reused for a different tenant.

KLT treats tenant identity as Lambda invocation metadata rather than as a separate programming model.

## Access the tenant identifier

All KLT function contexts inherit `FunctionContext.TenantId`:

```csharp
public sealed class OrderHandler : IEventHandler<OrderCreated>
{
    public ValueTask HandleAsync(
        OrderCreated input,
        EventContext context,
        CancellationToken cancellationToken)
    {
        var tenantId = context.TenantId;

        // ...
        return ValueTask.CompletedTask;
    }
}
```

For a tenant-isolated invocation, `TenantId` contains the identifier supplied to Lambda. For a standard invocation it is `null`.

The value is available consistently through `EventContext`, `RequestContext`, `RecordContext`, and source-specific contexts derived from them.

## Initialization and dependency injection

Tenant identity is invocation-phase metadata. It is not available while Lambda initializes a new execution environment.

This distinction matters for long-lived state:

- singleton services can become tenant-affine after the first invocation uses them;
- static state can become tenant-affine after invocation begins;
- `/tmp` and other execution-environment-local caches inherit Lambda's tenant isolation boundary;
- constructors and startup/configuration hooks must not assume that a tenant identifier is already available.

KLT does not change dependency-injection lifetimes for tenant-isolated functions. The normal execution-environment service provider and per-invocation/per-record scopes continue to apply.

A singleton may therefore cache data that is safe to reuse for the tenant assigned to that environment, but tenant-specific initialization must happen lazily from invocation code rather than during application startup.

## Telemetry

The normal KLT host enriches the Lambda invocation activity with:

```text
kralizek.aws.lambda.tenant_id
```

when tenant isolation is active.

OpenTelemetry does not currently define a standard AWS Lambda tenant attribute, so KLT uses its own AWS-specific attribute namespace instead of inventing a standard-looking `aws.*` semantic-convention key.

Tenant identifiers are intentionally not added to KLT framework metric tags. Tenant populations can be large or unbounded, which makes the identifier unsuitable as a default metric dimension.

Minimal Event and Request hosts keep their existing telemetry boundary: they expose `TenantId` through the function context but do not add KLT framework enrichment to the invocation activity.

## Infrastructure remains external

KLT does not provision Lambda tenant isolation. Enable `TenancyConfig` through the infrastructure system that owns the Lambda function.

Tenant isolation also affects how functions are invoked: the tenant identifier must be supplied through the Lambda invocation mechanism. Event source mappings do not automatically turn event payload tenant information into Lambda tenant-isolated invocations. See AWS guidance for the routing pattern when integrating event sources.

## AWS references

- https://docs.aws.amazon.com/lambda/latest/dg/tenant-isolation.html
- https://docs.aws.amazon.com/lambda/latest/dg/tenant-isolation-invoke.html
- https://docs.aws.amazon.com/lambda/latest/dg/tenant-isolation-context.html
