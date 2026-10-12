# Architecture and security boundaries

Janus separates identity management, HTTP request handling and application record persistence.

## Flow

1. `POST /signup` calls Cognito `AdminCreateUser` and publishes an account event to SQS. The caller receives the Cognito identity before asynchronous persistence completes.
2. The **dedicated** `accountProvision` Lambda consumes SQS records and conditionally creates one DynamoDB account keyed by Cognito `sub`. Repeated deliveries do not overwrite an existing record.
3. Account and player routes require an API Gateway Cognito authorizer. The Lambda then obtains `sub` exclusively from authorizer claims and derives the DynamoDB key. A mismatched caller-supplied ID is rejected.
4. DynamoDB table names are stage-scoped. The generic data route has no deployment mapping.

## Trust boundaries

| Boundary | Control | Remaining risks |
| --- | --- | --- |
| External client -> API Gateway | API key on all HTTP methods; Cognito authorizer on protected record routes | API key is not a user principal. Signup abuse/rate limiting needs separate controls. |
| API Gateway -> Lambda | Authorizer claims are checked fail-closed | HTTP handlers rely on requests arriving via configured API Gateway; direct invocation privileges must be controlled by AWS IAM. |
| Auth Lambda -> Cognito | Scoped user-pool ARN | Prototype uses administrative password auth; credential abuse and challenge flows need integration tests. |
| Auth Lambda -> SQS -> accountProvision | Typed account message and conditional DynamoDB insert | Malformed messages go to a DLQ; account may not yet exist immediately after signup. |
| Lambda -> DynamoDB | Subject-derived keys for HTTP records, stage-specific tables | Provider-level IAM roles are broader than ideal; content validation and per-function IAM remain work. |

## Important implementation decisions

- **Cognito over custom credential storage.** Password authentication and identity lifecycle are delegated. The app does not validate a JWT locally or use unverified decoded claims for identity.
- **SQS for account creation.** Decouples identity and persistence but deliberately introduces eventual consistency and retry semantics. The consumer uses a conditional write to avoid overwriting existing accounts.
- **Subject-keyed records.** Simple ownership rule suitable for single-record-per-user prototypes; it is not an extensible authorization design for shared/organizational resources.
- **Fail-closed data exposure.** The former generic `/data` route is not registered because table-level and object-level authorization were absent.
- **Stage-scoped resources.** Allows isolated experiments at the cost of an explicit data and user-pool migration before reusing an existing installation.

## Validated versus not validated

The repository contains source-level access-policy unit tests and build configuration. No AWS integration test, Cognito round-trip, CloudFormation deploy, account-provisioning DLQ recovery or penetration test has yet been verified against a deployed system.

Before production consideration: separate the IAM roles, introduce bounded request DTO validation and data schema ownership, define account deletion semantics, monitor queue age and DLQ depth, test challenge and recovery flows, add abuse protections, and formalize migration/backup plans.
