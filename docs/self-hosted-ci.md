# Playstate self-hosted CI qualification (staged, not active)

Tracking: [Playstate #51](https://github.com/ryjen/playstate/issues/51),
[Dubnium #817](https://github.com/ryjen/dubnium/issues/817),
[Dubnium #750](https://github.com/ryjen/dubnium/issues/750), and
[Micrantha meta #125](https://github.com/hackelia-micrantha/.github/issues/125).

## Current boundary

This branch deliberately does **not** change `.github/workflows/validate.yml`
or register/activate any GitHub runner. The current workflow still selects
GitHub-hosted `ubuntu-latest` on PR/main events, so opening a normal PR from
this branch would consume hosted minutes in conflict with the operator's
self-hosted-only policy. Keep this candidate on a source branch until the
Dubnium JIT identity, trust admission, and required check are qualified.

The source-validated environment pins `nixos-26.05` to the same input revision
and integrity hash already present in `ryjen/ops-cadence`. This **does not prove**
that the Nix package set contains a working .NET 10 + AWS SAM CLI combination:
`nix flake check`, locked package materialization and actual packaging
on an approved Linux host are still required. **Verified source mismatch:**
the exact pinned Nixpkgs revision currently declares SAM CLI **1.160.0**
and `cfn-lint` **1.43.3** in its package definitions. The previously qualified
hosted CI instead pinned SAM CLI **1.168.0** and stand-alone `cfn-lint`
**1.57.2** (the latter isolated due to incompatible shared Python constraints).
The staged flake explicitly **fails Nix evaluation** until those qualified
versions are available from a reviewed new pin or approved package overrides.
Do not treat copying an existing lock as toolchain validation, weaken the
version assertions, or silently install dependencies with ambient `pip`
or `setup-dotnet`. This is a blocked candidate, not an executable CI claim.

## Proposed single entry point

After independently approved JIT routing:

```sh
nix flake check --no-write-lock-file
nix develop --command bash ci/validate-source.sh
```

The script runs the existing .NET 10 restore/build/unit tests, source-based
`sam build`, offline SAM template validation, `cfn-lint`, and 11 infrastructure
contract tests. It never runs `sam deploy`, and does not use AWS credentials.

## Before opening the migration PR

1. Resolve an **actual** compiled Dubnium registry record for `ryjen/playstate`
   (renamed from `ryjen/janus-api`), credential class, JIT profile, limits
   and exact allowed selector; none is currently declared in the 42-repo
   ordinary registry source. Do not guess a runner label.
2. Review public/fork PR trust and ensure no untrusted fork PR runs candidate
   code with any privileged workspace identity, controller or token. Keep
   the runner rootless, source scoped, least privileged and immutable.
3. Check Nix evaluation, pinned package versions, dependency isolation,
   `dotnet` tests, SAM packaging, linter behavior, and evidence reproducibility.
4. Coordinate branch-protection required check identity and exact
   independently reviewed merge candidate; never report blocked/skipped
   validations as successful.
5. Change the workflow to the **verified exact JIT selector** and flake
   entry point only as part of that reviewed candidate. No hosted fallback,
   increased billing budget, or activation by documentation changes.

Separate blockers [#7](https://github.com/ryjen/playstate/issues/7)
(legacy exposed AWS API Gateway usage-plan key and historical PR refs) and
[#9](https://github.com/ryjen/playstate/issues/9)
(real Cognito confirmation → SQS → DynamoDB) remain in force. This stage is
not a live AWS test or public release.
