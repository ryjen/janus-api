# Versioned player mutation activation gates

Status: **proposed / not deployed**. The deployed `ModelHandler` remains the compatibility-only v1 implementation.

## Current boundaries

- `RecordRequestEnvelope.TryDecode` optionally decodes plain/base64 API Gateway bodies with strict UTF-8 and a decoded-byte budget.
- `RecordRequestParser.TryParseMutation(body, limits, ...)` enforces a bounded JSON object, duplicate-property rejection, depth and Unicode checks.
- `RecordFieldSchema` is an opt-in, exact-case property-name and JSON token-type allowlist.
- `StrictRecordRequest.TryParse` composes these checks; **no Lambda handler calls it**.
- `SubjectRecordService` binds persistence to authenticated Cognito `sub`; caller-provided `id` cannot select another record. Authorization must remain independent of payload validation.

## Required before a versioned route is activated

1. Inventory real mobile/game clients and persisted player mutations. Record actual keys, nested shapes, optional/required/null semantics, schema evolution and payload sizes. The sample `rank`/`label` tests are **not** the production schema.
2. Specify a distinct v2 route/event contract (methods, identity source, request body/base64 mapping, content type, response/error codes). Do **not** route v1 calls through strict parsing.
3. Agree on bounded byte and depth limits with measured clients, plus field type/required-field/nested-object policy. Keep unknown-field handling explicit, not an accidental change in v1 semantics.
4. Characterize auth precedence (missing claims vs invalid body), subject and ID mismatch, read-only accounts, datastore outages and raw-payload non-logging. Never use a body field for persistence identity.
5. Add end-to-end v2 handler tests for plain/base64, malformed JSON/Unicode, duplicate keys, oversized/deep payloads, wrong types, unknown fields, null/required fields, and no-store-call-on-invalid-request.
6. Independently review a candidate schema, route configuration, IAM and change of authorization boundaries. Verify on actual AWS with Cognito → SQS → Dynamo where applicable (issue #9).
7. Complete historical credential/publication cleanup gate (issue #7) before claiming release or public security readiness.

## Decision rule

Until field inventory and versioned route contract are approved, the strict pipeline is a reusable **library** only. Passing .NET unit tests does not prove API Gateway deployment correctness or consumer compatibility.
