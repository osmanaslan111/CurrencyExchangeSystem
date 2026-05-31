using CoreWCF;
using CoreWCF.Configuration;
using Microsoft.EntityFrameworkCore;
using CurrencyExchangeService;
using CurrencyExchangeService.Models;

var builder = WebApplication.CreateBuilder(args);

// Add CoreWCF
builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();

// Add Database
var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "currency_exchange.db");
builder.Services.AddDbContext<ExchangeDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Register WCF service with DI
builder.Services.AddTransient<CurrencyExchangeServiceImpl>();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ExchangeDbContext>();
    db.Database.EnsureCreated();
    Console.WriteLine($"Database ready: {dbPath}");
}

// Configure WCF endpoints
app.UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<CurrencyExchangeServiceImpl>(serviceOptions =>
    {
        serviceOptions.DebugBehavior.IncludeExceptionDetailInFaults = true;
    });

    // BasicHttpBinding for easy consumption
    serviceBuilder.AddServiceEndpoint<CurrencyExchangeServiceImpl, ICurrencyExchangeService>(
        new BasicHttpBinding(),
        "/CurrencyExchange/basic"
    );

    // WSHttpBinding for WS-Security support
    serviceBuilder.AddServiceEndpoint<CurrencyExchangeServiceImpl, ICurrencyExchangeService>(
        new WSHttpBinding(SecurityMode.None),
        "/CurrencyExchange/ws"
    );
});

// Enable WSDL metadata
var serviceMetadataBehavior = app.Services.GetRequiredService<CoreWCF.Description.ServiceMetadataBehavior>();
serviceMetadataBehavior.HttpGetEnabled = true;

Console.WriteLine("=================================================");
Console.WriteLine("  Currency Exchange WCF Service");
Console.WriteLine("=================================================");
Console.WriteLine($"  WSDL:     http://localhost:5000/CurrencyExchange/basic?wsdl");
Console.WriteLine($"  Basic:    http://localhost:5000/CurrencyExchange/basic");
Console.WriteLine($"  WS:       http://localhost:5000/CurrencyExchange/ws");
Console.WriteLine("=================================================");

app.Run("http://localhost:5000");
