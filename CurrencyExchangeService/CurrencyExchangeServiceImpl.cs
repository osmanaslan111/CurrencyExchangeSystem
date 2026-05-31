using System.Collections.Concurrent;
using CurrencyExchangeService.Models;
using CurrencyExchangeService.Services;
using BCrypt.Net;

namespace CurrencyExchangeService
{
    public class CurrencyExchangeServiceImpl : ICurrencyExchangeService
    {
        private readonly NbpApiService _nbpService;
        private readonly ExchangeDbContext _db;

        // Simple in-memory token store (token -> userId, expiry)
        private static readonly ConcurrentDictionary<string, (int UserId, DateTime Expiry)> _tokens = new();

        public CurrencyExchangeServiceImpl(ExchangeDbContext db)
        {
            _db = db;
            _nbpService = new NbpApiService();
        }

        // ========== TOKEN HELPERS ==========

        private string GenerateToken(int userId)
        {
            var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            _tokens[token] = (userId, DateTime.UtcNow.AddHours(8));
            return token;
        }

        private bool ValidateToken(int userId, string token, out string error)
        {
            error = "";
            if (!_tokens.TryGetValue(token, out var entry))
            {
                error = "Invalid or expired session. Please login again.";
                return false;
            }
            if (entry.UserId != userId)
            {
                error = "Token does not match user.";
                return false;
            }
            if (entry.Expiry < DateTime.UtcNow)
            {
                _tokens.TryRemove(token, out _);
                error = "Session expired. Please login again.";
                return false;
            }
            return true;
        }

        // ========== NBP RATES ==========

        public ExchangeRateResult GetExchangeRate(string currencyCode)
        {
            try
            {
                var result = Task.Run(() => _nbpService.GetCurrentRateAsync(currencyCode)).GetAwaiter().GetResult();
                if (!result.Success && string.IsNullOrEmpty(result.ErrorMessage))
                    result.ErrorMessage = $"Unknown error fetching {currencyCode}";
                return result;
            }
            catch (Exception ex)
            {
                return new ExchangeRateResult
                {
                    CurrencyCode = currencyCode.ToUpper(),
                    Success = false,
                    ErrorMessage = ex.InnerException?.Message ?? ex.Message
                };
            }
        }

        public List<ExchangeRateResult> GetAllExchangeRates()
        {
            return Task.Run(() => _nbpService.GetAllRatesAsync()).GetAwaiter().GetResult();
        }

        public List<ExchangeRateResult> GetHistoricalRates(string currencyCode, DateTime startDate, DateTime endDate)
        {
            // NBP API allows max 93 days per request
            if ((endDate - startDate).TotalDays > 93)
                endDate = startDate.AddDays(93);

            return Task.Run(() => _nbpService.GetHistoricalRatesAsync(currencyCode, startDate, endDate)).GetAwaiter().GetResult();
        }

        // ========== USER MANAGEMENT ==========

        public UserAccountResult RegisterUser(string username, string email, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
                    return new UserAccountResult { Success = false, ErrorMessage = "Username must be at least 3 characters." };

                if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                    return new UserAccountResult { Success = false, ErrorMessage = "Invalid email address." };

                if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                    return new UserAccountResult { Success = false, ErrorMessage = "Password must be at least 6 characters." };

                if (_db.Users.Any(u => u.Username == username))
                    return new UserAccountResult { Success = false, ErrorMessage = "Username already exists." };

                if (_db.Users.Any(u => u.Email == email))
                    return new UserAccountResult { Success = false, ErrorMessage = "Email already registered." };

                var user = new User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    CreatedAt = DateTime.UtcNow
                };

                _db.Users.Add(user);
                _db.SaveChanges();

                // Create default PLN wallet
                _db.Wallets.Add(new Wallet { UserId = user.Id, CurrencyCode = "PLN", Balance = 0 });
                _db.SaveChanges();

                var token = GenerateToken(user.Id);

                return new UserAccountResult
                {
                    UserId = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Token = token,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new UserAccountResult { Success = false, ErrorMessage = $"Registration failed: {ex.Message}" };
            }
        }

