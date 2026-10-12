# Public portfolio publication gate

**Do not change this repository to public by toggling visibility.** The private repository retains historical commits and other artifacts that have not been verified safe for publication.

## Mandatory before publishing

- [ ] Confirm the historical GitGuardian API-key exposure has been investigated and all affected credentials are revoked/rotated, including any downstream integrations.
- [ ] Run an independent secret and sensitive-data scan over **every Git ref, tag, reachable commit, workflow log, release and issue attachment** intended for publication, not just the current checkout.
- [ ] Create a separate reviewed public source snapshot with a clean new history; exclude private CI, production identifiers, account data, credentials, environment files, and provenance that should stay private.
- [ ] Select and explicitly approve an open-source license and verify ownership/third-party rights.
- [x] Replace obsolete Serverless Framework files with an AWS SAM template, per-function IAM and local dotnet publish entrypoints (source committed; runtime and deployment validation still pending).
- [x] Locally restore/build the .NET 10 candidate; run twelve C# ownership/confirmation-event tests and publish all three Lambda artifact directories using `build.sh` (SDK 10.0.100, isolated Linux). Infrastructure checks: `cfn-lint` and seven Python contract tests passed.
- [ ] Run AWS SAM CLI template/package validation and inspect the expanded CloudFormation auth/IAM/resource wiring. Repeat local tests at the *exact final revision*; review dependencies and SBOM.
- [ ] Obtain independent review of build output, tests, SAM template and security assumptions.
- [x] Replace the source-level administrative self-enrollment path with Cognito self-signup, email confirmation, and trusted PostConfirmation provisioning (not yet verified in AWS).
- [x] Add a subject-scoped, verified-email-only account reconciliation request for confirmed users whose SQS enqueue failed (source-level repair, not AWS-verified).
- [ ] Verify the full Cognito confirmation → Lambda → SQS → DynamoDB and authenticated reconciliation flow, including permissions, retries and DLQ; see [security issue #9](https://github.com/ryjen/janus-api/issues/9).
- [ ] Exercise protected routes with missing, invalid and cross-user auth; test Cognito signup/challenge, SQS retry and DLQ handling in disposable AWS infrastructure.
- [x] Reconcile README and source diagrams with email-confirmed Cognito enrollment and the PostConfirmation SQS path at the source level.
- [ ] Reconcile public capability claims against an independently verified AWS integration candidate.
- [ ] Get an independent security/publication review before changing visibility. No production deploy is implied by publication.

## Intentionally deferred

- Migrating existing production state. The new stage-scoped table and identity names must **not** be treated as an in-place migration.
- Full role-per-function AWS permissions, managed deployment, hosted demo, and security certification.
- Public history preservation: a clean snapshot is preferred to attempting to prove every private historical object safe.

This is a publication checklist, not evidence that any check has passed.
