using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ppp_trade.Builders;
using ppp_trade.Mappings;
using ppp_trade.Models.Parsers;
using ppp_trade.Services;

namespace ppp_trade;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<PoeApiService>();
        services.AddSingleton<ClipboardMonitorService>();
        services.AddMemoryCache();
        services.AddSingleton<CacheService>();
        services.AddSingleton<GameStringService>();
        services.AddSingleton<RateLimitParser>();
        services.AddSingleton<IconService>();
        services.AddTransient<RequestBodyBuilder>();
        services.AddScoped<IParser, EnParser>();
        services.AddScoped<IParser, ChineseTradParser>();
        services.AddScoped<IParser, Poe2TWParser>();
        services.AddSingleton<ParserFactory>();
        services.AddAutoMapper(_ => { }, typeof(MappingProfile));
        services.AddLogging();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        ServiceProvider = serviceCollection.BuildServiceProvider();
        base.OnStartup(e);
    }
}