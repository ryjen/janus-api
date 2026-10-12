# Unreleased

- Move to the .NET 10 managed AWS Lambda runtime.
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
