# climanager

A .NET 8 command-line tool for Google Drive: authenticate with OAuth 2.0, sync files locally in parallel, search by name, and upload files to folder paths on Drive.

Built with **Clean Architecture**, **CQRS (MediatR)**, the **Result** pattern, and **Spectre.Console.Cli**.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- A Google Cloud project with the **Google Drive API** enabled
- An OAuth 2.0 **Desktop** client credentials file (`client_secret.json`)
- Your Google account added as a **test user** on the OAuth consent screen (while the app is in testing mode)

## Where to place `client_secret.json`

Place the downloaded credentials file in the **repository root**, next to `CliManager.sln`:

```
climanager/
├── CliManager.sln
├── client_secret.json    ← put it here
├── Downloads/
├── CliManager/
├── Application/
├── Infrastructure/
└── ...
```

The path is configured in `CliManager/appsettings.json` as `GoogleAuth:ClientSecretPath` (`client_secret.json`). The app resolves it relative to the repo root (found by walking up to `CliManager.sln`).

> **Do not commit** `client_secret.json`. It is listed in `.gitignore`.

## Build and run

Clone the repository, add `client_secret.json` as above, then from the **repository root**:

```bash
dotnet build
dotnet run --project CliManager -- auth
dotnet run --project CliManager -- sync
dotnet run --project CliManager -- search "report"
dotnet run --project CliManager -- upload ./Downloads/myfile.txt CliManager
```

### Commands

| Command | Description |
|---------|-------------|
| `auth` | Sign in with Google (OAuth 2.0). Tokens are saved under `.climanager/tokens/`. |
| `sync` | Download files from Google Drive into `Downloads/` (parallel, with statistics). |
| `search <query>` | Search Drive by name (files and folders). Unsynced files show `[Not Downloaded]`. |
| `upload <local_path> <drive_path>` | Upload a local file. Creates missing Drive folders (e.g. `Projects/Reports`). Use `""` for My Drive root. |

### Example: upload

```bash
# Upload to My Drive/CliManager/
dotnet run --project CliManager -- upload ./Downloads/test CliManager

# Upload to My Drive root
dotnet run --project CliManager -- upload ./Downloads/test ""
```

### Debugging (VS Code)

Launch profiles are in `.vscode/launch.json` (`auth`, `sync`, `search`, `upload`). Select a profile and press **F5**. Output appears in the **Terminal** tab.

Run `auth` once before other commands so OAuth tokens exist.

## Repository layout (runtime)

All paths are resolved from the repo root (same for every clone):

```
climanager/
├── client_secret.json       # you provide (gitignored)
├── Downloads/                 # synced / local files (contents gitignored)
└── .climanager/             # created on first run (gitignored)
    ├── climanager.db        # sync manifest (SQLite)
    └── tokens/              # OAuth tokens
```

| Setting | Location |
|---------|----------|
| `Sync:DownloadsPath` → `Downloads` | `{repo}/Downloads/` |
| `ConnectionStrings:DefaultConnection` | `{repo}/.climanager/climanager.db` |
| `GoogleAuth:TokenStorePath` | `{repo}/.climanager/tokens/` |

## Configuration

`CliManager/appsettings.json`:

```json
{
  "Sync": {
    "DownloadsPath": "Downloads",
    "MaxDegreeOfParallelism": 4
  },
  "GoogleAuth": {
    "ClientSecretPath": "client_secret.json",
    "TokenStorePath": ".climanager/tokens"
  }
}
```

## Architecture

The solution follows **Clean Architecture** (dependency rule: inner layers do not depend on outer layers).

```
┌─────────────────────────────────────────────────────────┐
│  CliManager (Presentation / Composition Root)           │
│  Spectre.Console.Cli commands, DI host, ResultConsole   │
└────────────────────────┬────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────┐
│  Application (Use cases)                                  │
│  Commands & queries (MediatR), handlers, repository ports │
└────────┬───────────────────────────────┬────────────────┘
         │                               │
┌────────▼────────┐            ┌─────────▼─────────┐
│  Domain         │            │  SharedKernel      │
│  Entities       │            │  Result, ICommand, │
│  DriveFile,     │            │  IQuery            │
│  SyncEntry      │            └────────────────────┘
└─────────────────┘
                         ▲
┌────────────────────────┴────────────────────────────────┐
│  Infrastructure (Adapters)                                │
│  Google Drive API, OAuth, SQLite (EF Core), repositories  │
└───────────────────────────────────────────────────────────┘
```

