# Security Policy

## Supported versions

Security fixes are provided for the latest stable major version and, when practical, the currently supported prerelease line leading to the next stable version.

Older major versions may no longer receive security updates.

## Reporting a vulnerability

Please do not report security vulnerabilities through public GitHub issues.

Use GitHub's private vulnerability reporting for this repository when available. If private vulnerability reporting is not available, contact the maintainer privately through the contact information on the maintainer's GitHub profile.

When reporting a vulnerability, include:

- affected package and version;
- a clear description of the vulnerability and impact;
- minimal reproduction steps or proof of concept;
- any relevant AWS Lambda event-source or deployment configuration;
- suggested mitigations, if known.

Do not include live AWS credentials, tokens, secret values, or other sensitive production data.

## Scope

Security reports may concern the runtime packages, project templates, dependency-injection/configuration behavior, serialization and payload decoding, generated deployment defaults, or build/release supply-chain behavior.

AWS service permissions, IAM policies, event-source mappings, and infrastructure configuration are generally application responsibilities, but issues where this library generates or documents unsafe defaults are in scope.