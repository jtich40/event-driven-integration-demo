# event-driven-integration-demo
A serverless app that mocks integrating a CRM with an ERP through event-driven flow.

## API Gateway Endpoint

The ERP processor is accessible via REST API:

**Endpoint**: `https://4iu4z3q137.execute-api.us-east-1.amazonaws.com/prod/events`

### Request
```bash
POST /events
Content-Type: application/json

{
  "eventId": "string",
  "eventType": "UserCreated",
  "timestamp": "ISO-8601 datetime",
  "user": {
    "id": "string",
    "name": "string",
    "email": "string"
  }
}
```

### Response
```json
{
  "message": "Event processed successfully",
  "eventId": "evt-001",
  "userId": "user-123",
  "processedAt": "2025-10-16T..."
}
```

### Architecture
- API Gateway receives HTTP POST requests
- Routes to Lambda function via proxy integration
- Lambda processes event and stores in DynamoDB
- Returns JSON response with processing results

## Monitoring

- **CloudWatch Logs**: Full request/response logging enabled
- **CloudWatch Metrics**: Request count, latency, 4xx/5xx errors
- **Log Groups**:
  - API Gateway: `API-Gateway-Execution-Logs_[api-id]/prod`
  - Lambda: `/aws/lambda/ErpProcessorFunction`
