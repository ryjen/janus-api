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
