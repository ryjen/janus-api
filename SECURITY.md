# Security policy

Playstate is a **reference prototype, not a supported hosted security service**. It should not be used to protect real customer accounts or sensitive records without independent review and integration validation.

Please use GitHub's **private vulnerability reporting** for security findings once that feature is enabled, rather than publishing credential material in a public issue.

The current auth model requires a Cognito authorizer for identity-scoped endpoints and a separate API Gateway API key for traffic access; an API key is not authorization. See [architecture and known limits](docs/architecture.md).

Do not report exploitable details, tokens or real user data in public issues, screenshots or logs.

## Public history caution

This repository's visibility was changed to **public** before the historical-credential and clean-history reviews were completed. The publisher must independently confirm that any historical credentials are revoked or rotated and review all exposed refs/artifacts. See [publication remediation #7](https://github.com/ryjen/playstate/issues/7). Do not post token values or evidence containing secrets publicly.

## Known publication blocker

The draft now uses Cognito email-code confirmation and a trusted PostConfirmation Lambda before account provisioning. This is a **source-level fix, not verified security assurance**: no deployed end-to-end identity, retry, and abuse-resilience tests have passed. See [issue #9](https://github.com/ryjen/playstate/issues/9). Keep the public registration and deployment gate closed until negative-path integration tests and independent review pass.
