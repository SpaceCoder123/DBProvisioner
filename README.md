# Database Provisioner (POC)

## Overview

This console application provisions a SQL Server database and executes SQL scripts during runtime.

It is designed for **POC and development environments** where quick database setup is required without manual intervention.

---

## Features

* Creates a database based on the connection string
* Executes SQL scripts from the `Scripts` directory
* Uses Windows Authentication (`Trusted_Connection=True`)
* Lightweight (no ORM / no migrations framework)

---

## Configuration

Update the database name in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=Test1;Trusted_Connection=True;"
  }
}
```

---

## Behavior

* If the database **does not exist** → it will be created
* If the database **already exists** → creation is skipped
* SQL scripts inside the `Scripts` folder are executed sequentially

> ⚠️ Note:
> Scripts should be **idempotent** (use `IF NOT EXISTS` checks) to avoid errors on re-execution.

---

## Folder Structure

```
DbProvisioner/
│
├── Scripts/
│   ├── 001_CreateTables.sql
│   ├── 002_SeedData.sql
│
├── Program.cs
├── appsettings.json
```

---

## How to Run

```bash
dotnet run
```

---

## Output

* Database will be created in the configured SQL Server instance
* All scripts in the `Scripts` folder will be executed
* 
## Summary

This tool acts as a **minimal database bootstrapper**, enabling developers to spin up a database and schema quickly during development or POC phases.
