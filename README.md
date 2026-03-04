# Instructions for candidates

This is the .NET version of the Payment Gateway challenge. If you haven't already read this [README.md](https://github.com/cko-recruitment/) on the details of this exercise, please do so now. 

## Template structure
```
src/
    PaymentGateway.Api - a skeleton ASP.NET Core Web API
test/
    PaymentGateway.Api.Tests - an empty xUnit test project
imposters/ - contains the bank simulator configuration. Don't change this

.editorconfig - don't change this. It ensures a consistent set of rules for submissions when reformatting code
docker-compose.yml - configures the bank simulator
PaymentGateway.sln
```
#OVERVIEW

This solution implements a simple payment gateway API capable of:

Processing payments via an acquiring bank

Storing payment results in memory

Retrieving previously processed payments

Masking sensitive card information (only last 4 digits stored)

The solution includes integration tests with a mocked acquiring bank.

#ARCHITECTURE

The controller

Business logic isolated in PaymentsService

Persistence isolated in PaymentsRepository

External dependency isolated in AcquiringBankClient

This separation ensures:

Testability

Clear responsibility boundaries

Easy replacement of infrastructure components

#STORAGE

An in-memory ConcurrentDictionary is used.

Reasons:

Simplicity (as per take-home scope)

Thread safety

No external dependency required

In production this would be replaced with:

A relational database 

With proper indexing on Payment Id

#SENSITIVE DATA HANDLING

Full card number is never stored

Only CardNumberLastFour is persisted

CVV is never stored

No logging of sensitive fields

#BANK INTEGRATION

The acquiring bank is abstracted behind AcquiringBankClient.

In tests:

The bank is mocked via a custom HttpMessageHandler

No Docker dependency required

Full end-to-end HTTP pipeline is tested

This ensures:

Deterministic tests

Faster execution

CI-friendly

#Payment Status Handling

Possible statuses:

Authorized

Declined

Rejected (invalid request)

Enums are serialized as strings for:

Better API clarity

Stronger contract stability

#Validation Strategy

Validation is handled via:

Data annotations

Custom expiry date validation

ModelState automatic 400 response

Invalid requests:

Do not call the acquiring bank

Return HTTP 400

#Testing Strategy

Integration tests cover:

Successful authorization

Declined payment

Bank unavailable (503)

Invalid request (400)

Retrieval of stored payment

404 for unknown payment

The tests validate:

HTTP status codes

Response payload structure

Business behavior

That the bank is not called for invalid requests

##Production Improvements

If this were a production system, I would:

Replace in-memory storage with persistent database

Add idempotency keys to prevent duplicate payments

Implement retry policy (with exponential backoff) for transient bank failures

Add structured logging (Serilog + correlation id)

Add OpenTelemetry tracing

Secure endpoints (authentication + authorization)

Add rate limiting

Add circuit breaker (e.g. Polly)

Introduce proper error contract (problem details mapping)

Add container health checks




##How to Run

Start acquiring bank simulator:

docker compose up

Run API:

dotnet run --project src/PaymentGateway.Api

Run tests:

dotnet test

json example for test 
Cas Authorized

{
  "cardNumber": "2222405343248877",
  "expiryMonth": 4,
  "expiryYear": 2030,
  "currency": "GBP",
  "amount": 100,
  "cvv": "123"
}

Cas Declined

{
  "cardNumber": "2222405343248878",
  "expiryMonth": 4,
  "expiryYear": 2030,
  "currency": "GBP",
  "amount": 100,
  "cvv": "123"
}

Cas Bank Down
{
  "cardNumber": "2222405343248870",
  "expiryMonth": 4,
  "expiryYear": 2030,
  "currency": "GBP",
  "amount": 100,
  "cvv": "123"
}

Cas Rejected

{
  "cardNumber": "2222405343248870",
  "expiryMonth": 4,
  "expiryYear": 2020,
  "currency": "GBP",
  "amount": 100,
  "cvv": "123"
}