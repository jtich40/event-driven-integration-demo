# Event-Driven Integration Demo

A serverless enterprise integration simulator demonstrating event-driven architecture for CRM/ERP data synchronization using .NET 8, AWS Lambda, SQS, API Gateway, and DynamoDB.

## Overview

This project showcases a modern, cloud-native integration pattern where a CRM system (ASP.NET Core API) publishes user events to an SQS queue, which automatically triggers a Lambda function to process and integrate data with downstream ERP systems.

## Architecture

### Components

- **ASP.NET Core Web API**: RESTful API for CRM operations (user management)
  - Validates and persists user data to DynamoDB
  - Publishes domain events to SQS for asynchronous processing
  - Includes mock external system integration (simulating Salesforce)

- **Amazon SQS**: Message queue for event-driven architecture
  - Decouples API from downstream processing
  - Ensures reliable event delivery with automatic retries
  - Enables scalable, asynchronous communication between services

- **AWS Lambda**: Serverless event processor for ERP integration
  - Triggered automatically by SQS messages (event-driven)
  - Also exposed via API Gateway for direct invocation
  - Simulates enterprise system integration (Oracle Fusion, Peoplesoft)
  - Processes events and stores results in DynamoDB

- **Amazon DynamoDB**: NoSQL database (2 tables)
  - `Users`: Primary CRM data store
  - `ErpProcessedUsers`: Audit trail of processed integration events

- **AWS API Gateway**: REST API endpoint for Lambda function
  - Provides HTTP interface to event processor
  - Demonstrates serverless API patterns with Lambda proxy integration

### Architecture Diagram
```
┌─────────────┐
│   Client    │
└──────┬──────┘
       │ POST /api/crm
       ▼
┌─────────────────────┐
│  ASP.NET Core API   │
│  (Local/EC2)        │
└──────┬──────────────┘
       │
       ├──────────────────┐
       │                  │
       ▼                  ▼
┌─────────────┐    ┌──────────────┐
│  DynamoDB   │    │  SQS Queue   │
│   (Users)   │    │  (Events)    │
└─────────────┘    └──────┬───────┘
                          │
                          │ (Auto-trigger)
                          ▼
                   ┌──────────────┐
                   │    Lambda    │
                   │  (Processor) │
                   └──────┬───────┘
                          │
                          ▼
                   ┌──────────────┐
                   │  DynamoDB    │
                   │ (Processed)  │
                   └──────────────┘
```

### Event Flow

1. Client sends POST request to ASP.NET Core API (`/api/crm`)
2. API validates and saves user to DynamoDB `Users` table
3. API publishes `UserCreatedEvent` to SQS queue
4. SQS automatically triggers Lambda function
5. Lambda processes event (simulates ERP integration)
6. Lambda stores processed event in DynamoDB `ErpProcessedUsers` table
7. Full audit trail maintained for compliance

## API Gateway Endpoint

The ERP processor is also accessible via REST API for direct invocation:

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

### How It Works
- API Gateway receives HTTP POST requests
- Routes to Lambda function via proxy integration
- Lambda processes event and stores in DynamoDB
- Returns JSON response with processing results

## Technologies Used

### Backend
- **.NET 8**: Modern C# framework for API and Lambda development
- **ASP.NET Core Web API**: RESTful API with controller-based routing
- **C# Language Features**: Async/await, dependency injection, nullable reference types

### AWS Services
- **Lambda**: Serverless compute for event processing
- **API Gateway**: Managed REST API with Lambda proxy integration
- **SQS**: Message queue for event-driven architecture
- **DynamoDB**: NoSQL database for user and event storage
- **CloudWatch**: Logging and monitoring
- **IAM**: Role-based access control

### Development Tools
- **Visual Studio Code**: Primary IDE
- **AWS CLI**: Infrastructure management
- **AWS Lambda Tools**: .NET deployment tooling
- **Git/GitHub**: Version control

## Key Features

### Event-Driven Architecture
- Asynchronous processing via SQS
- Automatic Lambda triggering on queue messages
- Loose coupling between services
- Scalable, decoupled microservices pattern

### Domain-Driven Design
- Clear separation of concerns with domain events (`UserCreatedEvent`)
- Service layer abstractions (`IDynamoDbService`, `ISqsService`)
- Event sourcing approach with audit trail

### Enterprise Integration Patterns
- Mock CRM integration (Salesforce via JSONPlaceholder API)
- Simulated ERP processing (Oracle Fusion/Peoplesoft)
- Pub/Sub pattern for event distribution
- Retry logic and error handling

### Observability
- Structured logging with `ILogger`
- CloudWatch Logs for both API and Lambda
- CloudWatch Metrics for API Gateway performance
- Request/response logging for debugging

## Monitoring

