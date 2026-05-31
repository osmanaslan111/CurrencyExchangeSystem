using System.Runtime.Serialization;
using System.ServiceModel;

namespace CurrencyExchangeClient.ServiceProxy
{
    // ========== Data Contracts (mirror of service) ==========

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

    // ========== Service Contract (mirror of service interface) ==========

    [ServiceContract(Namespace = "http://CurrencyExchange/2024")]
    public interface ICurrencyExchangeService
    {
        [OperationContract]
        ExchangeRateResult GetExchangeRate(string currencyCode);

        [OperationContract]
        List<ExchangeRateResult> GetAllExchangeRates();

        [OperationContract]
        List<ExchangeRateResult> GetHistoricalRates(string currencyCode, DateTime startDate, DateTime endDate);

        [OperationContract]
        UserAccountResult RegisterUser(string username, string email, string password);

        [OperationContract]
        UserAccountResult LoginUser(string username, string password);

        [OperationContract]
        WalletBalanceResult GetWalletBalance(int userId, string token);

        [OperationContract]
        OperationResult TopUpAccount(int userId, string token, decimal amount, string currencyCode);

        [OperationContract]
        TransactionResult ExchangeCurrency(int userId, string token, string fromCurrency, string toCurrency, decimal amount);

        [OperationContract]
        TransactionResult BuyCurrency(int userId, string token, string currencyCode, decimal amount);

        [OperationContract]
        TransactionResult SellCurrency(int userId, string token, string currencyCode, decimal amount);

        [OperationContract]
        List<TransactionResult> GetTransactionHistory(int userId, string token);
    }

    // ========== WCF Client ==========

    public class ExchangeServiceClient : ClientBase<ICurrencyExchangeService>, ICurrencyExchangeService
    {
        public ExchangeServiceClient(string endpointUrl)
            : base(new BasicHttpBinding
            {
                MaxReceivedMessageSize = 10 * 1024 * 1024, // 10MB
                ReceiveTimeout = TimeSpan.FromSeconds(30),
                SendTimeout = TimeSpan.FromSeconds(30)
            },
            new EndpointAddress(endpointUrl))
        { }

        public ExchangeRateResult GetExchangeRate(string currencyCode) =>
            Channel.GetExchangeRate(currencyCode);

        public List<ExchangeRateResult> GetAllExchangeRates() =>
            Channel.GetAllExchangeRates();

        public List<ExchangeRateResult> GetHistoricalRates(string currencyCode, DateTime startDate, DateTime endDate) =>
            Channel.GetHistoricalRates(currencyCode, startDate, endDate);

        public UserAccountResult RegisterUser(string username, string email, string password) =>
            Channel.RegisterUser(username, email, password);

        public UserAccountResult LoginUser(string username, string password) =>
            Channel.LoginUser(username, password);

        public WalletBalanceResult GetWalletBalance(int userId, string token) =>
            Channel.GetWalletBalance(userId, token);

        public OperationResult TopUpAccount(int userId, string token, decimal amount, string currencyCode) =>
            Channel.TopUpAccount(userId, token, amount, currencyCode);

        public TransactionResult ExchangeCurrency(int userId, string token, string fromCurrency, string toCurrency, decimal amount) =>
            Channel.ExchangeCurrency(userId, token, fromCurrency, toCurrency, amount);

        public TransactionResult BuyCurrency(int userId, string token, string currencyCode, decimal amount) =>
            Channel.BuyCurrency(userId, token, currencyCode, amount);

        public TransactionResult SellCurrency(int userId, string token, string currencyCode, decimal amount) =>
            Channel.SellCurrency(userId, token, currencyCode, amount);

        public List<TransactionResult> GetTransactionHistory(int userId, string token) =>
            Channel.GetTransactionHistory(userId, token);
    }
}
