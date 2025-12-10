using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ppp_trade.Builders;
using ppp_trade.Mappings;
using ppp_trade.Models.Parsers;
using ppp_trade.Services;
using ppp_trade.ViewModels;

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
        services.AddSingleton<NameMappingService>();
        services.AddSingleton<OverlayWindowService>();
        services.AddSingleton<GlobalHotkeyService>();
        services.AddTransient<RequestBodyBuilder>();
        services.AddScoped<IParser, Poe1ENParser>();
        services.AddScoped<IParser, Poe1TWParser>();
        services.AddScoped<IParser, Poe2TWParser>();
        services.AddSingleton<ParserFactory>();
        services.AddTransient<OverlayRegexWindowViewModel>();
        services.AddAutoMapper(_ => { }, typeof(MappingProfile));
        services.AddLogging();
        
        services.AddTransient<SettingWindowViewModel>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        ServiceProvider = serviceCollection.BuildServiceProvider();
        base.OnStartup(e);
    }
}