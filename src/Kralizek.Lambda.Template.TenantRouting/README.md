# Kralizek.Lambda.Template.TenantRouting

Tenant-aware synchronous invocation routing for AWS Lambda tenant isolation.

The package is source-agnostic. Applications compose it with the KLT integration that receives the original event, such as SQS, and decide how to obtain and validate the tenant identifier.

```csharp
services.AddTenantLambdaRouting();
```

Then route a payload to a tenant-isolated Lambda:

```csharp
await router.RouteAsync(
    TenantLambdaRoute.Utf8(
        tenantId,
        "orders-processor",
        message.Body),
    cancellationToken);
```

Routing uses synchronous `RequestResponse` invocation. A downstream Lambda function error is surfaced as `TenantLambdaInvocationException`, allowing the source-specific KLT integration to retain ownership of retry, acknowledgement, and partial-batch semantics.

The routing span uses the OpenTelemetry FaaS outgoing-invocation attributes `faas.invoked_name` and `faas.invoked_provider`, plus `kralizek.aws.lambda.tenant_id`.

See [Tenant Isolation](https://github.com/Kralizek/AWSLambdaSharpTemplate/blob/HEAD/docs/Tenant-Isolation.md) for the complete model.
