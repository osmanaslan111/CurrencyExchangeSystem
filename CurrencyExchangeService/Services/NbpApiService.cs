using System.Text.Json;
using CurrencyExchangeService.Models;

namespace CurrencyExchangeService.Services
{
    public class NbpApiService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://api.nbp.pl/api";

        public NbpApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Gets current exchange rate for a single currency from NBP Table C (buy/sell rates)
        /// </summary>
        public async Task<ExchangeRateResult> GetCurrentRateAsync(string currencyCode)
        {
            try
            {
                // Table C has bid/ask (buy/sell) rates
                var url = $"{BaseUrl}/exchangerates/rates/c/{currencyCode.ToUpper()}/?format=json";
                var response = await _httpClient.GetStringAsync(url);

                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;
                var rates = root.GetProperty("rates")[0];

                return new ExchangeRateResult
                {
                    CurrencyCode = currencyCode.ToUpper(),
                    CurrencyName = root.GetProperty("currency").GetString() ?? "",
                    BuyRate = rates.GetProperty("bid").GetDecimal(),
                    SellRate = rates.GetProperty("ask").GetDecimal(),
                    MidRate = (rates.GetProperty("bid").GetDecimal() + rates.GetProperty("ask").GetDecimal()) / 2,
                    Date = DateTime.Parse(rates.GetProperty("effectiveDate").GetString() ?? DateTime.Today.ToString("yyyy-MM-dd")),
                    Success = true
                };
            }
            catch (HttpRequestException)
            {
                // Fallback to Table A (mid rates only) if Table C doesn't have the currency
                return await GetMidRateAsync(currencyCode);
            }
            catch (Exception ex)
            {
                return new ExchangeRateResult
                {
                    CurrencyCode = currencyCode.ToUpper(),
                    Success = false,
                    ErrorMessage = $"Error fetching rate for {currencyCode}: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Gets mid rate from Table A
        /// </summary>
        private async Task<ExchangeRateResult> GetMidRateAsync(string currencyCode)
        {
            try
            {
                var url = $"{BaseUrl}/exchangerates/rates/a/{currencyCode.ToUpper()}/?format=json";
                var response = await _httpClient.GetStringAsync(url);

                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;
                var rates = root.GetProperty("rates")[0];
                var mid = rates.GetProperty("mid").GetDecimal();

                return new ExchangeRateResult
                {
                    CurrencyCode = currencyCode.ToUpper(),
                    CurrencyName = root.GetProperty("currency").GetString() ?? "",
                    MidRate = mid,
                    BuyRate = Math.Round(mid * 0.99m, 4),   // Simulate spread: 1% margin
                    SellRate = Math.Round(mid * 1.01m, 4),
                    Date = DateTime.Parse(rates.GetProperty("effectiveDate").GetString() ?? DateTime.Today.ToString("yyyy-MM-dd")),
                    Success = true
                };
            }
            catch (HttpRequestException ex)
            {
                return new ExchangeRateResult
                {
                    CurrencyCode = currencyCode.ToUpper(),
                    Success = false,
                    ErrorMessage = $"NBP API HTTP error for {currencyCode}: {ex.StatusCode} - {ex.Message}"
                };
            }
            catch (TaskCanceledException)
            {
                return new ExchangeRateResult
                {
                    CurrencyCode = currencyCode.ToUpper(),
                    Success = false,
                    ErrorMessage = $"NBP API timeout for {currencyCode}. Check internet connection."
                };
            }
            catch (Exception ex)
            {
                return new ExchangeRateResult
                {
                    CurrencyCode = currencyCode.ToUpper(),
                    Success = false,
                    ErrorMessage = $"Error fetching {currencyCode}: [{ex.GetType().Name}] {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Gets all available exchange rates from Table A
        /// </summary>
        public async Task<List<ExchangeRateResult>> GetAllRatesAsync()
        {
            var results = new List<ExchangeRateResult>();

            try
            {
                var url = $"{BaseUrl}/exchangerates/tables/a/?format=json";
                var response = await _httpClient.GetStringAsync(url);

                using var doc = JsonDocument.Parse(response);
                var table = doc.RootElement[0];
                var effectiveDate = table.GetProperty("effectiveDate").GetString() ?? DateTime.Today.ToString("yyyy-MM-dd");
                var ratesArray = table.GetProperty("rates");

                foreach (var rate in ratesArray.EnumerateArray())
                {
                    var mid = rate.GetProperty("mid").GetDecimal();
                    results.Add(new ExchangeRateResult
                    {
                        CurrencyCode = rate.GetProperty("code").GetString() ?? "",
                        CurrencyName = rate.GetProperty("currency").GetString() ?? "",
                        MidRate = mid,
                        BuyRate = Math.Round(mid * 0.99m, 4),
                        SellRate = Math.Round(mid * 1.01m, 4),
                        Date = DateTime.Parse(effectiveDate),
                        Success = true
                    });
                }

                // Add PLN as base currency
                results.Insert(0, new ExchangeRateResult
                {
                    CurrencyCode = "PLN",
                    CurrencyName = "Polish Zloty",
                    MidRate = 1.0m,
                    BuyRate = 1.0m,
                    SellRate = 1.0m,
                    Date = DateTime.Parse(effectiveDate),
                    Success = true
                });
            }
            catch (Exception ex)
            {
                results.Add(new ExchangeRateResult
                {
                    Success = false,
                    ErrorMessage = $"Error fetching all rates: {ex.Message}"
                });
            }

            return results;
        }

        /// <summary>
        /// Gets historical exchange rates for a currency
        /// </summary>
        public async Task<List<ExchangeRateResult>> GetHistoricalRatesAsync(string currencyCode, DateTime startDate, DateTime endDate)
        {
            var results = new List<ExchangeRateResult>();

            try
            {
                var start = startDate.ToString("yyyy-MM-dd");
                var end = endDate.ToString("yyyy-MM-dd");
                var url = $"{BaseUrl}/exchangerates/rates/a/{currencyCode.ToUpper()}/{start}/{end}/?format=json";

                var response = await _httpClient.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;
                var currencyName = root.GetProperty("currency").GetString() ?? "";
                var ratesArray = root.GetProperty("rates");

                foreach (var rate in ratesArray.EnumerateArray())
                {
                    var mid = rate.GetProperty("mid").GetDecimal();
                    results.Add(new ExchangeRateResult
                    {
                        CurrencyCode = currencyCode.ToUpper(),
                        CurrencyName = currencyName,
                        MidRate = mid,
                        BuyRate = Math.Round(mid * 0.99m, 4),
                        SellRate = Math.Round(mid * 1.01m, 4),
                        Date = DateTime.Parse(rate.GetProperty("effectiveDate").GetString() ?? ""),
                        Success = true
                    });
                }
            }
            catch (Exception ex)
            {
                results.Add(new ExchangeRateResult
                {
                    CurrencyCode = currencyCode.ToUpper(),
                    Success = false,
                    ErrorMessage = $"Error fetching historical rates: {ex.Message}"
                });
            }

            return results;
        }
    }
}
