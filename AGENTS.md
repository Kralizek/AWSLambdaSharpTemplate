# Repository guide for coding agents

This repository provides a programming model and `dotnet new` templates for AWS Lambda functions.

## Architecture

The core programming model has three semantic roots:

- `EventFunction` for one-way event handling;
- `RequestFunction` for request/response handling;
- `RecordFunction` for envelopes containing independently processed records.

Keep source-specific AWS behavior in the integration packages rather than pushing it into the common abstractions unless the behavior is genuinely shared.

Consumer-facing handler contracts and contexts live in `Kralizek.Lambda.Template.Abstractions`. AWS runtime and dependency-injection plumbing belongs in `Kralizek.Lambda.Template` or the relevant integration package.

## Record processing rules

- Record handlers get independent DI scopes.
- Sequential processing is the default.
- SQS and SNS expose explicit bounded-parallel variants.
- DynamoDB Streams and Kinesis Streams intentionally do not add in-process parallelism. Consumers should use the Lambda event source mapping `ParallelizationFactor` instead so AWS remains responsible for stream concurrency and ordering.
- Cancellation aborts the invocation; do not translate invocation cancellation into a record failure.
- Use source-specific record result types derived from `LambdaRecordResult` rather than generic booleans or completion-only plumbing.
- Preserve the raw AWS record in the context property bag when a synthetic consumer-facing model is exposed.

## Source semantics matter

Before implementing or changing an AWS integration, verify:

- whether one invocation contains one event or a batch;
- partial-batch response support;
- retry and checkpoint behavior;
- ordering guarantees;
- event-source-mapping configuration owned by infrastructure;
- whether payload decoding belongs in the library or the Lambda runtime serializer.

Do not infer one integration's behavior from another just because the event envelopes look similar.

## Integration slice checklist

A public integration change normally needs coordinated updates to:

1. runtime package under `src/`;
2. tests under `tests/`;
3. sample under `samples/`;
4. project template under `templates/content/`;
5. template packaging metadata;
6. smoke-test matrix in `.github/workflows/ci.yml`;
7. root/package/template documentation.

Generated templates must remain neutral with respect to the consumer repository. In particular, do not package repository-local build controls such as the `templates/content/Directory.Packages.props` used to isolate template source projects from this repository's Central Package Management settings.

## Dependency management

NuGet package versions are centrally managed in `Directory.Packages.props` with transitive pinning enabled. Do not add inline package versions to normal repository projects.

Template source projects are an exception because the generated projects must be self-contained; the template pack step substitutes the library version token when packaging.

## Validation

Run:

```bash
dotnet restore
dotnet format --verify-no-changes --no-restore
dotnet build --configuration Release --no-restore --warnaserror
dotnet test --configuration Release --no-build
```

For template-affecting work, preserve the CI smoke-test contract: install the packed template, instantiate it outside the repository tree, restore against the locally packed `Kralizek.Lambda.*` packages, and build with warnings as errors.

## Scope discipline

Prefer small, source-specific changes over broad framework abstractions. Avoid compatibility shims for obsolete pre-V6 APIs unless an issue explicitly requires them. Update migration and changelog documentation when a public API or behavior changes.