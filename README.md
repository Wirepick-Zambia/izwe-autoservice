# Izwe Auto SMS Service

Automated SMS delivery system for **Izwe Loans**. Processes pipe-delimited CSV files from country-specific folders, stores records in a database, sends messages through the WirePick SMS gateway, and tracks delivery status — all on a configurable schedule.

Includes a **React monitoring portal** for real-time visibility into message status, processing logs, and full configuration management (SMTP alerts, gateway settings, processing intervals).

---

## Table of Contents

- [Architecture](#architecture)
- [Business Logic](#business-logic)
- [Project Structure](#project-structure)
- [Database Schema](#database-schema)
- [API Endpoints](#api-endpoints)
- [Frontend Portal](#frontend-portal)
- [Configuration](#configuration)
- [Getting Started](#getting-started)
- [Switching Database Providers](#switching-database-providers)
- [CSV File Format](#csv-file-format)
- [Email Alerts](#email-alerts)

---

## Architecture

The backend follows **Clean Architecture** with four layers. Dependencies point inward — outer layers depend on inner layers, never the reverse.

```
┌─────────────────────────────────────────────────┐
│                    API Layer                     │
│   Controllers, BackgroundService, Program.cs     │
│   Serves the React SPA from wwwroot             │
├─────────────────────────────────────────────────┤
│              Infrastructure Layer                │
│   EF Core DbContext, Repositories,              │
│   WirePick gateway, SMTP alerts, CSV parser     │
├─────────────────────────────────────────────────┤
│              Application Layer                   │
│   Services, DTOs, Interface contracts           │
│   (ISmsGateway, IEmailAlertService, etc.)       │
├─────────────────────────────────────────────────┤
│                Domain Layer                      │
│   Entities, Enums, Repository interfaces        │
│   Zero external dependencies                    │
└─────────────────────────────────────────────────┘
```

**Key design decisions:**

- **Domain** has no NuGet dependencies — pure C# entities and interfaces.
- **Application** defines service logic and interface contracts. Infrastructure implements them.
- **Infrastructure** owns all I/O: database, HTTP, SMTP, filesystem.
- **API** wires everything together via DI and hosts the background processing job.

---

## Business Logic

### Processing Pipeline

The `SmsProcessingJob` background service runs on a configurable interval (default: every 5 minutes). Each cycle executes a three-step pipeline inside `SmsProcessingService.ProcessAsync()`:

```
Step 1: Poll         Step 2: Parse & Store       Step 3: Send
───────────────      ────────────────────        ──────────────
Scan country         Read pipe-delimited         Fetch pending
folders for          CSV files, create           records from DB,
new .csv files  ──►  SmsRecord entities,    ──►  call WirePick
                     insert in chunked            API per record,
Move files to        batches (5K per              batch-update
/pending/            commit)                      all statuses
                                                  in single commit
                     Move files to
                     /processed/
```

#### Step 1 — File Polling (`CsvFileProcessor.PollForFiles`)

Scans the configured base folder for country subdirectories. For each country directory:

1. Skips `pending/` and `processed/` directories.
2. Creates a `pending/` subdirectory if it doesn't exist.
3. Moves all `*.csv` files from the country root into `pending/`.
4. Returns the list of moved file paths.

**Folder structure:**

```
/data/sms/                    ← BaseFolderPath (configurable)
├── Ghana/
│   ├── batch_001.csv         ← Picked up, moved to pending/
│   ├── pending/
│   │   └── batch_001.csv     ← Awaiting parse
│   └── processed/
│       └── 20260328_103000_batch_001.csv  ← Done
├── Kenya/
│   └── ...
└── SouthAfrica/
    └── ...
```

#### Step 2 — Parse & Store (`CsvFileProcessor.ParseCsvFile` + `SmsRepository.AddBatchAsync`)

For each file in `pending/`:

1. Streams lines using `yield return` (no full-file memory load).
2. Splits each line by pipe (`|`) delimiter.
3. Creates an `SmsRecord` entity per valid line.
4. Inserts into the database in chunks of 5,000 records per `SaveChangesAsync` call.
5. Moves the file to `processed/` with a timestamp prefix.

The country is extracted from the folder path (e.g., `Ghana/pending/file.csv` → country = `Ghana`).

A **unique constraint** on `(ContractId, PhoneNumber, SourceFile)` prevents duplicate records if a file is accidentally reprocessed.

#### Step 3 — Send SMS (`SmsProcessingService` + `WirePickSmsGateway`)

1. Fetches up to `SmsBatchSize` (default: 500) pending records, ordered oldest-first.
2. For each record, calls the WirePick HTTP API:
   ```
   GET {ApiUrl}?client={ClientId}&password={ApiPassword}&phone={Phone}&text={Message}&from={SenderId}
   ```
3. Parses the XML response to extract `status`, `messageid`, and `cost`.
4. Marks the record as `Sent` or `Failed` with the API response details.
5. After processing all records, commits **all updates in a single `SaveChangesAsync`** call (not per-record).
6. Logs the run to the `processing_logs` table.
7. If any failures occurred, sends an email alert (non-blocking — alert failures don't stop processing).

### Concurrency Protection

A database-level **distributed lock** (`processing_locks` table) prevents multiple instances from processing simultaneously:

1. Before each cycle, the job attempts an atomic `UPDATE...WHERE` to acquire the `SmsProcessing` lock.
2. The lock is only acquired if it's unheld or expired (10-minute safety expiry).
3. The lock is released in a `finally` block after processing completes.
4. If another instance holds the lock, the cycle is skipped with a debug log.

The lock holder is identified by `{MachineName}-{ProcessId}`.

---

## Project Structure

```
izwe-autoservice/
├── IzweAutoService.sln
├── README.md
├── .gitignore
│
├── client-app/                                 # React + Vite + TypeScript
│   ├── package.json
│   ├── vite.config.ts                          # Builds to Api/wwwroot, dev proxy to :5000
│   └── src/
│       ├── api/client.ts                       # Typed API client for all endpoints
│       ├── components/Layout.tsx               # Sidebar navigation + page layout
│       └── pages/
│           ├── Dashboard.tsx                   # Stats, countries, recent processing logs
│           ├── Messages.tsx                    # Paginated SMS list with filters
│           ├── Logs.tsx                        # Processing run history
│           └── Settings.tsx                    # SMTP, alerts, gateway, general config
│
└── src/
    ├── IzweAutoService.Domain/                 # Zero dependencies
    │   ├── Entities/
    │   │   ├── SmsRecord.cs                    # Core SMS message entity
    │   │   ├── AppSetting.cs                   # Key-value configuration store
    │   │   ├── ProcessingLog.cs                # Per-cycle run log
    │   │   └── ProcessingLock.cs               # Distributed lock row
    │   ├── Enums/
    │   │   └── SmsStatus.cs                    # Pending = 0, Sent = 1, Failed = 2
    │   └── Interfaces/
    │       ├── ISmsRepository.cs
    │       ├── ISettingsRepository.cs
    │       ├── IProcessingLogRepository.cs
    │       └── IProcessingLockRepository.cs
    │
    ├── IzweAutoService.Application/            # Depends on Domain only
    │   ├── Interfaces/
    │   │   ├── ISmsGateway.cs                  # SMS send contract + SmsGatewayResult record
    │   │   ├── IEmailAlertService.cs           # Alert send contract
    │   │   └── IFileProcessor.cs               # File poll/parse/move contract
    │   ├── DTOs/
    │   │   ├── DashboardDto.cs                 # Dashboard + ProcessingLogDto
    │   │   ├── SmsRecordDto.cs                 # SmsRecordDto + SmsPagedResult
    │   │   └── SettingsDto.cs                  # SettingsDto + UpdateSettingsRequest
    │   └── Services/
    │       ├── SmsProcessingService.cs         # Main 3-step processing pipeline
    │       ├── SmsQueryService.cs              # Paged SMS queries for the portal
    │       ├── DashboardService.cs             # Dashboard aggregation
    │       └── SettingsService.cs              # Settings CRUD
    │
    ├── IzweAutoService.Infrastructure/         # Depends on Application
    │   ├── Data/
    │   │   └── AppDbContext.cs                 # EF Core context, indexes, seed data
    │   ├── Migrations/                         # Auto-generated EF Core migrations
    │   ├── Repositories/
    │   │   ├── SmsRepository.cs                # Chunked inserts, batch updates, range queries
    │   │   ├── SettingsRepository.cs           # Bulk upsert (single query fetch)
    │   │   ├── ProcessingLogRepository.cs
    │   │   └── ProcessingLockRepository.cs     # Atomic SQL lock acquire/release
    │   ├── Services/
    │   │   ├── WirePickSmsGateway.cs           # HTTP + XML response parsing
    │   │   ├── SmtpEmailAlertService.cs        # MailKit SMTP with safe resource handling
    │   │   └── CsvFileProcessor.cs             # Streaming CSV parser with yield return
    │   └── DependencyInjection.cs              # AddInfrastructure() extension method
    │
    └── IzweAutoService.Api/                    # Depends on Application + Infrastructure
        ├── Program.cs                          # Startup: DI, auto-migrate, SPA serving
        ├── appsettings.json                    # Connection string, provider, logging
        ├── BackgroundServices/
        │   └── SmsProcessingJob.cs             # Scheduled loop with distributed lock
        └── Controllers/
            ├── DashboardController.cs          # GET /api/dashboard
            ├── SmsController.cs                # GET /api/sms, POST /api/sms/process
            ├── SettingsController.cs           # GET/PUT /api/settings
            └── LogsController.cs               # GET /api/logs
```

---

## Database Schema

### Tables

#### `sms_records`

| Column           | Type      | Nullable | Description                          |
|------------------|-----------|----------|--------------------------------------|
| Id               | INTEGER   | PK       | Auto-increment primary key           |
| ContractId       | TEXT      | No       | Loan contract identifier             |
| PhoneNumber      | TEXT      | No       | Recipient phone number               |
| MessageContent   | TEXT      | No       | SMS message body                     |
| MessageTimestamp  | TEXT      | Yes      | Timestamp from the CSV file          |
| Country          | TEXT      | No       | Derived from folder name             |
| SenderId         | TEXT      | No       | SMS sender ID (e.g., "IzweLoans")    |
| ClientId         | TEXT      | No       | API client ID (e.g., "WireGhana")    |
| SourceFile       | TEXT      | Yes      | Original CSV filename                |
| Status           | INTEGER   | No       | 0=Pending, 1=Sent, 2=Failed         |
| ApiMessageId     | TEXT      | Yes      | Message ID from WirePick response    |
| ApiStatus        | TEXT      | Yes      | Status code from WirePick response   |
| ApiCost          | TEXT      | Yes      | Cost from WirePick response          |
| ErrorMessage     | TEXT(500) | Yes      | Error details (truncated to 500)     |
| CreatedAt        | TEXT      | No       | UTC timestamp when record was created|
| ProcessedAt      | TEXT      | Yes      | UTC timestamp when SMS was sent      |

**Indexes:**

| Index Name                                          | Columns                              | Unique |
|-----------------------------------------------------|--------------------------------------|--------|
| `IX_sms_records_Status_CreatedAt`                   | (Status, CreatedAt)                  | No     |
| `IX_sms_records_Country`                            | Country                              | No     |
| `IX_sms_records_ContractId`                         | ContractId                           | No     |
| `IX_sms_records_PhoneNumber`                        | PhoneNumber                          | No     |
| `IX_sms_records_ProcessedAt`                        | ProcessedAt                          | No     |
| `IX_sms_records_ContractId_PhoneNumber_SourceFile`  | (ContractId, PhoneNumber, SourceFile)| Yes    |

The composite `(Status, CreatedAt)` index covers the most frequent query: "get oldest pending records". The unique composite index prevents duplicate records from reprocessed files.

#### `app_settings`

| Column   | Type    | Nullable | Description                |
|----------|---------|----------|----------------------------|
| Id       | INTEGER | PK       | Auto-increment primary key |
| Key      | TEXT    | No       | Setting key (unique)       |
| Value    | TEXT    | No       | Setting value              |
| Category | TEXT    | No       | Grouping category          |

**Indexes:** Unique on `Key`, non-unique on `Category`.

**Seed data:**

| Key                 | Value                                  | Category |
|---------------------|----------------------------------------|----------|
| BaseFolderPath      | /data/sms                              | General  |
| CronIntervalMinutes | 5                                      | General  |
| SmsBatchSize        | 500                                    | General  |
| SenderId            | IzweLoans                              | Sms      |
| ClientId            | WireGhana                              | Sms      |
| ApiUrl              | https://api.wirepick.com/httpsms/send  | Sms      |
| ApiPassword         | *(empty)*                              | Sms      |
| SmtpHost            | *(empty)*                              | Smtp     |
| SmtpPort            | 587                                    | Smtp     |
| SmtpUsername        | *(empty)*                              | Smtp     |
| SmtpPassword        | *(empty)*                              | Smtp     |
| SmtpUseSsl          | true                                   | Smtp     |
| AlertEmailFrom      | *(empty)*                              | Alerts   |
| AlertEmailTo        | *(empty)*                              | Alerts   |
| AlertsEnabled       | false                                  | Alerts   |

All settings are configurable from the portal's Settings page.

#### `processing_logs`

| Column       | Type    | Nullable | Description                       |
|--------------|---------|----------|-----------------------------------|
| Id           | INTEGER | PK       | Auto-increment primary key        |
| StartedAt    | TEXT    | No       | UTC start time of processing run  |
| CompletedAt  | TEXT    | Yes      | UTC end time (null if running)    |
| TotalFound   | INTEGER | No       | Records parsed + pending fetched  |
| TotalSent    | INTEGER | No       | Successfully sent count           |
| TotalFailed  | INTEGER | No       | Failed send count                 |
| ErrorMessage | TEXT    | Yes      | Top-level error if pipeline fails |

**Indexes:** Non-unique on `StartedAt`.

#### `processing_locks`

| Column     | Type    | Nullable | Description                         |
|------------|---------|----------|-------------------------------------|
| Id         | INTEGER | PK       | Auto-increment primary key          |
| LockName   | TEXT    | No       | Lock identifier (unique)            |
| HeldBy     | TEXT    | Yes      | "{MachineName}-{ProcessId}" or null |
| AcquiredAt | TEXT    | Yes      | UTC time lock was acquired          |
| ExpiresAt  | TEXT    | Yes      | UTC time lock auto-expires          |

**Indexes:** Unique on `LockName`.

Seeded with one row: `LockName = "SmsProcessing"`.

---

## API Endpoints

| Method | Route                   | Description                         | Request                                               | Response              |
|--------|-------------------------|-------------------------------------|-------------------------------------------------------|-----------------------|
| GET    | `/api/dashboard`        | Dashboard stats and recent logs     | —                                                     | `DashboardDto`        |
| GET    | `/api/sms`              | Paginated SMS records               | `?page=1&pageSize=25&status=0&country=Ghana&search=…` | `SmsPagedResult`      |
| POST   | `/api/sms/process`      | Manually trigger a processing cycle | —                                                     | `{ message: string }` |
| GET    | `/api/settings`         | All settings as key-value map       | —                                                     | `SettingsDto`         |
| GET    | `/api/settings/{cat}`   | Settings filtered by category       | —                                                     | `SettingsDto`         |
| PUT    | `/api/settings`         | Update settings by category         | `{ category: string, settings: { key: value, … } }`  | `{ message: string }` |
| GET    | `/api/logs`             | Recent processing logs              | `?count=20`                                           | `ProcessingLogDto[]`  |

### Query Parameters for `GET /api/sms`

| Parameter  | Type   | Default | Description                                  |
|------------|--------|---------|----------------------------------------------|
| page       | int    | 1       | Page number (1-based)                        |
| pageSize   | int    | 25      | Records per page                             |
| status     | int?   | null    | Filter: 0=Pending, 1=Sent, 2=Failed         |
| country    | string?| null    | Filter by country name                       |
| search     | string?| null    | Search in PhoneNumber or ContractId          |

---

## Frontend Portal

The React portal is built with Vite + TypeScript and served by the .NET backend from `wwwroot/`.

### Pages

| Route        | Page        | Description                                                        |
|--------------|-------------|--------------------------------------------------------------------|
| `/`          | Dashboard   | Stats cards (pending/sent/failed/today), active countries, recent processing log table with duration and status badges. "Run Now" button to manually trigger a cycle. |
| `/messages`  | Messages    | Paginated table of all SMS records. Filter by status dropdown, country, and free-text search on phone/contract. Shows contract, phone, country, truncated message, status badge, created/processed timestamps. |
| `/logs`      | Logs        | Table of processing run history (last 50). Columns: ID, started, completed, found, sent, failed, error. Error rows are highlighted. |
| `/settings`  | Settings    | Grouped configuration forms. Each group has its own Save button. Groups: **General** (base folder, interval, batch size), **SMS Gateway** (API URL, client ID, sender ID, password), **SMTP** (host, port, username, password, SSL toggle), **Email Alerts** (enabled toggle, from/to addresses). |

### Development

During development, the Vite dev server runs on `:5173` and proxies `/api` requests to `http://localhost:5000` (the .NET backend).

---

## Configuration

### `appsettings.json`

```json
{
  "DatabaseProvider": "sqlite",
  "ConnectionStrings": {
    "Default": "Data Source=izwe_sms.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

### Runtime Settings (via portal or database)

All operational settings are stored in the `app_settings` table and editable from the Settings page:

| Category | Settings                                                |
|----------|---------------------------------------------------------|
| General  | BaseFolderPath, CronIntervalMinutes, SmsBatchSize       |
| Sms      | ApiUrl, ClientId, SenderId, ApiPassword                 |
| Smtp     | SmtpHost, SmtpPort, SmtpUsername, SmtpPassword, SmtpUseSsl |
| Alerts   | AlertsEnabled, AlertEmailFrom, AlertEmailTo             |

Settings changes take effect on the **next processing cycle** — no restart required.

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)
- [pnpm](https://pnpm.io/) (`npm install -g pnpm`)

### Development

A single command starts both the .NET backend and the Vite dev server with hot-reload:

```bash
pnpm install
pnpm dev
```

This runs:
- `[api]` .NET backend on `http://localhost:5000`
- `[web]` Vite frontend on `http://localhost:5173` (proxies `/api` to `:5000`)

Open `http://localhost:5173` in your browser.

### Build for Production

```bash
pnpm build       # Build frontend to wwwroot
pnpm publish     # Build frontend + publish .NET to ./publish/
```

---

## Deployment

### Windows Server — Option A: Windows Service (recommended)

Runs as a background service without a user logged in.

```powershell
# 1. Build and publish
pnpm publish

# 2. Copy to the server
Copy-Item -Recurse .\publish\ C:\izwe-sms\

# 3. Configure the database (edit C:\izwe-sms\appsettings.json)
#    Set DatabaseProvider and ConnectionStrings as needed (see "Switching Database Providers" below)

# 4. Create the SMS folder structure
New-Item -ItemType Directory -Force -Path C:\SFTP\SMS_Automation\Ghana
New-Item -ItemType Directory -Force -Path C:\SFTP\SMS_Automation\Kenya
New-Item -ItemType Directory -Force -Path C:\SFTP\SMS_Automation\SouthAfrica

# 5. Install and start the Windows Service
sc.exe create IzweSmsService binPath="C:\izwe-sms\IzweAutoService.Api.exe --urls http://0.0.0.0:5000" start=auto
sc.exe start IzweSmsService
```

To check status: `sc.exe query IzweSmsService`

To uninstall:
```powershell
sc.exe stop IzweSmsService
sc.exe delete IzweSmsService
```

### Windows Server — Option B: IIS

1. Install the [ASP.NET Core Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/10.0) on the server.
2. Run `pnpm publish` on your build machine.
3. Copy the `./publish/` folder to the server (e.g., `C:\inetpub\izwe-sms`).
4. In IIS Manager, create a new site pointing to that folder.
5. Set the Application Pool to **No Managed Code**.
6. The included `web.config` handles in-process hosting automatically.

### Post-deployment Configuration

Both options auto-migrate the database on startup. Once the app is running, open the portal and configure:

| Settings Page Section | What to set                                           |
|-----------------------|-------------------------------------------------------|
| **General**           | `BaseFolderPath` → `C:\SFTP\SMS_Automation`           |
| **SMS Gateway**       | API URL, Client ID, Sender ID, API password           |
| **SMTP**              | Mail server host, port, credentials, SSL              |
| **Email Alerts**      | Toggle on, set From/To addresses                      |

Settings take effect on the next processing cycle — no restart required.

---

## Switching Database Providers

The application supports **SQLite** (default), **PostgreSQL**, and **MySQL**. Change the provider in `appsettings.json`:

### PostgreSQL

```json
{
  "DatabaseProvider": "postgresql",
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=izwe_sms;Username=postgres;Password=yourpassword"
  }
}
```

### MySQL

```json
{
  "DatabaseProvider": "mysql",
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=izwe_sms;User=root;Password=yourpassword"
  }
}
```

### SQLite (default)

```json
{
  "DatabaseProvider": "sqlite",
  "ConnectionStrings": {
    "Default": "Data Source=izwe_sms.db"
  }
}
```

The database is auto-migrated on startup. When switching providers, the new database will be created and seeded automatically.

---

## CSV File Format

Files must be placed in country subdirectories under the configured `BaseFolderPath`. Format is pipe-delimited with no header row:

```
ContractId|PhoneNumber|MessageContent|MessageTimestamp
```

**Example** (`/data/sms/Ghana/batch_001.csv`):

```
GH-2024-001|+233201234567|Your loan payment of GHS 500 is due on 2024-04-01.|2024-03-28T10:00:00
GH-2024-002|+233209876543|Your loan application has been approved.|2024-03-28T10:00:00
```

- **ContractId** — Required. Loan contract reference.
- **PhoneNumber** — Required. Full international phone number.
- **MessageContent** — Required. SMS body text.
- **MessageTimestamp** — Optional. Timestamp from the source system.

Lines with fewer than 3 pipe-separated fields are skipped with a warning log.

---

## Email Alerts

Email alerts are **disabled by default** and are non-blocking — alert failures never stop SMS processing.

To enable:

1. Open the portal Settings page.
2. Under **SMTP Configuration**, enter your mail server details.
3. Under **Email Alerts**, toggle "Enable Alerts" on and set the From/To addresses.
4. Click Save on both sections.

Alerts are sent when:

- A processing cycle completes with **one or more failed** SMS sends.
- The **entire processing pipeline** throws an unhandled exception.

Multiple recipients are supported — separate email addresses with semicolons in the "To" field.

---

## Tech Stack

| Component      | Technology                                         |
|----------------|----------------------------------------------------|
| Backend        | .NET 10, ASP.NET Core, C# 13                      |
| ORM            | Entity Framework Core 10                           |
| Database       | SQLite (default), PostgreSQL, MySQL                |
| SMS Gateway    | WirePick HTTP API (XML responses)                  |
| Email          | MailKit (SMTP)                                     |
| Frontend       | React 19, TypeScript, Vite 8                       |
| Routing        | React Router 7                                     |
