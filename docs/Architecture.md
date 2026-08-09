# Architecture

The library separates three concerns:

1. The semantic invocation model: event, request/response, or records.
2. The AWS integration: mapping the AWS event source into that model.
3. Application behavior: a handler resolved through dependency injection.

This avoids a single universal Lambda base class with source-specific switches. Source packages remain responsible for the semantics that genuinely differ: envelope mapping, metadata, decoding, ordering, retry behavior, and partial failure responses.

The common runtime provides configuration, dependency injection, logging, cancellation, Lambda context mapping, scopes, and handler dispatch. The abstractions package contains source-neutral handler, context, and decoder contracts without depending on `Amazon.Lambda.Core`.

Original AWS context and record objects are retained as explicit escape hatches rather than leaking into every application handler contract.
