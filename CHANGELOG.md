# Unreleased

- Validate opt-in versioned record JSON against strict RFC 8259 token grammar (reject comments, trailing commas, unquoted/single-quoted keys and invalid number spellings), while preserving nested duplicate-key checks and unchanged legacy v1 parsing; add focused regression tests. Source-only candidate; no v2 route is activated.

- Add an opt-in, fixed-size in-memory authentication outcome counter using enum-only dimensions and atomic increments; no arbitrary synchronous callback or external telemetry, preserving authentication response availability.

- Redact Cognito ID tokens and opaque challenge sessions from default authentication result string formatting; preserve actual result fields for protocol use and test the diagnostic representations.

- Move Cognito login/challenge completion and provider-error classification into a typed application service; retain legacy HTTP response mapping and fail-closed token requirements. Add fake-gateway outcome regression tests.

- Centralize the v1 Cognito Lambda route literals behind an enum-backed route contract; preserve all existing route names and fail-closed unknown-path behavior with regression tests.

- Distinguish Cognito login and required-new-password challenge rejections (generic 401) from upstream or unknown-effect errors (generic 503), using a typed exception-classification policy and fake-provider regression tests. No API routes, success payloads, IAM or AWS resources changed.

- Return generic 503 for Cognito signup/confirmation provider failures, preserving 400 for known invalid request/code outcomes and duplicate-signup non-enumeration. Do not expose upstream errors or assert rollback after indeterminate signup results.

- Validate Cognito authentication and legacy new-password challenge inputs without coercing structured credentials into strings; preserve exact password/session bytes and keep invalid-credential responses generic.

- Extract account recovery and provisioning application ports with a shared SQS/DynamoDB adapter; split the SQS Lambda entry point from the account HTTP handler and inject Cognito/client queue dependencies for isolated tests. Preserve the existing wire envelope and conditional DynamoDB behavior.

- Isolate subject-scoped record operations behind an application service and a per-resource DynamoDB adapter; add fake-port unit tests, stop mapping backend outages to HTTP 400, preserve existing record JSON and Lambda entry points.


- Enable Cognito account-existence error protection for login/recovery, normalize duplicate-signup responses, and add code-based forgotten-password request/confirmation routes; retain and correctly describe legacy /reset challenge semantics.


- Validate SAM CLI 1.168.0 template with focused W3005 exception for generated API usage-plan dependencies; add SAM validation to CI.
- Replace unverified administrative self-enrollment with Cognito email-confirmed signup and a PostConfirmation SQS provisioning trigger; cloud integration remains blocked.

- Move to the .NET 10 managed AWS Lambda runtime.
- Replace unsupported Serverless Framework tooling with AWS SAM; separate per-function IAM, API key usage plan, resource names, DLQ and PITR for DynamoDB.
- Return Cognito ID tokens to clients for API Gateway user-pool authorizer.
- Remove generic table-selected HTTP CRUD exposure and make account HTTP access read-only.
- Require Cognito authorizer and subject-derived record IDs for account/player HTTP routes.
- Split account SQS provisioning from HTTP routing; conditionally insert account records to survive duplicate delivery.
- Namespace AWS table, user-pool and queue resources by stage; add SQS dead-letter queue.
- Remove unvalidated JWT identity helper and personal-data message logging.
- Add access-policy and confirmed-enrollment tests, architecture notes, and a publication gate.

# v0.1.0

- generic 'data' api endpoint for any table
- api endpoints no longer assume authorized id, must pass in params
- data fields lowercased
- consolidated shared code and other cleanup

## Generic Data

```json
{
  "entity": "<table>",
  "id": "<key>",
  "data": {
    ..json..
  }
}
```

- uses 'id', 'entity' and 'data' keys
- data can be any json

### Existing endpoints

- 'player' and 'account' endpoints can specify data directly in request

```json
{
   "id": "<value>",
   "<key>": "<value>",
   ...
}
```
