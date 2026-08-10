# Changelog

All notable changes to this project will be documented in this file.

## [6.0.0] - Unreleased

Version 6 is a major redesign of the library and template set. It replaces the V5 programming model with semantic function types, expands the supported AWS event sources, and moves the project to .NET 10.

### Breaking changes

- Target framework moved from .NET 6 to .NET 10.
- Replaced the previous `Function` and `RequestResponseFunction` programming model with semantic roots:
  - `EventFunction` for one-way events.
  - `RequestFunction` for request/response invocations.
  - `RecordFunction` for batched record sources.
- Reworked handler contracts around strongly typed function contexts and `CancellationToken` propagation.
- Primary handlers are now resolved through dependency injection and run in framework-managed scopes.
- Reworked SQS and SNS APIs around typed/raw record handlers and source-neutral payload decoders rather than the V5 serializer-specific extension points.
- Record-oriented handlers now return source-specific result types derived from `LambdaRecordResult` instead of relying on completion-only handler contracts.
- Replaced the previous generic template variants with the new semantic and source-specific template catalog.

### Added

- Added the lightweight `Kralizek.Lambda.Template.Abstractions` package containing handler contracts, function contexts, record-result abstractions, and payload decoder interfaces without dependencies on the Lambda runtime or Microsoft.Extensions infrastructure.
- Added strongly typed `EventContext`, `RequestContext`, and `RecordContext` models, while retaining access to the original `ILambdaContext` through the runtime package when needed.
- Added built-in plain-text and System.Text.Json string/binary payload decoders, including support for source-generated JSON metadata.
- Added SQS support for both decoded messages and raw records, including bounded parallel variants and partial-batch failure responses.
- Added SNS support for both decoded notifications and raw records, including bounded parallel variants and SNS whole-invocation failure semantics.
- Added strongly typed Amazon Cognito user-pool trigger specializations and templates, including separate Pre Token Generation V1 and V2 support.
- Added Amazon EventBridge support with strongly typed event detail and access to the complete AWS event envelope.
- Added DynamoDB Streams support with sequential per-record processing, stream-specific contexts and results, and `StreamsEventResponse` partial-batch failures.
- Added Amazon S3 event notification support with synthetic object/event models and access to the raw AWS S3 record.
- Added S3 Batch Operations schema 2.0 support with typed task models and explicit success, temporary-failure, and permanent-failure results.
- Added Kinesis Streams support for raw records and decoded binary payloads, stream-specific result types, and partial-batch failure responses.
- Added source-specific samples and `dotnet new` templates for the supported integrations.
- Added template smoke tests that install the packed template package, instantiate every template, restore its runtime dependencies from the locally packed packages, and build the generated project as an external consumer.

### Changed

- Lambda-compatible logging is configured by the framework so injected `ILogger<T>` works without template-specific bootstrap code.
- Configuration, logging, and service-registration hooks remain available for consumer customization while framework-owned registrations are kept internal.
- Record processing uses one invocation scope plus independently disposed per-record scopes.
- SQS and SNS keep explicit bounded-parallel function variants; DynamoDB Streams and Kinesis Streams deliberately process records sequentially inside the invocation and leave concurrency to the Lambda event source mapping.
- DynamoDB Streams expose AWS `AttributeValue` images directly rather than treating stream images as ordinary JSON.
- Runtime and template packages now share the same MinVer-derived version so generated templates reference the exact matching runtime package version.

### Repository and release infrastructure

- Replaced the previous AppVeyor/Cake/GitVersion setup with GitHub Actions, standard `dotnet` commands, and MinVer.
- Added reproducible builds, symbol packages, warnings-as-errors validation, formatting validation, and NuGet Trusted Publishing for releases.
- Added manual alpha publishing to GitHub Packages and release-driven publishing to NuGet.org.
- Added NuGet Central Package Management with transitive pinning.
- Added Dependabot updates for NuGet packages and GitHub Actions.
- Removed obsolete V5-era SQS/SNS sample projects.

## [5.0.0] - 2022-10-31

Last major release before the V6 programming-model redesign.
