# DBProvisioner POC

## Overview

This repository contains two related proof-of-concept apps:

1. `DbProvisioner`
   Creates the database and runs SQL scripts from the `Scripts` folder.

2. `GraphQLGrpcDemo.Api`
   Exposes REST and GraphQL endpoints over the seeded SQL Server data, including paginated reads, streaming, and asynchronous export jobs for large datasets.

---

## Projects

### `DbProvisioner`

**Purpose:**
- creates the target database if it does not exist
- runs `.sql` scripts in filename order
- continues executing later scripts even if one script fails

**Current script behavior:**
- each script is executed independently
- a failed script is logged
- remaining scripts still continue
- a final summary lists failed script names

**Main files:**
- `Program.cs`
- `appsettings.json`
- `Scripts/*`

### `GraphQLGrpcDemo.Api`

**Purpose:**
- exposes user and order data through HTTP APIs
- supports paginated reads for normal UI usage
- supports streamed reads for progressive large downloads
- supports asynchronous export jobs for enterprise-style long-running downloads

**Main files:**
- `GraphQLGrpcDemo.Api/Program.cs`
- `GraphQLGrpcDemo.Api/Controllers/UserController.cs`
- `GraphQLGrpcDemo.Api/Data/UserRepository.cs`
- `GraphQLGrpcDemo.Api/Services/UserExportBackgroundService.cs`

---

## Database Scripts

**Current SQL scripts:**
- `001_CreateTables.sql`: creates `Users` and `Orders`
- `002_SeedData.sql`: inserts sample users and orders
- `003_SeedMillionUsers.sql`: inserts one million synthetic users for load testing
- `004_CreateUserExportJobs.sql`: creates `UserExportJobs` for persisted async export tracking

> Run `004_CreateUserExportJobs.sql` before testing async exports.

---

## Configuration

**Provisioner config:**
- `appsettings.json`

**API config:**
- `GraphQLGrpcDemo.Api/appsettings.json`

**Example connection string:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=TestAsyncDB;Trusted_Connection=True;"
  }
}
```

---

## Running The POC

### 1. Provision the database

```bash
dotnet run --project DbProvisioner.csproj
```

This will:
- ensure the database exists
- execute scripts from `Scripts`
- continue after individual script failures

### 2. Run the API

```bash
dotnet run --project GraphQLGrpcDemo.Api\GraphQLGrpcDemo.Api.csproj
```

**Swagger is typically available at:**
- `http://localhost:5288/swagger`

---

## API Documentation

**Base route:**
- `/api/user`

### 1. Get paginated users

**Endpoint:**

```http
GET /api/user?page=1&pageSize=100
```

**Purpose:**
- normal UI/API consumption
- avoids returning the full table at once

**Response shape:**

```json
{
  "items": [],
  "page": 1,
  "pageSize": 100,
  "totalCount": 1000000
}
```

---

### 2. Get orders by user

**Endpoint:**

```http
GET /api/user/{id}/orders
```

**Purpose:**
- fetch orders for a single user

---

### 3. Stream users progressively

**Endpoint:**

```http
GET /api/user/stream
```

**Optional query parameters:**
- `batchSize`
- `commandTimeoutSeconds`

**Response content type:**
- `application/x-ndjson`

**Purpose:**
- streams users little by little instead of building one massive payload in memory
- suitable for backend clients, download clients, or POC streaming tests

> This endpoint is not ideal for Swagger UI rendering with very large datasets.

---

### 4. Create async export job

**Endpoint:**

```http
POST /api/user/exports
```

**Request body:**

```json
{
  "format": "csv"
}
```

**Supported formats:**
- `csv`
- `ndjson`

**Purpose:**
- creates a background export job
- returns quickly with a `jobId`

---

### 5. List all export jobs

**Endpoint:**

```http
GET /api/user/exports
```

**Purpose:**
- returns all export jobs
- useful for seeing every `jobId`, status, file name, and timestamps in one call

---

### 6. Get export job status

**Endpoint:**

```http
GET /api/user/exports/{jobId}
```

**Possible statuses:**
- `Pending`
- `Running`
- `Completed`
- `Failed`

---

### 7. Download completed export

**Endpoint:**

```http
GET /api/user/exports/{jobId}/download
```

**Behavior:**
- returns the file when the export is completed and the file exists
- returns `409 Conflict` if the job is not ready yet

---

## Async Export Flow

**Recommended flow:**

1. Call `POST /api/user/exports`
2. Capture the returned `jobId`
3. Poll `GET /api/user/exports/{jobId}`
4. Wait for `status = Completed`
5. Call `GET /api/user/exports/{jobId}/download`

**Why this exists:**
- avoids holding one long browser request open
- allows large exports to run in the background
- supports browser close and retry scenarios
- is closer to enterprise export patterns than returning millions of rows directly

---

## Persistence And Restart Behavior

Async export metadata is persisted in SQL through the `UserExportJobs` table.

**Current behavior:**
- browser can be closed after export creation
- job status survives app restarts
- pending and running jobs are re-queued on startup
- failed jobs keep their error message in SQL

> Export files are written to the API application's content root under an `Exports` folder at runtime.

---

## Load Testing

**To populate large test data:**
- run `003_SeedMillionUsers.sql`

This script inserts one million synthetic users with unique emails for streaming and export testing.

---

## Example Test Flow

1. Run `DbProvisioner`
2. Confirm `004_CreateUserExportJobs.sql` has created the `UserExportJobs` table
3. Run `GraphQLGrpcDemo.Api`
4. Call `POST /api/user/exports`
5. Call `GET /api/user/exports`
6. Poll `GET /api/user/exports/{jobId}`
7. Download with `GET /api/user/exports/{jobId}/download`

---

## Notes

- Swagger is good for creating and monitoring export jobs, but not ideal for rendering massive streamed responses.
- For enterprise-scale reads, use pagination for interactive screens and async exports for large downloads.
- For future enterprise caching work, paginated reads are the best place to add cache-aside caching first.
