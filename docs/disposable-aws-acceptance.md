# Disposable AWS identity and record acceptance protocol

Status: **non-executed source-only qualification plan**.
Tracks [#9](https://github.com/ryjen/playstate/issues/9),
[#5](https://github.com/ryjen/playstate/issues/5),
[#6](https://github.com/ryjen/playstate/issues/6),
[#12](https://github.com/ryjen/playstate/issues/12) and
[#20](https://github.com/ryjen/playstate/issues/20).
Does **not** authorize AWS operations, create resources or deploy.

## Operator preflight — required before any live call

Obtain operator approval for a newly disposable sandbox account, budget and
region, isolated role, synthetic inbox/identities, stack name and cleanup owner.
Verify the historic usage-plan API key exposure has been contained (#7), and
that source revisions, dependencies, required CI checks and independent review
meet the project engineering gates. Use only short-lived explicitly approved
credentials; never put identifiers, messages, secrets or AWS user records in
public GitHub issues or CI logs.

Preflight check is **blocked** until the owner approves the environment and
the self-hosted flake toolchain (#51) is independently qualified.

## Static test evidence to attach privately

- Exact commit SHA, Nix lock revision and versioned SAM template hash.
- .NET release build, all unit/contract tests and source-based SAM build.
- Linter/SAM versions and successful offline validation. Skips are not passes.
- Independent source/security review tied to the exact head.
- Privately retained IAM diff and least-privilege limits; no wildcard roles,
  production credentials or remote mutable operations in public workflows.
- A pre-created teardown manifest and cost alarm.

## Live scenarios — no scenario has been executed

| Area | Setup / stimulus | Expected decision and private evidence |
| --- | --- | --- |
| Signup | Synthetic unconfirmed email, valid attributes | Cognito user remains unconfirmed; no application account |
| Confirmation | Correct code and owned inbox | Cognito verified identity triggers **trusted** PostConfirmation callback, queue publish then conditional account create |
| Forged callback | Client-supplied confirmation/sub/email claim | Cannot invoke trusted trigger or create someone else's account |
| Codes | Wrong, expired and reused confirmation code | Generic error, no spurious account record; no code in logs |
| Duplicate user | Existing vs eligible synthetic email | Consistent safe acknowledgment; note timing and delivery differences; measure rate and abuse exposure privately |
| Password reset | Unknown account and valid synthetic account, redeem code | Non-enumerating response; secrets unchanged; unconfirmed/expired values cannot bypass Cognito |
| Protected routes | No API key, no token, invalid/expired token, access token vs ID token | Reject according to actual authorizer contract; never confuse usage key with proof of user |
| Cross-subject | JWT for synthetic subject A with payload `id` of B | Reject before any database access; no B data read/written |
| Missing account | Confirmed user prior to record materialization | Authenticated verified-email reconciliation safely queues own account |
| Queue outage | Denied/timeout SQS publish after confirmation | Treat effect as unknown, do not claim Cognito rollback; authenticated reconciliation recovers |
| Queue replay | Two identical delivery events | Conditional Dynamo create exactly once, no overwrite |
| Cognito trigger boundary | Wrong `triggerSource`, unverified email, missing `request.userAttributes.sub` or email | Validate at `VerifiedEnrollment.FromCognitoEvent` **before** SQS publishing; no account message for rejected inputs. A non-PostConfirmation event is ignored. These are not SQS retry/DLQ cases. |
| Malformed SQS account message | Null or malformed JSON, missing/blank `Id` or `Email` in the `Model.Account` message, invalid persistence input | `AccountProvisioner` consumes the queue's account DTO (not a Cognito trigger). Validate non-provisioning and bounded retry/redrive/DLQ according to the configured Lambda SQS integration; do not assume a `triggerSource` or `email_verified` field is checked at this boundary. |
| DLQ / backoff | Repeated transient Dynamo/SQS failure | Bounded retry, expected queue visibility/redrive, actionable private recovery procedure |
| Dynamo conflict | Duplicate create and two writers with stale v2 revision | At most one conditional winner; **v2 not yet implemented**; don't present as v1 result |
| Parser boundary | Invalid UTF-8, base64, duplicates, deep/massive objects, conflicting headers | For **future v2 only**, bounded rejection before persistence; keep v1 unchanged |
| Retention | Operator teardown and fresh empty stack/list | All synthetic Cognito, queues, tables, logs and billing artifacts deleted or explicitly inventoried |

## Evidence protocol

For each test record: stable case ID, intended operation, exact source SHA,
resource class only (not raw account IDs), expected and observed status,
retry/elapsed measurements where approved, redacted failure category,
queue/DLQ and Dynamo count or digest, reviewer and cleanup receipt.
Sensitive synthetic credentials and session tokens must not appear in the
evidence. Use private operator files rather than public GitHub issue bodies.
A model-generated narrative or SDK fake is **not** real AWS evidence.

Require negative tests and recoverability before any endpoint is described
as safe. Publish no demo URL, API key, user pool ID, JWT or login credential
from this protocol.

## Completion gates

- [ ] Human account/budget/IAM/privacy authority received.
- [ ] Source exact-head CI and independent security review passed on approved runner.
- [ ] All applicable scenarios executed against disposable AWS and observed
      results independently compared to expected invariants.
- [ ] Confirmation → PostConfirmation → SQS → conditional Dynamo operation
      proven causal even across partial failure and recovery.
- [ ] API Gateway token/usage-plan authorization and cross-subject boundaries
      proven against actual deployed resources.
- [ ] Retry/DLQ/redrive and teardown proven without orphaned resources.
- [ ] Findings repaired, retested and scoped release decision approved.
- [ ] Publication incident and licensing (#7) resolved separately.

No checklist item in this document is marked as performed simply because
source CI previously passed. Keep #9 open until independently validated.