### Patterns used

| Pattern | Role in this project |
|---------|----------------------|
| **Clean Architecture** | Separates CLI, business logic, and Google/SQLite adapters. |
| **CQRS + MediatR** | `auth` / `sync` / `upload` are commands; `search` is a query. Handlers live in Application. |
| **Repository** | `IDriveFileRepository`, `ISyncEntryRepository` abstract Drive and manifest access. |
| **Unit of Work** | `IUnitOfWork` coordinates `SaveChangesAsync` after manifest updates. |
| **Result** | Handlers return `Result` / `Result<T>` instead of throwing for expected failures. |
| **Options** | `SyncOptions`, `GoogleAuthOptions` from `appsettings.json`. |

### Parallel downloads (`sync`)

File downloads use **`Parallel.ForEachAsync`** with a configurable degree of parallelism (`Sync:MaxDegreeOfParallelism`, default **4**). This limits concurrent HTTP calls to Google Drive while still using multiple cores/connections efficiently.

```csharp
var parallelOptions = new ParallelOptions
{
    MaxDegreeOfParallelism = Math.Max(1, syncOptions.Value.MaxDegreeOfParallelism),
    CancellationToken = cancellationToken,
};

await Parallel.ForEachAsync(files, parallelOptions, async (file, ct) =>
{
    // download + update manifest per file
});
```

**Thread safety:**

- **Statistics** (`successful`, `skipped`, `failed`) are updated with **`Interlocked.Increment`**, so parallel tasks never race on shared counters.
- **EF Core `DbContext` is not thread-safe**, so each parallel task creates its own DI scope (`IServiceScopeFactory.CreateScope()`), resolves `ISyncEntryRepository` and `IUnitOfWork`, and calls `SaveChangesAsync` inside that scope after a successful download.
- A failed download in one task does not stop the others; failures are counted, logged with a mapped error message, and reported in the final summary.

### Sync behaviour (summary)

- Lists all non-folder files from Drive.
- Skips unchanged files by comparing Drive `modifiedTime` (from the API) with the local file’s last-write time (set after download).
- Re-downloads and overwrites when Drive content is newer.
- Removes local files that were deleted on Drive.
- Prints: total, downloaded, skipped, failed, removed, elapsed time.

### Search state (`search`)

Whether a file is downloaded is determined from the **SQLite manifest** (`SyncEntry`) plus **`File.Exists`** on the stored local path. Files without a manifest row or missing on disk are printed with **`[Not Downloaded]`**.

### Error handling

`DriveCommandResults` maps exceptions to `Result` messages (rate limiting **429**, network errors, auth **401/403**, invalid paths, etc.) in one place. Handlers use a single `catch` and the CLI displays errors via `ResultConsole`.

## Solution structure

| Project | Responsibility |
|---------|----------------|
| `CliManager` | CLI entry point, Spectre commands, composition root |
| `CliManager.Application` | Commands, queries, handlers, ports |
| `CliManager.Domain` | `DriveFile`, `SyncEntry`, domain helpers |
| `CliManager.Infrastructure` | Google Drive API, OAuth, EF Core SQLite |
| `SharedKernel` | `Result`, `ICommand`, `IQuery` |

## Database

SQLite is created automatically on startup (`EnsureCreated`). No EF migrations. If the schema changes during development, delete `.climanager/climanager.db` and run again.

## Test plan

- [ ] `auth` — browser sign-in; tokens under `.climanager/tokens/`
- [ ] `sync` (first run) — files appear in `Downloads/`
- [ ] `sync` (second run, no Drive changes) — mostly **Skipped (unchanged)**
- [ ] Edit a file on Drive → `sync` re-downloads it
- [ ] Delete a file on Drive → `sync` removes the local copy
- [ ] `search <name>` — results; `[Not Downloaded]` for files not synced
- [ ] `upload <local> <drive_path>` — file appears in the target Drive folder

## NuGet packages (main)

- `Google.Apis.Drive.v3` — Google Drive API
- `Google.Apis.Auth` — OAuth 2.0
- `MediatR` — CQRS dispatch
- `Spectre.Console.Cli` — CLI parsing and output
- `Microsoft.EntityFrameworkCore.Sqlite` — local sync manifest