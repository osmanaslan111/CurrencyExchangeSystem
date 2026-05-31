# Currency Exchange Office System
### WCF Web Service — .NET 6 | NBP API | SQLite

---

## Project Overview

A network-based currency exchange simulation system built with:
- **WCF Service** (CoreWCF on .NET 6) — business logic + NBP API integration
- **Console Client** — interactive CLI consuming the WCF service
- **SQLite Database** — persistent user accounts, wallets, and transactions

---

## Architecture

```
┌─────────────────────────────────────────────────────┐
│              CurrencyExchangeSystem.sln             │
│                                                     │
│  ┌──────────────────────┐  ┌──────────────────────┐ │
│  │  CurrencyExchange    │  │  CurrencyExchange    │ │
│  │     Service          │  │     Client           │ │
│  │  (CoreWCF / .NET 6)  │  │  (Console / .NET 6) │ │
│  │                      │  │                      │ │
│  │  ICurrencyExchange   │  │  ServiceProxy        │ │
│  │  ServiceImpl         │◄─┤  (BasicHttpBinding)  │ │
│  │  NbpApiService       │  │  Interactive Menu    │ │
│  │  ExchangeDbContext   │  └──────────────────────┘ │
│  │  Models (EF Core)    │           │               │
│  └──────────┬───────────┘           │ WCF/HTTP      │
│             │                       │               │
│      SQLite │              ┌────────┘               │
│          currency_         │                        │
│          exchange.db       ▼                        │
│                    http://localhost:5000             │
└─────────────────────────────────────────────────────┘
              │
              ▼ HTTPS
    https://api.nbp.pl/api
    (National Bank of Poland)
```

---

## Prerequisites

- [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) or newer
- Visual Studio 2022 (recommended) or VS Code
- Internet connection (for NBP API)

---

## Quick Start

### 1. Clone / open the solution

```bash
git clone <your-repo-url>
cd CurrencyExchangeSystem
```

Open `CurrencyExchangeSystem.sln` in Visual Studio.

### 2. Restore NuGet packages

```bash
dotnet restore
```

### 3. Run the WCF Service

```bash
cd CurrencyExchangeService
dotnet run
```

The service starts at **http://localhost:5000**

WSDL available at: http://localhost:5000/CurrencyExchange/basic?wsdl

### 4. Run the Console Client (separate terminal)

```bash
cd CurrencyExchangeClient
dotnet run
```

---

## WCF Service Endpoints

| Endpoint | URL |
|----------|-----|
| BasicHttpBinding | `http://localhost:5000/CurrencyExchange/basic` |
| WSHttpBinding    | `http://localhost:5000/CurrencyExchange/ws`    |
| WSDL Metadata    | `http://localhost:5000/CurrencyExchange/basic?wsdl` |

---

## Service Operations

### Public (No Authentication)

| Method | Parameters | Description |
|--------|-----------|-------------|
| `GetExchangeRate` | `currencyCode` | Current rate for one currency |
| `GetAllExchangeRates` | — | All NBP Table A rates |
| `GetHistoricalRates` | `currencyCode, startDate, endDate` | Historical data (max 93 days) |

### Authenticated (requires userId + token)

| Method | Description |
|--------|-------------|
| `RegisterUser` | Create new account |
| `LoginUser` | Login, receive session token |
| `GetWalletBalance` | View all currency balances |
| `TopUpAccount` | Add PLN to wallet |
| `BuyCurrency` | Spend PLN to buy foreign currency |
| `SellCurrency` | Sell foreign currency for PLN |
| `ExchangeCurrency` | Cross-rate exchange |
| `GetTransactionHistory` | Last 100 transactions |

---

## NBP API Integration

Data sourced from the **National Bank of Poland** official API:
- **Table A** — Mid rates for all currencies
- **Table C** — Bid/Ask rates (used when available)
- Documentation: http://api.nbp.pl/en.html

Exchange rate spread: ±1% on mid rate (bank margin simulation)

---

## Database

SQLite database auto-created at service startup.
Location: `CurrencyExchangeService/bin/Debug/net6.0/currency_exchange.db`

Schema file: `CurrencyExchangeDB/schema.sql`

Tables:
- `Users` — account data (username, email, hashed password)
- `Wallets` — per-user, per-currency balances
- `Transactions` — full transaction log

---

## Project Structure

```
CurrencyExchangeSystem/
├── CurrencyExchangeSystem.sln
│
├── CurrencyExchangeService/              ← WCF Service (Lab 1–14)
│   ├── ICurrencyExchangeService.cs       ← Service Contract (interface)
│   ├── CurrencyExchangeServiceImpl.cs    ← Service Implementation
│   ├── Program.cs                        ← CoreWCF host setup
│   ├── Models/
│   │   └── Models.cs                     ← DataContracts + EF entities
│   └── Services/
│       └── NbpApiService.cs             ← NBP API client
│
├── CurrencyExchangeClient/               ← Console Client (Lab 1, optional)
│   ├── Program.cs                        ← Interactive menu
│   └── ServiceProxy/
│       └── CurrencyExchangeProxy.cs      ← WCF proxy + data contracts
│
├── CurrencyExchangeDB/
│   └── schema.sql                        ← Database schema
│
└── docs/
    └── README.md                         ← This file
```

---

## Lab Coverage

| Lab | Topic | Status |
|-----|-------|--------|
| Lab 1 | WCF service creation + console client | ✅ |
| Labs 2–4 | NBP API integration, exchange rate method | ✅ |
| Lab 5 | Project architecture | ✅ |
| Lab 6 | Currency exchange logic | ✅ |
| Lab 7 | NBP API integration | ✅ |
| Lab 8–10 | User accounts, transactions | ✅ |
| Lab 11–12 | Database integration (SQLite + EF Core) | ✅ |
| Lab 13 | Historical rates | ✅ |
| Lab 14 | Final improvements | ✅ |

---

## GitHub

```bash
git init
git add .
git commit -m "Initial commit: Currency Exchange WCF System"
git remote add origin https://github.com/<your-username>/<repo-name>.git
git push -u origin main
```

---

## Author

**Osman Aslan**  
Student ID: 64519  
Computer Science — Master's Degree  
Web Services course — .NET / CoreWCF / SQLite / NBP API
