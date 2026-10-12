# Security policy

Janus is a **reference prototype, not a supported hosted security service**. It should not be used to protect real customer accounts or sensitive records without independent review and integration validation.

Please use GitHub's **private vulnerability reporting** for security findings once that feature is enabled, rather than publishing credential material in a public issue.

The current auth model requires a Cognito authorizer for identity-scoped endpoints and a separate API Gateway API key for traffic access; an API key is not authorization. See [architecture and known limits](docs/architecture.md).

Do not report exploitable details, tokens or real user data in public issues, screenshots or logs.

## Known publication blocker

Self-service signup currently relies on an administrative Cognito enrollment path that has not verified the requested email address before queuing account creation. See [issue #9](https://github.com/ryjen/janus-api/issues/9). Keep the public registration and cloud deployment gate closed pending an identity-flow redesign and negative-path integration tests.
