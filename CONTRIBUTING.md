# Contributing

Contributions are welcome. Thanks for taking the time to improve the project.

## Prerequisites

- .NET SDK version specified by `global.json`
- A C# IDE or editor
- Docker or an AWS account only when a change needs integration-level validation

## Build and test

```bash
dotnet restore
dotnet format --verify-no-changes --no-restore
dotnet build --configuration Release --no-restore --warnaserror
dotnet test --configuration Release --no-build
```

The repository treats warnings as errors. Please keep the build warning-free.

## Project templates

Changes affecting a public integration normally need to keep these pieces aligned:

- runtime package under `src/`;
- tests under `tests/`;
- sample under `samples/`;
- project template under `templates/content/`;
- template smoke-test coverage in `.github/workflows/ci.yml`;
- package, template, and root documentation where relevant.

Use the existing integration slices as the reference shape rather than adding source-specific behavior to the common abstractions without a clear cross-source need.

## Pull request expectations

- Open an issue first for non-trivial changes so the design and AWS semantics can be discussed before implementation.
- Keep each PR focused on one logical change.
- Add or update tests for changed behavior.
- Update documentation when public behavior, APIs, templates, or operational requirements change.
- Preserve source-specific AWS semantics such as batching, retries, ordering, partial-batch responses, and event-source-mapping responsibilities.
- Ensure CI is green before merge.

## Code style

The repository includes an `.editorconfig`; use `dotnet format --verify-no-changes --no-restore` before submitting changes.

## Automation and coding agents

Automated coding agents should also read [`AGENTS.md`](AGENTS.md) for repository-specific architecture and workflow guidance.

## Reporting issues

Use the structured issue forms under `.github/ISSUE_TEMPLATE/` for bugs and feature requests. Do not include AWS credentials, tokens, secret values, or other sensitive data in public issues.