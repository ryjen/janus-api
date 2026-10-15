# Authentication observation contract (source-only)

Status: **opt-in, no telemetry configured**. This source seam is not a deployed
counter/metrics integration, monitoring capability, or security attestation.

## Trust boundary

The HTTP adapter resolves each incoming path through the existing
enum-backed `AuthRoutes` table and emits **one** `AuthObservation` after
determining the response status. The only observation fields are:

- `AuthRoute` (seven members, including `Unknown`)
- `AuthObservationKind` (six members: `Succeeded`, `Accepted`,
  `InvalidInput`, `DeniedOrChallenged`, `Unavailable`, `Failed`)

Maximum theoretical dimension combinations: **42**, with no variable labels.
A caller-supplied path is never recorded; unknown paths map to `Unknown`.

The classification is **coarse** and is not a fine-grained authentication event
stream. HTTP 401 covers both authentication rejection and challenge-required;
those cases are deliberately combined as `DeniedOrChallenged`. HTTP 200 on
signup includes both eligible requests and `UsernameExistsException`, as
required by the existing anti-enumeration policy. A recovery request HTTP 202
does **not** mean a code was delivered or that an account exists. A 503 is an
indeterminate failure, not proof of rollback. No automatic retry is authorized
from a metric event.

## Privacy and execution

- The default `NoOpAuthObservationSink` emits **nothing**.
- An external sink must be explicitly injected into the Lambda constructor;
  this change does not configure logging, CloudWatch custom metrics,
  telemetry services or user identification.
- Observation sink implementations must accept only the two enums and may
  count them; they must **not** add emails, subject IDs, credentials, codes,
  tokens, challenge sessions, raw provider exceptions, arbitrary dimensions,
  timestamps with per-user resolution, correlation identifiers, IP addresses
  or request/response payloads.
- The handler intentionally never reads HTTP response bodies to classify
  results; Cognito challenge sessions may legitimately be in those bodies.
- A sink failure is swallowed without logging the error, so optional metrics
  cannot change authentication responses. One sink invocation is attempted
  per handler invocation; delivery is not guaranteed.
- Backend counters require explicit retention, access controls, minimum
  aggregation thresholds and independent privacy review before activation.
  Low-count aggregates can still expose information if published carelessly.

## Gates

This does not change IAM, API routes, Cognito semantics, Lambda resource
templates or API v1 payloads. It is **not** evidence of live AWS verification.
Disposable Cognito/PostConfirmation → SQS → DynamoDB acceptance remains in
issue #9, and historical PR-ref/API-key cleanup remains in issue #7.
