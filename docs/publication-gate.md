# Public portfolio publication gate

**Current state (October 12, 2026): this repository has already been changed to public without completing this gate.** Public visibility exposes existing Git history as well as the latest branch. This checklist is now an **urgent post-exposure remediation gate**, not merely a pre-publication plan. Historical credential exposure must be treated as potentially compromised until provider-side revocation is confirmed. Making a repository private again or deleting a file is not sufficient credential remediation, since public copies may persist. Do not claim a clean public release or deploy the service until remaining gates are independently satisfied.

## Immediate exposure response and remaining mandatory gates

- [ ] Urgently confirm revocation/rotation of the historically detected API key and audit relevant provider usage; do not post the key value in an issue, log or chat.
- [ ] Decide whether to temporarily restrict GitHub visibility while making a separately reviewed clean-history public snapshot. This is a repository-owner decision; visibility remains public until deliberately changed.
- [ ] Assume historic reachable Git objects may have been cloned; preserve incident evidence and do not equate branch cleanup with removal of previously exposed bytes.

## Original publication acceptance criteria (not yet cleared)

- [ ] Confirm the historical GitGuardian API-key exposure has been investigated and all affected credentials are revoked/rotated, including any downstream integrations.
- [ ] Run an independent secret and sensitive-data scan over **every Git ref, tag, reachable commit, workflow log, release and issue attachment** intended for publication, not just the current checkout.
- [ ] Create a separate reviewed public source snapshot with a clean new history; exclude private CI, production identifiers, account data, credentials, environment files, and provenance that should stay private.
- [ ] Select and explicitly approve an open-source license and verify ownership/third-party rights.
- [x] Replace obsolete Serverless Framework files with an AWS SAM template, per-function IAM and source-based SAM build entrypoints (source committed; runtime and deployment validation still pending).
- [x] Locally restore/build the .NET 10 candidate; run nineteen C# ownership/confirmation-event/recovery tests and package all Lambda functions with `sam build` (SDK 10.0.100, isolated Linux). Infrastructure checks: `cfn-lint` and nine Python contract tests passed.
- [x] Validate the source SAM template with AWS SAM CLI 1.168.0 and package all Lambda functions locally with `sam build` (restricted host used a validation-only zip compatibility utility).
- [ ] Validate the expanded CloudFormation auth/IAM/resource wiring against disposable AWS; repeat local and portable CI tests at the *exact publication revision*; review dependencies and SBOM.
- [ ] Obtain independent review of build output, tests, SAM template and security assumptions.
- [x] Replace the source-level administrative self-enrollment path with Cognito self-signup, email confirmation, and trusted PostConfirmation provisioning (not yet verified in AWS).
- [x] Add a subject-scoped, verified-email-only account reconciliation request for confirmed users whose SQS enqueue failed (source-level repair, not AWS-verified).
- [ ] Verify the full Cognito confirmation → Lambda → SQS → DynamoDB and authenticated reconciliation flow, including permissions, retries and DLQ; see [security issue #9](https://github.com/ryjen/janus-api/issues/9).
- [ ] Exercise protected routes with missing, invalid and cross-user auth; test Cognito signup/challenge, SQS retry and DLQ handling in disposable AWS infrastructure.
- [x] Reconcile README and source diagrams with email-confirmed Cognito enrollment and the PostConfirmation SQS path at the source level.
- [ ] Reconcile public capability claims against an independently verified AWS integration candidate.
- [ ] Obtain independent security/publication review now that visibility has already changed; record exposure remediation and approve any eventual clean-history replacement. No production deploy is implied by public source access.

## Intentionally deferred

- Migrating existing production state. The new stage-scoped table and identity names must **not** be treated as an in-place migration.
- Full role-per-function AWS permissions, managed deployment, hosted demo, and security certification.
- Public history preservation: a clean snapshot is preferred to attempting to prove every private historical object safe.

The successful hosted source-build and SAM packaging check on PR #13 (`b8aa2f1d2bd6f0b45bf2f3d16120dbf76d280867`) demonstrates compilation/packaging, **not** historical safety or runtime security. This is a checklist; unchecked items remain blockers even if the repository is publicly accessible.
