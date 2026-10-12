# FlagForge(Phase 1 Development)

Feature flag and A/B testing platform: admin dashboard, low-latency evaluation API,
TypeScript SDK, audit log, and a demo shop.

The platform is currently in the first phase of development which involves:
- Seting up a monorepo with apps/api, apps/dashboard, apps/demo-shop, packages/sdk, and docs/.
- Writing the data model: Project, Environment, Flag, Variation, Rule, Segment, SdkKey, AuditEntry, Experiment, Event.
- Definition the API contract in OpenAPI and set up CI from day one.

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
