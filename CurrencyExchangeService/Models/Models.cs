using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore;

namespace CurrencyExchangeService.Models
{
    // ========== DATA CONTRACTS (WCF Serializable) ==========

    [DataContract(Namespace = "http://CurrencyExchange/2024")]
    public class ExchangeRateResult
    {
        [DataMember] public string CurrencyCode { get; set; } = "";
        [DataMember] public string CurrencyName { get; set; } = "";
        [DataMember] public decimal BuyRate { get; set; }
        [DataMember] public decimal SellRate { get; set; }
        [DataMember] public decimal MidRate { get; set; }
        [DataMember] public DateTime Date { get; set; }
        [DataMember] public bool Success { get; set; }
        [DataMember] public string ErrorMessage { get; set; } = "";
    }

    [DataContract(Namespace = "http://CurrencyExchange/2024")]
    public class UserAccountResult
    {
        [DataMember] public int UserId { get; set; }
        [DataMember] public string Username { get; set; } = "";
        [DataMember] public string Email { get; set; } = "";
        [DataMember] public bool Success { get; set; }
        [DataMember] public string ErrorMessage { get; set; } = "";
        [DataMember] public string Token { get; set; } = "";
    }

    [DataContract(Namespace = "http://CurrencyExchange/2024")]
    public class WalletBalanceResult
    {
        [DataMember] public int UserId { get; set; }
        [DataMember] public List<CurrencyBalance> Balances { get; set; } = new();
        [DataMember] public bool Success { get; set; }
        [DataMember] public string ErrorMessage { get; set; } = "";
    }

    [DataContract(Namespace = "http://CurrencyExchange/2024")]
    public class CurrencyBalance
    {
        [DataMember] public string CurrencyCode { get; set; } = "";
        [DataMember] public decimal Amount { get; set; }
    }

    [DataContract(Namespace = "http://CurrencyExchange/2024")]
    public class TransactionResult
    {
        [DataMember] public int TransactionId { get; set; }
        [DataMember] public string FromCurrency { get; set; } = "";
        [DataMember] public string ToCurrency { get; set; } = "";
        [DataMember] public decimal FromAmount { get; set; }
        [DataMember] public decimal ToAmount { get; set; }
        [DataMember] public decimal Rate { get; set; }
        [DataMember] public DateTime Timestamp { get; set; }
        [DataMember] public bool Success { get; set; }
        [DataMember] public string ErrorMessage { get; set; } = "";
    }

    [DataContract(Namespace = "http://CurrencyExchange/2024")]
    public class OperationResult
    {
        [DataMember] public bool Success { get; set; }
        [DataMember] public string Message { get; set; } = "";
    }

    // ========== DATABASE ENTITIES ==========

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<Wallet> Wallets { get; set; } = new();
        public List<Transaction> Transactions { get; set; } = new();
    }

    public class Wallet
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string CurrencyCode { get; set; } = "";
        public decimal Balance { get; set; } = 0;
        public User? User { get; set; }
    }

    public class Transaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string FromCurrency { get; set; } = "";
        public string ToCurrency { get; set; } = "";
        public decimal FromAmount { get; set; }
        public decimal ToAmount { get; set; }
        public decimal Rate { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Type { get; set; } = "EXCHANGE"; // BUY, SELL, EXCHANGE, TOPUP
        public User? User { get; set; }
    }

    // ========== DATABASE CONTEXT ==========

    public class ExchangeDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        public ExchangeDbContext(DbContextOptions<ExchangeDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Wallet>()
                .HasIndex(w => new { w.UserId, w.CurrencyCode })
                .IsUnique();

            modelBuilder.Entity<Wallet>()
                .Property(w => w.Balance)
                .HasColumnType("decimal(18,4)");

            modelBuilder.Entity<Transaction>()
                .Property(t => t.FromAmount)
                .HasColumnType("decimal(18,4)");

            modelBuilder.Entity<Transaction>()
                .Property(t => t.ToAmount)
                .HasColumnType("decimal(18,4)");

            modelBuilder.Entity<Transaction>()
                .Property(t => t.Rate)
                .HasColumnType("decimal(18,6)");
        }
    }
}
