# Migrating from v5 to v6

The concrete v5-to-v6 upgrade guide now lives in [`docs/Migrating-from-v5-to-v6.md`](docs/Migrating-from-v5-to-v6.md).

It includes itemized before/after migrations for:

- Request/response functions;
- Event functions;
- SNS functions;
- SQS functions;
- configuration, logging, and dependency-injection hooks;
- `ILambdaContext` replacement;
- custom SNS/SQS payload serialization;
- parallel record processing;
- SQS partial-batch responses;
- v6 full vs Minimal hosting.

See [`CHANGELOG.md`](CHANGELOG.md) for the complete v6 change summary.