# Architecture, access control and operational boundaries

Janus is a historical serverless backend prototype, now being prepared as a read-only portfolio case study rather than a hardened authentication product.

## Control plane

AWS SAM `template.yaml` defines a Cognito user pool, a REST API with **Cognito as the default authorizer**, a usage plan / API key, three identity/data Lambdas, a dedicated SQS consumer, two DynamoDB tables, and a dead-letter queue. Resources are stack-scoped; no pre-existing production table or identity migration is implied.

## Request boundary

1. `POST /signup` calls Cognito admin signup, then publishes an account event to SQS. It intentionally opts out of Cognito authorization, but remains API-key protected.
2. A dedicated Lambda consumes the queue, conditionally creating the account keyed by Cognito `sub`. Idempotent repeated deliveries cannot overwrite existing account data. The queue retries failures and uses a DLQ after repeated failures.
3. `/account` only allows GET; `/player` supports GET/POST/PUT/DELETE for **the current subject only**. API Gateway first validates the ID token; the Lambda uses only the validated authorizer `sub` and rejects a contradictory caller-supplied ID.
4. The generic arbitrary-table `/data` handler is not configured for deployment. An explicit allowlisted API is required before reintroducing any such functionality.

## Trust boundary and controls

| Boundary | Control | Residual risk |
| --- | --- | --- |
| Untrusted client → API Gateway | Per-API usage plan, key requirement, default Cognito authorizer | Keys are not user auth; abuse protection remains incomplete. |
| API Gateway → Lambda | Cognito claim `sub`, checked against any supplied record ID | Direct Lambda invocation must remain IAM restricted; full sandbox negative-path tests not yet run. |
| Auth → Cognito | IAM action subset against a single pool ARN | Admin password authentication requires account lockout and threat review. |
| Auth → SQS → provision | SQS redrive policy; conditional DynamoDB put | Successful Cognito signup followed by failed enqueue can leave orphaned identity. |
| Function → DynamoDB | Separate execution roles per function, table-scoped IAM | Request body schema/field validation is incomplete; authorized callers can overwrite their own player record. |

## Decisions and trade-offs

**Managed Cognito** delegates passwords, identity lifecycle and token signature verification. The original `ReadJwtToken` identity helper was removed because decoding is not verification.

**SQS** decouples authentication from persistence and introduces eventual consistency. Conditional writes support at-least-once delivery; reconciliation of abandoned identities is still needed.

**Subject-keyed records** are simple and make cross-tenant access difficult when correctly enforced, but do not implement organization/team-level authorization.

**Per-function IAM** limits the blast radius of record functions versus a shared provider-wide role. This does not replace Lambda invocation and API Gateway access policies.

**SAM** removes the obsolete npm-based Serverless Framework packaging path; local `dotnet publish` produces artifacts and `sam validate --lint` checks the template. Deployment remains a separate explicit operation.

## Outstanding verification

- Build + unit tests on .NET 10, pinned dependency updates and C# static analysis.
- API Gateway Cognito authorizer validation with ID and access tokens; unauthenticated and cross-subject 401/403 tests.
- SQS trigger, conditional-write replays, malformed messages, DLQ handling and recovery.
- DynamoDB and Cognito lifecycle/migration, WAF and rate limits, IAM role policy analysis, logs/privacy review.
- Credential incident and Git history scan before creating any **public** mirror.

## Infrastructure lint interpretation

AWS SAM CLI 1.168.0 accepts the template under `sam validate --template-file template.yaml --region ca-central-1`. The extended `--lint` run raises W3005 for `DependsOn` edges inserted **by the SAM transform** in generated API usage-plan resources; `.cfnlintrc` documents the exception for this single warning code. Source-level `cfn-lint`, SAM `--lint` (with the exception), and Python trust-boundary contract tests pass locally. The exception does not validate runtime authentication, stage permissions, or AWS deployment.

## Publication-blocking identity design gap

The legacy signup implementation accepts a caller-selected email and temporary password through `AdminCreateUser` and suppresses the Cognito invitation. This is not proof of control of the email address, and a subsequent password challenge must not be presented as email confirmation. The current design is not suitable for exposed self-service registration. [Issue #9](https://github.com/ryjen/janus-api/issues/9) tracks a verified enrollment flow with account provisioning only after trusted confirmation. This limitation prevents public deployment claims, independently of successful compilation and template checks.
