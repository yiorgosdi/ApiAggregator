Το αναλυτικό documentation, Documentation_for_AggregatorAPI.docx, βρίσκεται στο φάκελο /docs: 
	https://github.com/yiorgosdi/ApiAggregator/tree/main/docs 

*********

# ApiAggregator

Το **ApiAggregator** είναι μια .NET 8/9 εφαρμογή που συνδέεται με εξωτερικές πηγές (HackerNews, GitHub, Open-Meteo) και επιστρέφει ενοποιημένα αποτελέσματα μέσω ενός ενιαίου endpoint.

## ✨ Features
- Ενοποίηση αποτελεσμάτων από πολλαπλά APIs
- Resilience με Polly (timeouts, retries, circuit breaker)
- In-memory caching με TTL
- Monitoring stats (hits, misses, errors)
- (Optional) JWT Bearer Authentication για secured access
- Swagger UI για δοκιμή endpoints

## 🏗️ Architecture
- **AggregateService**: core service που ενώνει τα αποτελέσματα  
- **IApiConnector**: abstraction για connectors  
- Connectors: `HnApiConnectorAdapter`, `GhApiConnectorAdapter`, `MeteoApiConnectorAdapter`  
- Infrastructure: Caching, Resilience, Stats  

## 🚀 Setup & Run

**Prerequisites**
- .NET 8/9 SDK
- (Optional) Docker για integration με εξωτερικά APIs

**Run**
```bash
dotnet build
dotnet run --project Aggregator.Api
