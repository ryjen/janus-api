# Player v1 compatibility inventory and v2 activation evidence

Status: **consumer/data inventory template**, not an implemented schema.
Tracks [#12](https://github.com/ryjen/playstate/issues/12),
[#20](https://github.com/ryjen/playstate/issues/20), and
[#4](https://github.com/ryjen/playstate/issues/4).
Read alongside [player-api-contract.md](player-api-contract.md).

## What the source currently proves

- `player/Handler.cs` delegates to the legacy shared `ModelHandler`;
  `/player` has GET/POST/PUT/DELETE through Cognito-protected API Gateway.
- Ownership is tied to the verified authorizer subject; client-supplied
  `id` must match or be omitted. API key is not an identity token.
- POST and PUT have matching historical upsert behavior; GET returns
  `id` and `data` storage shape. Do not reinterpret these as v2 semantics.
- `shared/RecordRequestParser.cs` contains **opt-in** bounded UTF-8
  and depth checks with duplicate JSON property rejection; its legacy
  entry point remains permissive for backward compatibility.
- `shared/StrictRecordRequest.cs` optionally enforces a unique
  application/json content type across single- and multi-value API Gateway
  headers; **v1 does not activate this stricter path**.
- Test-only names such as `rank` and `label` are not an actual client
  or production-data inventory.

## Redacted inventory form — gather from real clients and private dataset

No production samples, credentials, subject IDs or raw game state belong in
this document. Collect only reviewed aggregate, non-identifying evidence.

| Evidence | What must be recorded |
| --- | --- |
| Consumer | Application and SDK versions, supported API stage, reachable user versions |
| Methods | Actual GET/POST/PUT/DELETE expectations, retry behaviors, special headers |
| Data | Field names/types, required/optional/null, nesting/arrays, numeric boundaries |
| Identifiers | Authenticated subject mapping, client-present `id` behavior, any legacy aliases |
| Encoding | UTF-8, escaped Unicode and base64-transformed gateway behavior |
| Size | Approved observed size/depth percentiles and maxima, not copied payloads |
| Unknowns | Unrecognized/removed fields and whether clients round-trip them |
| Conflicts | Two-device updates, overwrite expectations, recovery and offline queues |
| Compatibility | Per-version acceptance and intended deprecation timeline |
| Privacy | Whether any values are sensitive; approved redaction and retention |

If evidence is missing, the corresponding v2 field/schema/precondition
decision is **blocked**. Do not invent an allowlist or arbitrary limits.
A reviewer must accept exact bytes/depth defaults after measured envelopes
and API Gateway/ Lambda limits are assessed.

## Proposed v2 approval decisions

- Explicit version route and controlled overlap with v1, as agreed in
  [migration runbook](versioned-migration-runbook.md).
- Fixed DTO with enum-backed fields/operations where feasible;
  documented additional/unknown properties behavior.
- Nonzero maximum decoded UTF-8 bytes, JSON nesting depth, string size,
  collection size, duplicate-property behavior and numeric bounds.
- POST create-only with conditional write, PUT versioned replace,
  DELETE precondition and GET missing-record outcome. Document absence
  semantics, idempotency, 409 conflict and 413 over-limit behavior.
- ETag or revision precondition choice must be explicit. Include replay
  and concurrent writers; no check-then-put race without a condition.
- Field validation must run **after verified subject authorization** and
  **before storage mutation**. Reject caller-chosen tables/identities.
- Strict parser and media type may activate on **v2 only** after
  approved tests; do not silently change the legacy endpoint.

## Qualification cases (future contract only)

| Family | Synthetic fixture | Required invariant |
| --- | --- | --- |
| Principal | Missing, contradictory, wrong type, cross-subject `id` | Deny before storage calls |
| JSON | Empty/nonobject, trailing documents, duplicate nested keys | Reject without conversion |
| Encoding | Lone surrogates, invalid escapes, bad base64, non-UTF8 bytes | Reject safely |
| Boundary | Near/over approved byte/depth/string limits | Reject before unbounded allocation |
| HTTP headers | Missing, conflicting case/duplicate/multivalue Content-Type | Deterministic strict v2 rejection |
| Schema | Null, unknown keys, large arrays, mixed numeric types | Match versioned DTO rules |
| Persistence | Create race, stale update/delete, missing row | Conditional-write semantics, no lost update |
| AWS | Gateway encoding, JWT authorizer, throttling, dependency outage | Evidence from disposable stack only |

The existing synthetic source tests can support implementation but do not
prove network-level API Gateway transformations or DynamoDB conditional-write
behavior. Those require the explicitly approved disposable environment and
[acceptance protocol](disposable-aws-acceptance.md).

## State

- [x] Historic v1 behavior documented as baseline.
- [x] Opt-in parsing/header hardening exists in source, not in v1 routing.
- [ ] Real consumer and persisted-field inventory supplied and reviewed.
- [ ] Approved v2 DTO, sizes and concurrency semantics.
- [ ] Separate v2 handler/schema/persistence implementation.
- [ ] Exact-head self-hosted CI, independent review and actual AWS
      negative-path/concurrency results.
- [ ] Migration and rollback acceptance.

Keep #12 and #20 open until the uncompleted checks are demonstrated.
