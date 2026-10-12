# Public portfolio publication gate

**Do not change this repository to public by toggling visibility.** The private repository retains historical commits and other artifacts that have not been verified safe for publication.

## Mandatory before publishing

- [ ] Confirm the historical GitGuardian API-key exposure has been investigated and all affected credentials are revoked/rotated, including any downstream integrations.
- [ ] Run an independent secret and sensitive-data scan over **every Git ref, tag, reachable commit, workflow log, release and issue attachment** intended for publication, not just the current checkout.
- [ ] Create a separate reviewed public source snapshot with a clean new history; exclude private CI, production identifiers, account data, credentials, environment files, and provenance that should stay private.
- [ ] Select and explicitly approve an open-source license and verify ownership/third-party rights.
- [x] Replace obsolete Serverless Framework files with an AWS SAM template, per-function IAM and local dotnet publish entrypoints (source committed; runtime and deployment validation still pending).
- [ ] Build and test the exact candidate with .NET 10, then run `sam validate --lint` and review the expanded SAM/CloudFormation for auth and resource wiring; review dependencies/SBOM.
- [ ] Exercise protected routes with missing, invalid and cross-user auth; test Cognito signup/challenge, SQS retry and DLQ handling in disposable AWS infrastructure.
- [ ] Reconcile README diagrams, supported API claims, CI, and current limitations with the exact publication candidate.
- [ ] Get an independent security/publication review before changing visibility. No production deploy is implied by publication.

## Intentionally deferred

- Migrating existing production state. The new stage-scoped table and identity names must **not** be treated as an in-place migration.
- Full role-per-function AWS permissions, managed deployment, hosted demo, and security certification.
- Public history preservation: a clean snapshot is preferred to attempting to prove every private historical object safe.

This is a publication checklist, not evidence that any check has passed.
