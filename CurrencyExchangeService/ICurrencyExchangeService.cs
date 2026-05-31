using CoreWCF;
using CurrencyExchangeService.Models;

namespace CurrencyExchangeService
{
    [ServiceContract(Namespace = "http://CurrencyExchange/2024")]
    public interface ICurrencyExchangeService
    {
        // ========== NBP Exchange Rates (No Auth Required) ==========

        [OperationContract]
        ExchangeRateResult GetExchangeRate(string currencyCode);

        [OperationContract]
        List<ExchangeRateResult> GetAllExchangeRates();

        [OperationContract]
        List<ExchangeRateResult> GetHistoricalRates(string currencyCode, DateTime startDate, DateTime endDate);

        // ========== User Account Management ==========

        [OperationContract]
        UserAccountResult RegisterUser(string username, string email, string password);

        [OperationContract]
        UserAccountResult LoginUser(string username, string password);

        // ========== Wallet / Balance ==========

        [OperationContract]
        WalletBalanceResult GetWalletBalance(int userId, string token);

        [OperationContract]
        OperationResult TopUpAccount(int userId, string token, decimal amount, string currencyCode = "PLN");

        // ========== Currency Exchange Operations ==========

        [OperationContract]
        TransactionResult ExchangeCurrency(int userId, string token, string fromCurrency, string toCurrency, decimal amount);

        [OperationContract]
        TransactionResult BuyCurrency(int userId, string token, string currencyCode, decimal amount);

        [OperationContract]
        TransactionResult SellCurrency(int userId, string token, string currencyCode, decimal amount);

        // ========== Transaction History ==========

        [OperationContract]
        List<TransactionResult> GetTransactionHistory(int userId, string token);
    }
}
