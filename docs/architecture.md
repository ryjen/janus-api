# Architecture, access control and operational boundaries

Playstate is a historical serverless backend prototype, now being prepared as a read-only portfolio case study rather than a hardened authentication product.

## Control plane

AWS SAM `template.yaml` defines a Cognito user pool, a REST API with **Cognito as the default authorizer**, a usage plan / API key, three HTTP identity/data Lambdas, a separate PostConfirmation Lambda, a dedicated SQS consumer, two DynamoDB tables, and a dead-letter queue. Resources are stack-scoped; no pre-existing production table or identity migration is implied.

## Account lifecycle dependency boundaries

The auth, account recovery and provisioning Lambda entry points retain parameterless
constructors for AWS activation but also accept injected interfaces in tests.
`AccountConfirmed` is a Cognito event adapter, `AccountRecoveryService`
controls verified-account reconciliation, and `AccountProvisioner` is a
separate SQS event adapter (no longer a method on the account query handler).
`IAccountPublisher` and `IAccountStore` keep these application decisions
independent of SQS and DynamoDB. Infrastructure remains in the existing shared
project until the historical auth/account duplicate assembly names can be
migrated without breaking deployed handler names. The Cognito confirmation callback propagates publisher failures rather than
acknowledging success, but this does **not** establish Cognito retry guarantees.
A confirmed user can request reconciliation if enqueue fails. SQS consumer
failures propagate for queue redelivery/DLQ handling; HTTP recovery dependency
failures return 503. Only the trusted confirmation trigger initiates
new-account provisioning.

## Request boundary

1. `POST /signup` calls Cognito self-enrollment; `POST /confirm` verifies the email code. Cognito invokes a PostConfirmation Lambda which publishes the account event to SQS. These external auth flows intentionally opt out of the user authorizer but remain API-key protected.
2. A dedicated Lambda consumes the queue, conditionally creating the account keyed by Cognito `sub`. Idempotent repeated deliveries cannot overwrite existing account data. The queue retries failures and uses a DLQ after repeated failures.
3. `/account` only allows GET; `/player` supports GET/POST/PUT/DELETE for **the current subject only**. API Gateway first validates the ID token; the Lambda uses only the validated authorizer `sub` and rejects a contradictory caller-supplied ID.
4. The generic arbitrary-table `/data` handler is not configured for deployment. An explicit allowlisted API is required before reintroducing any such functionality.

## Trust boundary and controls

| Boundary | Control | Residual risk |
| --- | --- | --- |
| Untrusted client → API Gateway | Per-API usage plan, key requirement, default Cognito authorizer | Keys are not user auth; abuse protection remains incomplete. |
| API Gateway → Lambda | Cognito claim `sub`, checked against any supplied record ID | Direct Lambda invocation must remain IAM restricted; full sandbox negative-path tests not yet run. |
| Auth → Cognito | Self-service email-confirmed signup; admin auth action subset against a single pool ARN | Password authentication requires lockout and abuse review. |
| PostConfirmation → SQS → provision | Trigger checks confirmed email, SQS redrive, conditional DynamoDB put | Confirmation followed by failed enqueue can leave an orphaned application account; reconciliation needed. |
| Function → DynamoDB | Separate execution roles per function, table-scoped IAM | Request body schema/field validation is incomplete; authorized callers can overwrite their own player record. |

## Decisions and trade-offs

**Managed Cognito** delegates passwords, identity lifecycle and token signature verification. The app client explicitly sets `PreventUserExistenceErrors: ENABLED` to avoid the default legacy account-enumeration behavior in login and recovery. Cognito's `SignUp` still throws `UsernameExistsException` for duplicate usernames even with this setting; the HTTP handler normalizes that case to the same acknowledgement as a valid signup, although timing/rate/delivery differences remain unverified. Public password recovery uses Cognito `ForgotPassword` and `ConfirmForgotPassword`, not the legacy administrative `NEW_PASSWORD_REQUIRED` challenge route. The original `ReadJwtToken` identity helper was removed because decoding is not verification.

**SQS** decouples authentication from persistence and introduces eventual consistency. Conditional writes support at-least-once delivery; reconciliation of abandoned identities is still needed.

**Subject-keyed records** are simple and make cross-tenant access difficult when correctly enforced, but do not implement organization/team-level authorization.

**Per-function IAM** limits the blast radius of record functions versus a shared provider-wide role. This does not replace Lambda invocation and API Gateway access policies.

**SAM** removes the obsolete npm-based Serverless Framework packaging path; local `dotnet restore/build/test` verifies the application; the source-based `sam build` then packages Lambda archives into `.aws-sam/build`, and `sam validate --lint` checks the template. Deployment remains a separate explicit operation.

## Outstanding verification

- On this candidate, .NET 10 release build and **68 C# unit/adapter tests** passed locally; **11 SAM source-contract tests**, direct CloudFormation lint and SAM template validation passed. .NET auth/account/player publish succeeded. Latest-head SAM ZIP packaging and portable hosted CI have not passed, and dependency review and C# static analysis remain open.
- API Gateway Cognito authorizer validation with ID and access tokens; unauthenticated and cross-subject 401/403 tests, non-enumerating password recovery, expired or incorrect reset codes and rate limits.
- SQS trigger, conditional-write replays, malformed messages, DLQ handling and recovery.
- DynamoDB and Cognito lifecycle/migration, WAF and rate limits, IAM role policy analysis, logs/privacy review.
- Credential incident and Git history scan before approving a sanitized public release.

## Infrastructure lint interpretation

AWS SAM CLI 1.168.0 accepts the template under `sam validate --template-file template.yaml --region ca-central-1`. The extended `--lint` run raises W3005 for `DependsOn` edges inserted **by the SAM transform** in generated API usage-plan resources; `.cfnlintrc` documents the exception for this single warning code. Source-level `cfn-lint`, SAM `--lint` (with the exception), and Python trust-boundary contract tests pass locally. The exception does not validate runtime authentication, stage permissions, or AWS deployment.

## Enrollment security implementation and validation gap

The historical administrative self-signup path has been replaced in this candidate with Cognito self-enrollment and email-code confirmation. A Cognito PostConfirmation Lambda emits the SQS account event only for the `PostConfirmation_ConfirmSignUp` trigger with confirmed email attributes. Unit and source-template tests cover trigger filtering and IAM boundaries. **No disposable AWS integration test has established that the confirmation, Lambda permission, enqueue, SQS retry and account write actually work together**, including partial failure and recovery. [Issue #9](https://github.com/ryjen/playstate/issues/9) remains blocking until that evidence is available.

## Authenticated recovery of partial confirmation failures

Because Cognito confirmation and SQS enqueue are not a transaction, a confirmed identity may not yet have an account record. The explicit `POST /account/reconcile` route uses the existing Cognito default authorizer, requiring signed `sub`, `email` and `email_verified=true` claims. It checks its own account record and queues provisioning only when missing. The SQS consumer's conditional write makes repeated or overlapping deliveries idempotent. Recovery has a separate Lambda role with `GetItem` on Account and `SendMessage` on the single queue; it cannot mutate tables or enroll identities. Network, AWS IAM, consumer and rate/abuse validation remain cloud-side integration gates. This restores account materialization after a failed PostConfirmation enqueue, but does not address unconfirmed identities, abandoned signup attempts, or DLQ operator alerting.
