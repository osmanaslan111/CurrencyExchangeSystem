using CurrencyExchangeClient.ServiceProxy;

namespace CurrencyExchangeClient
{
    class Program
    {
        private const string ServiceUrl = "http://localhost:5000/CurrencyExchange/basic";

        private static ExchangeServiceClient? _client;
        private static int _userId = 0;
        private static string _token = "";
        private static string _username = "";

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "Currency Exchange Client";

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════╗");
            Console.WriteLine("║     Currency Exchange Office Client      ║");
            Console.WriteLine("╚══════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine($"\nConnecting to: {ServiceUrl}\n");

            try
            {
                _client = new ExchangeServiceClient(ServiceUrl);
                RunMainMenu();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR: Cannot connect to service: {ex.Message}");
                Console.WriteLine("\nMake sure the CurrencyExchangeService is running.");
                Console.ResetColor();
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static void RunMainMenu()
        {
            while (true)
            {
                Console.Clear();
                PrintHeader();

                if (_userId == 0)
                {
                    // Not logged in
                    Console.WriteLine("\n  PUBLIC MENU");
                    Console.WriteLine("  ──────────────────────────────");
                    Console.WriteLine("  1. View exchange rate (single currency)");
                    Console.WriteLine("  2. View all exchange rates");
                    Console.WriteLine("  3. View historical rates");
                    Console.WriteLine("  4. Register new account");
                    Console.WriteLine("  5. Login");
                    Console.WriteLine("  0. Exit");
                }
                else
                {
                    // Logged in
                    Console.WriteLine($"\n  Welcome, {_username}! (ID: {_userId})");
                    Console.WriteLine("  ──────────────────────────────");
                    Console.WriteLine("  1. View exchange rate (single currency)");
                    Console.WriteLine("  2. View all exchange rates");
                    Console.WriteLine("  3. View historical rates");
                    Console.WriteLine("  4. My wallet / balances");
                    Console.WriteLine("  5. Top up account (PLN)");
                    Console.WriteLine("  6. Buy currency (PLN → Foreign)");
                    Console.WriteLine("  7. Sell currency (Foreign → PLN)");
                    Console.WriteLine("  8. Exchange currencies (cross-rate)");
                    Console.WriteLine("  9. Transaction history");
                    Console.WriteLine("  L. Logout");
                    Console.WriteLine("  0. Exit");
                }

                Console.Write("\n  Your choice: ");
                var key = Console.ReadLine()?.Trim().ToUpper();

                Console.Clear();
                switch (key)
                {
                    case "1": MenuGetRate(); break;
                    case "2": MenuAllRates(); break;
                    case "3": MenuHistoricalRates(); break;
                    case "4": if (_userId == 0) MenuRegister(); else MenuWallet(); break;
                    case "5": if (_userId == 0) MenuLogin(); else MenuTopUp(); break;
                    case "6": if (_userId != 0) MenuBuyCurrency(); break;
                    case "7": if (_userId != 0) MenuSellCurrency(); break;
                    case "8": if (_userId != 0) MenuExchangeCurrency(); break;
                    case "9": if (_userId != 0) MenuTransactionHistory(); break;
                    case "L": Logout(); break;
                    case "0": return;
                }
            }
        }

        static void PrintHeader()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════╗");
            Console.WriteLine("║     Currency Exchange Office Client      ║");
            Console.WriteLine("╚══════════════════════════════════════════╝");
            Console.ResetColor();
        }

        static void MenuGetRate()
        {
            Console.WriteLine("=== Current Exchange Rate ===\n");
            Console.Write("Enter currency code (e.g. USD, EUR, GBP): ");
            var code = Console.ReadLine()?.Trim().ToUpper() ?? "";

            if (string.IsNullOrEmpty(code)) { Pause(); return; }

            Console.WriteLine("\nFetching from NBP API...");
            var result = _client!.GetExchangeRate(code);

            if (result.Success)
            {
                PrintRate(result);
            }
            else
            {
                PrintError(result.ErrorMessage);
            }

            Pause();
        }

        static void MenuAllRates()
        {
            Console.WriteLine("=== All Exchange Rates (NBP Table A) ===\n");
            Console.WriteLine("Fetching from NBP API...");

            var rates = _client!.GetAllExchangeRates();
            var valid = rates.Where(r => r.Success).ToList();

            if (valid.Count == 0)
            {
                PrintError("Could not fetch rates.");
                Pause();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n{"CODE",-6} {"CURRENCY",-30} {"BUY",-10} {"SELL",-10} {"MID",-10} {"DATE",-12}");
            Console.WriteLine(new string('─', 78));
            Console.ResetColor();

            foreach (var r in valid)
            {
                Console.WriteLine($"{r.CurrencyCode,-6} {r.CurrencyName,-30} {r.BuyRate,-10:F4} {r.SellRate,-10:F4} {r.MidRate,-10:F4} {r.Date:yyyy-MM-dd}");
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"\nTotal: {valid.Count} currencies | Source: NBP API");
            Console.ResetColor();
            Pause();
        }

        static void MenuHistoricalRates()
        {
            Console.WriteLine("=== Historical Exchange Rates ===\n");
            Console.Write("Currency code (e.g. USD): ");
            var code = Console.ReadLine()?.Trim().ToUpper() ?? "";

            Console.Write("Start date (yyyy-MM-dd, default: 30 days ago): ");
            var startStr = Console.ReadLine()?.Trim();
            var startDate = string.IsNullOrEmpty(startStr) ? DateTime.Today.AddDays(-30) : DateTime.Parse(startStr);

            Console.Write("End date (yyyy-MM-dd, default: today): ");
            var endStr = Console.ReadLine()?.Trim();
            var endDate = string.IsNullOrEmpty(endStr) ? DateTime.Today : DateTime.Parse(endStr);

            Console.WriteLine("\nFetching...");
            var rates = _client!.GetHistoricalRates(code, startDate, endDate);

            if (rates.Count == 0 || !rates[0].Success)
            {
                PrintError(rates.FirstOrDefault()?.ErrorMessage ?? "No data.");
                Pause();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n{code} Historical Rates ({startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd})");
            Console.WriteLine($"{"DATE",-14} {"MID RATE",-12} {"BUY",-12} {"SELL",-12}");
            Console.WriteLine(new string('─', 50));
            Console.ResetColor();

            foreach (var r in rates)
                Console.WriteLine($"{r.Date:yyyy-MM-dd}   {r.MidRate,-12:F4} {r.BuyRate,-12:F4} {r.SellRate,-12:F4}");

            Pause();
        }

        static void MenuRegister()
        {
            Console.WriteLine("=== Register New Account ===\n");
            Console.Write("Username: ");
            var user = Console.ReadLine()?.Trim() ?? "";
            Console.Write("Email: ");
            var email = Console.ReadLine()?.Trim() ?? "";
            Console.Write("Password: ");
            var pass = ReadPassword();

            var result = _client!.RegisterUser(user, email, pass);
            if (result.Success)
            {
                PrintSuccess($"Registered successfully! Welcome, {result.Username}!");
                _userId = result.UserId;
                _token = result.Token;
                _username = result.Username;
            }
            else PrintError(result.ErrorMessage);

            Pause();
        }

        static void MenuLogin()
        {
            Console.WriteLine("=== Login ===\n");
            Console.Write("Username: ");
            var user = Console.ReadLine()?.Trim() ?? "";
            Console.Write("Password: ");
            var pass = ReadPassword();

            var result = _client!.LoginUser(user, pass);
            if (result.Success)
            {
                _userId = result.UserId;
                _token = result.Token;
                _username = result.Username;
                PrintSuccess($"Logged in as {result.Username}!");
            }
            else PrintError(result.ErrorMessage);

            Pause();
        }

        static void Logout()
        {
            _userId = 0;
            _token = "";
            _username = "";
            PrintSuccess("Logged out successfully.");
            Pause();
        }

        static void MenuWallet()
        {
            Console.WriteLine("=== My Wallet ===\n");
            var result = _client!.GetWalletBalance(_userId, _token);

            if (!result.Success) { PrintError(result.ErrorMessage); Pause(); return; }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{"CURRENCY",-12} {"BALANCE",-16}");
            Console.WriteLine(new string('─', 28));
            Console.ResetColor();

            foreach (var b in result.Balances)
                Console.WriteLine($"{b.CurrencyCode,-12} {b.Amount,16:F4}");

            Pause();
        }

        static void MenuTopUp()
        {
            Console.WriteLine("=== Top Up Account ===\n");
            Console.Write("Amount (PLN): ");
            if (!decimal.TryParse(Console.ReadLine(), out var amount)) { PrintError("Invalid amount."); Pause(); return; }

            var result = _client!.TopUpAccount(_userId, _token, amount, "PLN");
            if (result.Success) PrintSuccess(result.Message);
            else PrintError(result.Message);

            Pause();
        }

        static void MenuBuyCurrency()
        {
            Console.WriteLine("=== Buy Currency (PLN → Foreign) ===\n");
            Console.Write("Currency to buy (e.g. USD): ");
            var code = Console.ReadLine()?.Trim().ToUpper() ?? "";
            Console.Write("Amount of PLN to spend: ");
            if (!decimal.TryParse(Console.ReadLine(), out var amount)) { PrintError("Invalid amount."); Pause(); return; }

            var result = _client!.BuyCurrency(_userId, _token, code, amount);
            PrintTransactionResult(result);
            Pause();
        }

        static void MenuSellCurrency()
        {
            Console.WriteLine("=== Sell Currency (Foreign → PLN) ===\n");
            Console.Write("Currency to sell (e.g. USD): ");
            var code = Console.ReadLine()?.Trim().ToUpper() ?? "";
            Console.Write("Amount to sell: ");
            if (!decimal.TryParse(Console.ReadLine(), out var amount)) { PrintError("Invalid amount."); Pause(); return; }

            var result = _client!.SellCurrency(_userId, _token, code, amount);
            PrintTransactionResult(result);
            Pause();
        }

        static void MenuExchangeCurrency()
        {
            Console.WriteLine("=== Exchange Currencies ===\n");
            Console.Write("From currency: ");
            var from = Console.ReadLine()?.Trim().ToUpper() ?? "";
            Console.Write("To currency: ");
            var to = Console.ReadLine()?.Trim().ToUpper() ?? "";
            Console.Write("Amount: ");
            if (!decimal.TryParse(Console.ReadLine(), out var amount)) { PrintError("Invalid amount."); Pause(); return; }

            var result = _client!.ExchangeCurrency(_userId, _token, from, to, amount);
            PrintTransactionResult(result);
            Pause();
        }

        static void MenuTransactionHistory()
        {
            Console.WriteLine("=== Transaction History ===\n");
            var history = _client!.GetTransactionHistory(_userId, _token);

            if (history.Count == 0)
            {
                Console.WriteLine("No transactions yet.");
                Pause(); return;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{"#",-6} {"DATE",-22} {"FROM",-8} {"TO",-8} {"FROM AMT",-14} {"TO AMT",-14} {"RATE",-10}");
            Console.WriteLine(new string('─', 82));
            Console.ResetColor();

            foreach (var t in history)
            {
                Console.WriteLine($"{t.TransactionId,-6} {t.Timestamp:yyyy-MM-dd HH:mm}    {t.FromCurrency,-8} {t.ToCurrency,-8} {t.FromAmount,-14:F4} {t.ToAmount,-14:F4} {t.Rate,-10:F6}");
            }

            Pause();
        }

        static void PrintRate(ExchangeRateResult r)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n  {r.CurrencyCode} - {r.CurrencyName}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"  ┌─────────────────────────────┐");
            Console.WriteLine($"  │  Buy Rate:  {r.BuyRate,10:F4} PLN    │");
            Console.WriteLine($"  │  Sell Rate: {r.SellRate,10:F4} PLN    │");
            Console.WriteLine($"  │  Mid Rate:  {r.MidRate,10:F4} PLN    │");
            Console.WriteLine($"  │  Date:      {r.Date:yyyy-MM-dd}        │");
            Console.WriteLine($"  └─────────────────────────────┘");
            Console.ResetColor();
        }

        static void PrintTransactionResult(TransactionResult r)
        {
            if (r.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n  ✓ Transaction #{r.TransactionId} Successful!");
                Console.ResetColor();
                Console.WriteLine($"  {r.FromAmount:F4} {r.FromCurrency}  →  {r.ToAmount:F4} {r.ToCurrency}");
                Console.WriteLine($"  Rate: {r.Rate:F6} | Time: {r.Timestamp:yyyy-MM-dd HH:mm:ss}");
            }
            else PrintError(r.ErrorMessage);
        }

        static void PrintSuccess(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n  ✓ {msg}");
            Console.ResetColor();
        }

        static void PrintError(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n  ✗ ERROR: {msg}");
            Console.ResetColor();
        }

        static void Pause()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("\n  Press any key to continue...");
            Console.ResetColor();
            Console.ReadKey(true);
        }

        static string ReadPassword()
        {
            var pass = "";
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    pass += key.KeyChar;
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                {
                    pass = pass[..^1];
                    Console.Write("\b \b");
                }
            } while (key.Key != ConsoleKey.Enter);

            Console.WriteLine();
            return pass;
        }
    }
}
