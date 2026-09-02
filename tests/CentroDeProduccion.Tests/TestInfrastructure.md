# Test infrastructure decision: SQL Server LocalDB

## Why this exists

Every database-backed test in this solution runs against SQL Server LocalDB — the same engine
family the application runs on. There is no SQLite or in-memory provider in the test project.

Three reasons:

1. **SQLite cannot cover the concurrency path.** EF Core's SQLite provider does not implement
   `rowversion`, so the optimistic-concurrency retry on `Insumo` (Slice 6) is untestable there.
   That path is the whole reason the ledger stays consistent, so leaving it uncovered defeats
   the design.
2. **Provider divergence hides bugs.** Decimal precision, collation, and index semantics differ
   between SQLite and SQL Server. A green SQLite suite can pass while production fails.
3. **Supply chain.** The SQLite provider pulls in `SQLitePCLRaw.lib.e_sqlite3`, which carries a
   known high-severity advisory (GHSA-2m69-gcr7-jv3q). Dropping the provider removes it.

## Convention

- Tests that need a real database engine MUST be tagged:

  ```csharp
  [Trait("Category", "SqlServer")]
  ```

- Tests that only inspect the EF Core model (index shapes, property mappings, enum contents)
  need no engine. Build the context with `LocalDb.ModelOnlyOptions()` — it configures the
  SQL Server provider so the model is built, but never opens a connection. These carry no trait
  and run anywhere the .NET SDK is installed.

- Pure unit tests (validators, mappers, domain services) carry no trait and touch no context.

## Fixtures

`Infrastructure/LocalDb.cs` owns the connection details:

- `LocalDb.CreateAsync(name)` drops and recreates a database named
  `CentroDeProduccion.Tests.{name}`, so each tagged test class is isolated and never depends on
  leftover state.
- `LocalDb.DropAsync(db)` removes it afterwards.
- If the `MSSQLLocalDB` instance is unreachable, `CreateAsync` throws an actionable
  `InvalidOperationException` naming the exact remedy, rather than surfacing an opaque
  `SqlException`.

Instance: `(localdb)\MSSQLLocalDB`, verified present on this development machine via
`sqllocaldb info`.

## Running tests

| Command | What it runs | LocalDB required? |
|---|---|---|
| `dotnet test --filter Category!=SqlServer` | Model and unit tests only | No |
| `dotnet test --filter Category=SqlServer` | Database-backed tests only | Yes |
| `dotnet test` | Everything | Yes, for the full suite to pass |

## CI guidance

CI runners without LocalDB MUST filter explicitly with
`dotnet test --filter Category!=SqlServer`, so the skipped coverage is a visible decision rather
than an accident. A Windows runner with LocalDB, or a SQL Server service container with the
connection string in `LocalDb.cs` adjusted, runs the full suite.
