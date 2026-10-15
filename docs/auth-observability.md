# Authentication observation contract (source-only)

Status: **opt-in, in-memory counters only; no external telemetry configured**.
This is not a deployed metrics integration, monitoring service, or security attestation.

## Trust boundary

The HTTP adapter resolves each incoming path through the existing
enum-backed `AuthRoutes` table and, only when a counter is injected, increments **one**
`AuthObservation` category after determining the response status. The only observation fields are:

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

- The default Lambda composition injects **no counter** and performs no
  telemetry. The only opt-in type is sealed AuthObservationCounter, backed
  by a **fixed-size** array of 42 atomic counters and one aggregate total.
- Record executes only trusted enum range checks and Interlocked.Increment.
  The handler cannot invoke arbitrary callbacks, user-provided sink code,
  network I/O, logging, locks or background tasks on the response path.
- No arbitrary IAuthObservationSink extension point is provided. It was
  removed after security review found that a synchronous sink could block
  the response indefinitely, even when exceptions were caught.
- Counters are best-effort **process-local state**, reset when the Lambda
  execution environment is recycled; no persistence, export, retention or
  operational alerting is implemented or guaranteed.
- Invalid/out-of-range enum values are ignored. All permitted inputs have
  fixed cardinality; no caller-controlled dimensions, emails, subject IDs,
  passwords, confirmation codes, ID tokens, challenge sessions, raw exception
  messages, request/response payloads, IPs or correlation IDs are collected.
- The handler never inspects HTTP response bodies to classify outcomes;
  those bodies can legitimately carry a Cognito session or authentication token.
- Adding remote metrics/structured logging requires a **separate** bounded,
  asynchronous-delivery/privacy/retention/security design and review.
  Do not inject a synchronous network sink or assume the current in-memory
  counter provides deployable monitoring. Low-count aggregates can still
  expose information if published carelessly.

## Gates

This does not change IAM, API routes, Cognito semantics, Lambda resource
templates or API v1 payloads. It is **not** evidence of live AWS verification.
Disposable Cognito/PostConfirmation → SQS → DynamoDB acceptance remains in
issue #9, and historical PR-ref/API-key cleanup remains in issue #7.
