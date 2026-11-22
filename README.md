# Honey House 🍯

E-commerce platform for natural honey and bee products in Egypt.

- **Backend:** ASP.NET Core Web API (.NET 10), EF Core, SQL Server, Clean Architecture, JWT
- **Admin:** ASP.NET Core MVC (Phase 3)
- **Mobile:** Flutter (Phase 4)

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) and [DEVELOPMENT_PROGRESS.md](DEVELOPMENT_PROGRESS.md).

## Run locally

```bash
docker compose up -d            # SQL Server on localhost:1433
dotnet build
dotnet test
dotnet run --project src/HoneyHouse.API
```