- **CloudWatch Logs**: Full request/response logging enabled
- **CloudWatch Metrics**: Request count, latency, 4xx/5xx errors
- **Log Groups**:
  - API Gateway: `API-Gateway-Execution-Logs_4iu4z3q137/prod`
  - Lambda: `/aws/lambda/ErpProcessorFunction`
  - ASP.NET API: Local logs (when running locally)

## Getting Started

### Prerequisites

- .NET 8 SDK
- AWS Account with configured credentials (`aws configure`)
- AWS CLI installed
- Git

### Local Development

1. **Clone the repository**
```bash
git clone https://github.com/jtich40/event-driven-integration-demo.git
cd event-driven-integration-demo
```

2. **Configure AWS credentials**
```bash
aws configure
# Enter: Access Key, Secret Key, Region (us-east-1), format (json)
```

3. **Update configuration**

Edit `IntegrationApi/appsettings.Development.json`:
```json
{
  "AWS": {
    "Region": "us-east-1",
    "SQS": {
      "QueueUrl": "https://sqs.us-east-1.amazonaws.com/438707381373/IntegrationEventsQueue"
    }
  }
}
```

4. **Run the API locally**
```bash
cd IntegrationApi
dotnet run
# API available at http://localhost:5073
# Swagger UI at http://localhost:5073/swagger
```

5. **Test the endpoint**
```bash
curl -X POST http://localhost:5073/api/crm \
  -H "Content-Type: application/json" \
  -d '{"name":"Test User","email":"test@example.com"}'
```

### AWS Resources Setup

1. **DynamoDB Tables**:
   - `Users` (Partition key: `Id` - String)
   - `ErpProcessedUsers` (Partition key: `EventId` - String)

2. **SQS Queue**:
   - Name: `IntegrationEventsQueue`
   - Type: Standard
   - Copy the Queue URL for configuration

3. **Lambda Function**:
   - Name: `ErpProcessorFunction`
   - Runtime: .NET 8
   - Trigger: SQS (`IntegrationEventsQueue`)
   - Additional trigger: API Gateway (optional for direct invocation)

4. **IAM Roles**:
   - Lambda execution role with DynamoDB and SQS permissions
   - API Gateway CloudWatch logs role

### Deploy Lambda
```bash
cd ErpProcessorLambda
dotnet lambda deploy-function
```

## Project Structure
```
.
├── IntegrationApi/              # ASP.NET Core Web API
│   ├── Controllers/             # API endpoints
│   ├── Models/                  # Domain models and events
│   ├── Services/                # Business logic (DynamoDB, SQS)
│   └── Program.cs               # Application entry point
│
├── IntegrationApi.Tests/        # API unit tests
│   └── Controllers/             # Controller tests
│
├── ErpProcessorLambda/          # AWS Lambda function project
│   ├── src/
│   │   └── ErpProcessorLambda/
│   │       ├── Function.cs      # Lambda handlers (SQS, API Gateway)
│   │       ├── Models/          # Event models
│   │       └── aws-lambda-tools-defaults.json
│   │
│   └── test/
│       └── ErpProcessorLambda.Tests/
│           └── FunctionTest.cs  # Lambda unit tests
│
└── README.md
```

## Testing

### Run All Tests
```bash
# From repository root
dotnet test
```

### Test Coverage
- **IntegrationApi.Tests**: 4 tests covering GET/POST endpoints, validation, and error cases
  - `GetUsers_ReturnsOkResult_WithListOfUsers`
  - `GetUser_WithValidId_ReturnsUser`
  - `GetUser_WithInvalidId_ReturnsNotFound`
  - `CreateUser_SavesUserAndPublishesEvent`

- **ErpProcessorLambda.Tests**: Lambda function tests covering SQS message processing

### Expected Output
```
Test Run Successful.
Total tests: 4+
     Passed: 4+
     Failed: 0
  Duration: < 1 s
```

### Testing Approach
- **Unit tests** with xUnit and Moq for mocking dependencies
- **NullLogger** for test isolation
- **Mock HttpClient** for external API calls
- **Verify** calls to ensure proper service interactions

## CI/CD Pipeline

GitHub Actions workflow automatically:
- ✅ Runs on every push and pull request
- ✅ Builds all projects (.NET 8)
- ✅ Runs full test suite
- ✅ Deploys Lambda to AWS on merge to main
- ✅ Security scanning with GitGuardian

View workflow: [.github/workflows/ci-cd.yml](.github/workflows/ci-cd.yml)

### Pipeline Status
![CI/CD Pipeline](https://github.com/jtich40/event-driven-integration-demo/actions/workflows/ci-cd.yml/badge.svg)

## License

MIT License - feel free to use this as a learning resource or portfolio project.

## Contact

Jared Tichacek - jared.tichacek@gmail.com  
GitHub: [@jtich40](https://github.com/jtich40)