        public UserAccountResult LoginUser(string username, string password)
        {
            try
            {
                var user = _db.Users.FirstOrDefault(u => u.Username == username);
                if (user == null)
                    return new UserAccountResult { Success = false, ErrorMessage = "Invalid username or password." };

                if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                    return new UserAccountResult { Success = false, ErrorMessage = "Invalid username or password." };

                var token = GenerateToken(user.Id);

                return new UserAccountResult
                {
                    UserId = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Token = token,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new UserAccountResult { Success = false, ErrorMessage = $"Login failed: {ex.Message}" };
            }
        }

        // ========== WALLET ==========

        public WalletBalanceResult GetWalletBalance(int userId, string token)
        {
            if (!ValidateToken(userId, token, out var error))
                return new WalletBalanceResult { Success = false, ErrorMessage = error };

            var wallets = _db.Wallets.Where(w => w.UserId == userId).ToList();

            return new WalletBalanceResult
            {
                UserId = userId,
                Balances = wallets.Select(w => new CurrencyBalance
                {
                    CurrencyCode = w.CurrencyCode,
                    Amount = w.Balance
                }).ToList(),
                Success = true
            };
        }

        public OperationResult TopUpAccount(int userId, string token, decimal amount, string currencyCode = "PLN")
        {
            if (!ValidateToken(userId, token, out var error))
                return new OperationResult { Success = false, Message = error };

            if (amount <= 0)
                return new OperationResult { Success = false, Message = "Amount must be greater than zero." };

            var wallet = _db.Wallets.FirstOrDefault(w => w.UserId == userId && w.CurrencyCode == currencyCode);
            if (wallet == null)
            {
                wallet = new Wallet { UserId = userId, CurrencyCode = currencyCode, Balance = 0 };
                _db.Wallets.Add(wallet);
            }

            wallet.Balance += amount;

            // Record top-up transaction
            _db.Transactions.Add(new Transaction
            {
                UserId = userId,
                FromCurrency = "TRANSFER",
                ToCurrency = currencyCode,
                FromAmount = amount,
                ToAmount = amount,
                Rate = 1,
                Type = "TOPUP",
                Timestamp = DateTime.UtcNow
            });

            _db.SaveChanges();

            return new OperationResult { Success = true, Message = $"Successfully added {amount:F2} {currencyCode} to your account." };
        }

        // ========== EXCHANGE OPERATIONS ==========

        public TransactionResult ExchangeCurrency(int userId, string token, string fromCurrency, string toCurrency, decimal amount)
        {
            if (!ValidateToken(userId, token, out var error))
                return new TransactionResult { Success = false, ErrorMessage = error };

            if (amount <= 0)
                return new TransactionResult { Success = false, ErrorMessage = "Amount must be greater than zero." };

            fromCurrency = fromCurrency.ToUpper();
            toCurrency = toCurrency.ToUpper();

            if (fromCurrency == toCurrency)
                return new TransactionResult { Success = false, ErrorMessage = "Cannot exchange same currency." };

            // Get exchange rates
            decimal rate;
            decimal toAmount;

            try
            {
                if (fromCurrency == "PLN")
                {
                    var toRate = _nbpService.GetCurrentRateAsync(toCurrency).GetAwaiter().GetResult();
                    if (!toRate.Success)
                        return new TransactionResult { Success = false, ErrorMessage = toRate.ErrorMessage };

                    // Buying foreign currency: we sell at ASK price
                    rate = toRate.SellRate;
                    toAmount = Math.Round(amount / rate, 4);
                }
                else if (toCurrency == "PLN")
                {
                    var fromRate = _nbpService.GetCurrentRateAsync(fromCurrency).GetAwaiter().GetResult();
                    if (!fromRate.Success)
                        return new TransactionResult { Success = false, ErrorMessage = fromRate.ErrorMessage };

                    // Selling foreign currency: we buy at BID price
                    rate = fromRate.BuyRate;
                    toAmount = Math.Round(amount * rate, 4);
                }
                else
                {
                    // Cross-rate: both via PLN
                    var fromRate = _nbpService.GetCurrentRateAsync(fromCurrency).GetAwaiter().GetResult();
                    var toRate = _nbpService.GetCurrentRateAsync(toCurrency).GetAwaiter().GetResult();

                    if (!fromRate.Success) return new TransactionResult { Success = false, ErrorMessage = fromRate.ErrorMessage };
                    if (!toRate.Success) return new TransactionResult { Success = false, ErrorMessage = toRate.ErrorMessage };

                    var plnAmount = amount * fromRate.BuyRate;
                    toAmount = Math.Round(plnAmount / toRate.SellRate, 4);
                    rate = toAmount / amount;
                }
            }
            catch (Exception ex)
            {
                return new TransactionResult { Success = false, ErrorMessage = $"Error fetching rates: {ex.Message}" };
            }

            // Check source wallet balance
            var fromWallet = _db.Wallets.FirstOrDefault(w => w.UserId == userId && w.CurrencyCode == fromCurrency);
            if (fromWallet == null || fromWallet.Balance < amount)
                return new TransactionResult { Success = false, ErrorMessage = $"Insufficient {fromCurrency} balance." };

            // Get or create destination wallet
            var toWallet = _db.Wallets.FirstOrDefault(w => w.UserId == userId && w.CurrencyCode == toCurrency);
            if (toWallet == null)
            {
                toWallet = new Wallet { UserId = userId, CurrencyCode = toCurrency, Balance = 0 };
                _db.Wallets.Add(toWallet);
            }

            fromWallet.Balance -= amount;
            toWallet.Balance += toAmount;

            var transaction = new Transaction
            {
                UserId = userId,
                FromCurrency = fromCurrency,
                ToCurrency = toCurrency,
                FromAmount = amount,
                ToAmount = toAmount,
                Rate = rate,
                Type = "EXCHANGE",
                Timestamp = DateTime.UtcNow
            };

            _db.Transactions.Add(transaction);
            _db.SaveChanges();

            return new TransactionResult
            {
                TransactionId = transaction.Id,
                FromCurrency = fromCurrency,
                ToCurrency = toCurrency,
                FromAmount = amount,
                ToAmount = toAmount,
                Rate = rate,
                Timestamp = transaction.Timestamp,
                Success = true
            };
        }

        public TransactionResult BuyCurrency(int userId, string token, string currencyCode, decimal amount)
        {
            // Buying foreign currency = exchanging PLN -> foreign currency
            return ExchangeCurrency(userId, token, "PLN", currencyCode, amount);
        }

        public TransactionResult SellCurrency(int userId, string token, string currencyCode, decimal amount)
        {
            // Selling foreign currency = exchanging foreign currency -> PLN
            return ExchangeCurrency(userId, token, currencyCode, "PLN", amount);
        }

        public List<TransactionResult> GetTransactionHistory(int userId, string token)
        {
            if (!ValidateToken(userId, token, out _))
                return new List<TransactionResult>();

            return _db.Transactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Timestamp)
                .Take(100)
                .Select(t => new TransactionResult
                {
                    TransactionId = t.Id,
                    FromCurrency = t.FromCurrency,
                    ToCurrency = t.ToCurrency,
                    FromAmount = t.FromAmount,
                    ToAmount = t.ToAmount,
                    Rate = t.Rate,
                    Timestamp = t.Timestamp,
                    Success = true
                })
                .ToList();
        }
    }
}
