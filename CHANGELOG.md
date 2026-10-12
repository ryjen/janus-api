# Unreleased

- Validate SAM CLI 1.168.0 template with focused W3005 exception for generated API usage-plan dependencies; add SAM validation to CI.
- Document unresolved email ownership verification in prototype account enrollment and block publication until addressed.

- Move to the .NET 10 managed AWS Lambda runtime.
- Replace unsupported Serverless Framework tooling with AWS SAM; separate per-function IAM, API key usage plan, resource names, DLQ and PITR for DynamoDB.
- Return Cognito ID tokens to clients for API Gateway user-pool authorizer.
- Remove generic table-selected HTTP CRUD exposure and make account HTTP access read-only.
- Require Cognito authorizer and subject-derived record IDs for account/player HTTP routes.
- Split account SQS provisioning from HTTP routing; conditionally insert account records to survive duplicate delivery.
- Namespace AWS table, user-pool and queue resources by stage; add SQS dead-letter queue.
- Remove unvalidated JWT identity helper and personal-data message logging.
- Add access-policy tests, architecture notes, and a publication gate.

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
