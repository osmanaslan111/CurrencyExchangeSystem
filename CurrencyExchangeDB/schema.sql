-- Currency Exchange Office System
-- Database Schema (SQLite)
-- Generated for: CurrencyExchangeService

-- Users table
CREATE TABLE IF NOT EXISTS Users (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Username    TEXT    NOT NULL UNIQUE,
    Email       TEXT    NOT NULL UNIQUE,
    PasswordHash TEXT   NOT NULL,
    CreatedAt   TEXT    NOT NULL DEFAULT (datetime('now'))
);

-- Wallets (one per user per currency)
CREATE TABLE IF NOT EXISTS Wallets (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId       INTEGER NOT NULL,
    CurrencyCode TEXT    NOT NULL,
    Balance      REAL    NOT NULL DEFAULT 0,
    UNIQUE(UserId, CurrencyCode),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- Transactions history
CREATE TABLE IF NOT EXISTS Transactions (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId       INTEGER NOT NULL,
    FromCurrency TEXT    NOT NULL,
    ToCurrency   TEXT    NOT NULL,
    FromAmount   REAL    NOT NULL,
    ToAmount     REAL    NOT NULL,
    Rate         REAL    NOT NULL,
    Type         TEXT    NOT NULL DEFAULT 'EXCHANGE',  -- EXCHANGE, BUY, SELL, TOPUP
    Timestamp    TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_wallets_user     ON Wallets(UserId);
CREATE INDEX IF NOT EXISTS idx_transactions_user ON Transactions(UserId);
CREATE INDEX IF NOT EXISTS idx_transactions_time ON Transactions(Timestamp);

-- Sample data (optional, for testing)
-- INSERT INTO Users (Username, Email, PasswordHash) VALUES ('testuser', 'test@test.com', '$2a$10$...');
