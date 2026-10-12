# Data model

Tables created in Phase 1 are in the first migration. Tables marked *later* arrive as new
migrations when their phase starts (one migration per feature is a good habit to show in git history).

```mermaid
erDiagram
    ADMIN_USERS {
        uuid id PK
        text email UK
        text password_hash
    }
    PROJECTS ||--o{ ENVIRONMENTS : has
    PROJECTS ||--o{ FLAGS : has
    ENVIRONMENTS ||--o{ SDK_KEYS : has
    FLAGS ||--|{ VARIATIONS : has
    FLAGS ||--o{ FLAG_ENVIRONMENT_CONFIGS : "configured per env"
    ENVIRONMENTS ||--o{ FLAG_ENVIRONMENT_CONFIGS : "configured per env"
    PROJECTS ||--o{ AUDIT_ENTRIES : "logged under"

    PROJECTS { uuid id PK
               text key UK }
    ENVIRONMENTS { uuid id PK
                   uuid project_id FK
                   text key "unique per project" }
    SDK_KEYS { uuid id PK
               uuid environment_id FK
               text key_prefix
               text key_hash UK
               timestamptz revoked_at }
    FLAGS { uuid id PK
            uuid project_id FK
            text key "unique per project"
            text kind
            bigint version "concurrency token"
            timestamptz archived_at }
    VARIATIONS { uuid id PK
                 uuid flag_id FK
                 jsonb value_json
                 int position }
    FLAG_ENVIRONMENT_CONFIGS { uuid id PK
                               uuid flag_id FK
                               uuid environment_id FK
                               bool enabled
                               uuid off_variation_id
                               jsonb fallthrough_json
                               jsonb rules_json
                               bigint version "ruleset version / ETag" }
    AUDIT_ENTRIES { bigint id PK
                    uuid project_id
                    text actor_email
                    text action
                    text resource_type
                    text resource_key
                    jsonb before_json
                    jsonb after_json }
```

## Added in later phases

| Table | Phase | Purpose |
| --- | --- | --- |
| `segments` | 3 | Reusable groups of users referenced from rule clauses |
| `experiments` | 7 | Links a flag + environment + metric, with start/stop times |
| `events` | 7 | Exposure and conversion events sent by SDKs |

## Design notes

- **Targeting lives in jsonb** (`fallthrough_json`, `rules_json`) instead of many small tables. Rules are always read
  and written as a unit, never queried individually, so a document fits better than joins.
- **Rollout weights are basis points** (10000 = 100%) and bucketing will be `hash(flagKey + userKey) mod 10000`.
- **`version` columns** do two jobs: optimistic concurrency now, and the ruleset version/ETag that SDKs and Redis use in Phase 3-4.
- **SDK keys are stored as SHA-256 hashes.** The plaintext key is returned once at creation.
- **The audit log is append-only**: no update or delete endpoints exist. In production, also revoke UPDATE/DELETE on the table
  for the application's database role.
