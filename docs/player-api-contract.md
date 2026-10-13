# Player record API contract — compatibility and modernization plan

Status: **design proposal; not a deployed or implemented v2 contract**. Tracks [#12](https://github.com/ryjen/playstate/issues/12). The existing API is experimental and must not be represented as production-ready.

## Current compatibility baseline (v1)

- `/player` supports GET, POST, PUT and DELETE, guarded by an API Gateway Cognito authorizer and checked again against the verified subject in the Lambda.
- A supplied record `id` must match the authenticated `sub`; absent `id` selects the subject's record. Never derive the table name or effective principal from an untrusted request.
- The current GET returns the stored `id`/`data` document shape.
- POST and PUT currently use the **same upsert** operation. This is historical behavior, **not** a promise that POST means create-only.
- The accepted `data` shape is not yet versioned, typed, or bounded; the historical handler accepted arbitrary JSON maps. Avoid publishing a restrictive schema as though implemented.
- Malformed JSON is a client error; infrastructure failures must not be reported as invalid client input.
- No versioned optimistic-concurrency guarantee is currently established. Concurrent devices can overwrite state.

## Proposed v2 contract — requires approval and implementation

Keep current routes and response bodies intact until consumers are inventoried and a versioned migration path is accepted.

| Operation | Proposed semantics | Errors to contract-test |
| --- | --- | --- |
| GET | Read only the verified subject's player state | 401/403 unauthorized, 404 missing, 503 dependency unavailable |
| POST | Create only if absent, using a conditional write | 400 invalid, 409 existing, 413 too large, 503 unavailable |
| PUT | Replace an existing record with a supplied version precondition | 400 invalid, 404 missing, 409 stale precondition, 413 too large, 503 unavailable |
| DELETE | Delete own record using a version precondition | 404 missing, 409 stale precondition, 503 unavailable |

These statuses are **proposed** and must not be mistaken for current wire behavior. Define conditional DynamoDB operations so read-then-write races cannot bypass conflicts. Choose a strong ETag or explicit integer revision; require `If-Match` (or an equivalent version field) for mutation and document exactly how first writes obtain revisions.

## Schema and boundary decisions to resolve

1. Inventory actual player fields and SDK consumers; define a canonical DTO with field-level types, required/optional/null behavior, limits and allowed unknown fields. Do not invent fields before that inventory.
2. Set explicit maximum decompressed request body bytes, JSON nesting depth, string lengths and collection sizes. Reject excess size **before** allocating or deserializing unbounded objects.
3. Specify Unicode normalization rules, duplicate JSON property handling, number formats and how malformed encodings fail.
4. Keep ownership validation independent from DTO validation and storage; reject contradictory `id` even if the rest of the body is valid.
5. Decide if absent records distinguish 404 from empty state, and whether DELETE is idempotent from the client's perspective.
6. Bound error bodies; never echo tokens, PII, AWS exception messages or the submitted player document.
7. Specify schema version and migration/rollback policy; consider an additive `/v2/player` path rather than switching live `/player` semantics in place.

## Qualification matrix

- **Unit/property tests:** null/blank/wrong-type/contradictory IDs; malformed/duplicate JSON; Unicode, deep nesting, huge objects, oversized strings, unknown fields, overflow numbers; wrong HTTP methods; auth checked before I/O.
- **Persistence integration:** create race (exactly one winner), stale-version PUT/DELETE, absent record, retry/idempotency, concurrent device writers, DynamoDB conditional failure translation.
- **HTTP/AWS integration:** API Gateway body mapping, actual Cognito JWT validation, unauthorized and cross-subject requests, gateway payload limits, Lambda timeout, DynamoDB throttling.
- **Operational tests:** no sensitive payloads in telemetry; bounded request costs, abuse throttling, structured correlation IDs, migration and rollback behavior.

## Delivery sequence

1. Freeze v1 behavior through deterministic adapter tests.
2. Extract a typed validation service and explicit command/result types behind the existing Lambda adapter, without changing wire semantics.
3. Add condition-aware persistence ports and fake-store concurrency tests.
4. Prototype versioned v2 contract and SDK migration with disposable AWS resources.
5. Require exact-head tests, SAM packaging, independent review, and the existing [publication](https://github.com/ryjen/playstate/issues/7) and [identity](https://github.com/ryjen/playstate/issues/9) gates before any exposed deployment.

Avoid introducing a generic CRUD framework, mediator, or broad dependency-injection container merely to support these boundaries.
