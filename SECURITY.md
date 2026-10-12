# Security policy

Janus is a **reference prototype, not a supported hosted security service**. It should not be used to protect real customer accounts or sensitive records without independent review and integration validation.

Please use GitHub's **private vulnerability reporting** for security findings once that feature is enabled, rather than publishing credential material in a public issue.

The current auth model requires a Cognito authorizer for identity-scoped endpoints and a separate API Gateway API key for traffic access; an API key is not authorization. See [architecture and known limits](docs/architecture.md).

Do not report exploitable details, tokens or real user data in public issues, screenshots or logs.
