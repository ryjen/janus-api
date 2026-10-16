# Versioned service migration, staging and rollback contract

Status: **proposed source-only operator design, not approved or deployed**.
Tracks [#1](https://github.com/ryjen/playstate/issues/1), [#2](https://github.com/ryjen/playstate/issues/2), [#3](https://github.com/ryjen/playstate/issues/3), [#4](https://github.com/ryjen/playstate/issues/4), [#5](https://github.com/ryjen/playstate/issues/5), [#6](https://github.com/ryjen/playstate/issues/6).
Player validation is separately tracked by [#12](https://github.com/ryjen/playstate/issues/12) and [#20](https://github.com/ryjen/playstate/issues/20).

## Verified source baseline, not a runtime claim

The committed `template.yaml` declares a REST API with stage name `v1`,
stack-name-scoped DynamoDB Account and Player tables, a Cognito user pool and
app client, PostConfirmation Lambda → SQS → conditional account creation, and
DynamoDB point-in-time recovery. The deployed status of any stack is **unknown**.
A fresh stack is not an export or migration of historical users/player data.

The existing `/player` v1 POST and PUT operations share upsert behavior.
`docs/player-api-contract.md` proposes different v2 semantics (create-only
POST, revision-controlled PUT/DELETE) but **neither a v2 schema nor a deployed
v2 route exists**. Keep all v1 behavior unchanged until clients and persisted
records have been inventoried and approved.

The source currently uses Cognito-confirmed subject IDs as the account/record
ownership key. Changes to Cognito pool or subject IDs may break access even if
DynamoDB records copy successfully. API-key gating is not identity proof.

## Migration prerequisites and roles

Before any stateful or identity migration, require an approved owner, source
and destination account/region/stack identity, data classification, migration
date and freeze window, explicit rollback authority, budget and privacy review.
Record **references** to proof in private operator evidence; do not paste
tokens, emails, subject IDs or exported player documents into issues, PRs or CI.

A clean source release or a successful CI job **cannot** authorize migration:
release gates [#7](https://github.com/ryjen/playstate/issues/7)
(historical API Gateway key), [#9](https://github.com/ryjen/playstate/issues/9)
(disposable AWS Cognito→SQS→Dynamo proof) and
[#51](https://github.com/ryjen/playstate/issues/51)
(approved self-hosted CI) remain independent.

Use segregated roles:
- **Operator/approver** authorizes cost, source/destination accounts and
  go/no-go, including access to identity data and export destination.
- **Migration worker** has time-limited, resource- and stage-scoped read
  permissions on a known source and write permissions only on a disposable
  destination; no account-wide wildcard admin role and no CI-exposed secrets.
- **Independent reviewer** verifies manifests, reconciliation and rollback
  evidence without needing raw credentials or private records.
- **Rollback operator** is explicitly authorized to switch traffic and
  restore an independently verified snapshot; not granted through this doc.

## #4 — Explicit versioning and compatibility

Proposed additive migration sequence (not active):
1. Preserve the current `/v1` REST base and all handlers and response
   shapes. New `/v2/player` is a *future* separate route/API stack, not a
   silent change to `/player`; choose whether versioning is per API stage or
   path **before** configuring DNS, SDKs or CloudFormation resources.
2. Publish a v2 schema only after real client payload inventory. Specify
   allowed fields, unknown-property behavior, validation/UTF-8 limits,
   precondition headers, error bodies and explicit POST/PUT/DELETE semantics.
3. Add versioned consumer tests and dual-client compatibility verification
   before any v2 caller is enabled. Never infer schema from test-only
   `rank`/`label` examples.
4. Observe adoption only through authorized, privacy-reviewed aggregate
   counts; keep v1 accepting its historical shape while migrations run.
5. Only retire v1 after a dated owner approval, verified client cutoff,
   archived compatibility matrix, migration reconciliation and rehearsed
   rollback. No automatic endpoint removal.

## #2 — Cognito identity migration

- Inventory **configuration**, never export or publish user identities in
  CI: pool sign-in aliases, verified-email policy, app-client flows and
  authorizers, PostConfirmation trigger, MFA, required attributes, current
  callback/wiring and IAM.
- Do not assume that Cognito password hashes or the original `sub` can be
  exported and reimported unchanged. Select and document a supported
  user-migration/authentication-challenge flow or deliberate password-reset
  process based on current AWS provider documentation, security review and
  explicit user communication/consent.
- Never forge email verification or synthesize a trusted Cognito `sub`
  from caller data. If subjects change, build a private, access-controlled
  old-subject ↔ new-subject reconciliation manifest with recorded authorization
  and deletion/retention policy. Encrypt the manifest at rest using an
  approved customer-managed encryption key with scoped decrypt access,
  change/audit logging and separately authorized backup recovery. Store it
  only in a private, access-controlled operator evidence location; redact
  individual subjects from CI, PRs, dashboards and public logs. Record
  immutable checksums and a bounded retention/deletion date. Do **not**
  commit or publicly export the mapping.
- Test duplicate email/account collision, verification state, abandoned and
  confirmed users, recovery codes, MFA and session expiry, off-by-one
  pagination, rate/throttle handling, account-provisioning trigger failures,
  retries and DLQ. The queue consumer must remain idempotent.
- Never mark identity migration complete until disposable AWS end-to-end
  authorization tests show old and new client behavior and the
  Cognito → PostConfirmation → SQS → DynamoDB causal chain.

## #3 — DynamoDB migration

- Before mutation, inventory exact table key/index types, capacity, PITR
  window, TTL, encryption, item shapes, record counts, oversized items, schema
  variations and access roles. Avoid assuming `data` has a typed schema.
- Obtain a point-in-time restore/export of each source table under dedicated,
  approved access, and verify restorability into a **separate destination**.
  A stack-name-derived new table is not automatically a restore.
- Produce a versioned, private manifest of source snapshot timestamp,
  algorithm/version, destination, transformed-record count, immutable content
  digest, rejected-record count and provenance. Keep sensitive keys/records
  out of public logs and PR bodies.
- Transform idempotently in batches with bounded memory and retry/backoff for
  transient throttling; use conditional writes to avoid overwriting a
  previously provisioned account or player record. Do not accept a
  user-provided table selector.
- Compare counts, per-type checksums and sampled schema constraints in a
  private audited environment. Record collisions and unknown fields as
  actionable exceptions, never silently drop items. Hold traffic cutover
  on unresolved reconciliation deltas.
- Rehearse destination restore and route rollback with no writes to the
  original source; define how writes that occur between snapshot and
  cutover are caught up or paused. **No dual write** without a tested conflict,
  replay, deduplication and compensation design.

## #5 — Staging environment contract

Stage isolation must be enforced **at both IAM/account and stack resources**:
- Prefer an approved separate sandbox AWS account and least-privilege role.
  A new SAM stack alone is not an account boundary.
- Resource names, Cognito pools, queues, DLQs, Dynamo tables, usage plan/API
  keys and logs must be stage-scoped. Source template names derived from
  stack name are not proof that a deployment has been performed or approved.
- Only generated, synthetic and non-sensitive fixture identities/state may
  enter staging until migration access receives separate authorization.
- Require bounded cost quotas/alarms, rate and abuse protection, log
  redaction, explicit teardown and post-teardown orphan-resource inspection.
- Keep public endpoints closed by default; no production traffic, customer
  exports, shared API keys, CI role elevation or informal localhost claims.
- An explicit operator-reviewed deploy plan must be approved before running
  `sam deploy`. All validation in this repository remains **no-deploy**.

## #6 — Testing environment and acceptance matrix

Tests must be independently reproducible and results tied to a full source
SHA, source template SHA, dependency lock revision, stack/account and region
(identifiers in private evidence), with clean teardown records. A skipped,
failed or unknown check is **not** a pass.

| Level | Required evidence | Status |
| --- | --- | --- |
| Source/contract | .NET build/tests, SAM source build/validate, lint, Python contracts at exact head | Earlier PR heads passed; future migration head not tested |
| Offline adverse cases | Wrong JWT/subject, malformed and oversized JSON, duplicate keys, missing credentials, tokenless Cognito responses, retry/idempotency | Source tests partial; cannot claim runtime |
| Disposable account integration | Cognito confirmation and verified-email gate, PostConfirmation invoke, SQS delivery/redrive/DLQ, conditional Dynamo record writes | Not executed |
| Migration rehearsal | Cognito identity and subject reconciliation, PITR restore, conditional writes, conflict handling, count/digest reconciliation | Not executed |
| Cutover rehearsal | Old/v2 client overlap, backward-compatible route, traffic freeze/catch-up, rollback, clean teardown | Not executed |
| Publication/operational | Historical key revocation, privacy/rights review, independently approved clean snapshot | Not complete |

Suggested acceptance scenarios for the disposable environment include:
- Auth without API key; missing/expired/cross-subject ID tokens.
- Unverified user attempting account reconciliation; forged confirmation
  callback; duplicated PostConfirmation callback; SQS transient failure and
  DLQ; duplicate conditional writes.
- Two devices racing POST creation, stale revision PUT/DELETE and DELETE
  retry **only once v2 semantics are explicitly approved and implemented**.
- Simulated provider outage, throttling and partial write; classify unknown
  effects without assuming an AWS timeout rolled back.

## Explicit cutover and rollback gate

**Go/no-go immediately before routing any v2 traffic:**
- Confirm destination restore from an approved source PITR snapshot by a
  checksum-verified *private* manifest and independently repeated counts.
  Record the exact snapshot time and the catch-up/writes-freeze boundary.
- Independently prove that the prior v1 API stage, authorizer, Cognito pool,
  usage plan, Lambda versions and subject-to-record mappings remain
  reachable by the approved rollback operator. Preserve old endpoints and
  their infrastructure resources throughout the overlap window.
- Confirm ownership of a tested reversible DNS/API-stage route switch,
  a rollback time window, synthetic smoke tests, write replay/catch-up
  rules and alarms. Do not make the old pool authoritative for new user
  subjects without the verified private subject mapping.

**Abort or roll back on** unverified identity/subject mapping, a missing
or mismatched record manifest, failed conditional persistence, unexplained
queue/DLQ accumulation, incorrect authorizer results, cross-subject data
access, failed v1/v2 compatibility checks, material cost/latency breach,
or failure of the approved reconciliation/monitoring controls.

**Rollback sequence:** stop v2 writes at the boundary; prevent newly
divergent traffic; verify a stable snapshot and approved replay/delta
manifest; restore the correct route, authorizer and API-key configuration
without weakening authentication; reconcile accepted post-snapshot writes
under independent review; prove correct reads and idempotent messages with
synthetic users; and only then reopen normal traffic. A route toggle alone
is not rollback when identity or Dynamo data has diverged.

**Stop conditions:** if writes cannot be safely paused/caught up, or
reconciliation evidence is incomplete, stop cutover rather than roll back
to an older data image and silently discard acknowledged changes.
Retain destination and incident evidence privately until an explicitly
approved deletion and cost cleanup.

## Release and rollback decisions

Use a human go/no-go gate to record: verified backups/restores, all tests and
reviewers, client inventory and migration communication, accepted error budget,
AWS billing approval, staged incremental cutover, safe rollback and all key
rotations. Every rollback must specify whether it restores data, routes,
identities or all three, and how writes during the transition are handled.
A rollback that simply points at old data after v2 writes is **not** safe
without tested catch-up/compensation.

This document establishes a concrete plan only. None of #2–#6 has finished
real migration, staging activation, AWS testing or publication from this
document. Do not close the parent #1 until each child has explicit acceptance
evidence and an approved migration strategy.
