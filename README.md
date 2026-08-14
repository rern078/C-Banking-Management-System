# Banking Management System

ASP.NET Core 8 MVC banking app with SQL Server (or SQLite for local demo), Identity auth, account management, deposits/withdrawals, transfers, transaction history, and an admin dashboard.

## Stack

- C# / .NET 8
- ASP.NET Core MVC
- Entity Framework Core
- SQL Server (recommended) or SQLite (default for quick start)
- ASP.NET Core Identity (Admin + Customer roles)

## Features

- Customer registration and login
- Admin dashboard (customers, accounts, balances, today's transactions)
- Open / freeze / activate / close accounts
- Deposit, withdraw, transfer
- Transaction history (customer + admin filters)

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- For SQL Server mode: SQL Server LocalDB, Express, or full SQL Server

## Run (quick start — SQLite)

```bash
cd BankingManagementSystem
dotnet restore
dotnet run --urls http://localhost:5180
```

Open http://localhost:5180

## Switch to SQL Server

1. Install SQL Server LocalDB or Express.
2. In `appsettings.json` set:

```json
"DatabaseProvider": "SqlServer",
"ConnectionStrings": {
  "SqlServer": "Server=YOUR_SERVER;Database=BankingManagementSystem;Trusted_Connection=True;TrustServerCertificate=True"
}
```

3. Apply migrations:

```bash
dotnet ef database update
dotnet run --urls http://localhost:5180
```

## Demo accounts

| Role     | Email                 | Password      |
|----------|-----------------------|---------------|
| Admin    | admin@bank.local      | Admin@123     |
| Customer | customer@bank.local   | Customer@123  |

On first run the database is created/migrated and these users are seeded automatically.

## Typical flow

1. Sign in as **admin** → open accounts for customers, review dashboard & transactions  
2. Sign in as **customer** → deposit / withdraw / transfer, view history  
3. Register new customers from the home page (Admin opens their bank accounts)

## Project layout

```
BankingManagementSystem/
  Controllers/   Home, Account, Dashboard, Admin, Accounts
  Models/        ApplicationUser, BankAccount, Transaction
  Services/      BankingService (deposit/withdraw/transfer)
  Data/          DbContext + seeder
  Views/         Razor UI
```
