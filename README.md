# FlagForge

Feature flag and A/B testing platform: admin dashboard, low-latency evaluation API,
TypeScript SDK, audit log, and a demo shop.

## Layout

| Path | What | Phase |
| --- | --- | --- |
| `apps/api` | ASP.NET Core API (Postgres, Redis later) | 1-4 |
| `packages/sdk` | TypeScript SDK published to npm | 5 |
| `apps/dashboard` | React admin dashboard | 6 |
| `apps/demo-shop` | Demo store using the SDK | 8 |
| `docs` | Design notes and data model | all |

## Run the API locally

```bash
docker compose up -d                       # Postgres + Redis
cd apps/api
dotnet tool install --global dotnet-ef     # once
dotnet ef migrations add InitialCreate -o Data/Migrations
dotnet run                                 # http://localhost:5080
```

In Development the API applies migrations on startup and seeds an admin user from
`appsettings.Development.json` (admin@example.com / ChangeMe123!). Open `apps/api/requests.http`
in VS Code (REST Client extension) to try the endpoints.
