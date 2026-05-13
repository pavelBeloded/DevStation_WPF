using DevStation.Data.DbContext;
using DevStation.Services.Implementations;
using DevStation.Services.Interfaces;
using DevStation.ViewModels;
using DevStation.Views.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows;

namespace DevStation;

public partial class App : Application
{
    private ServiceProvider _serviceProvider = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var services = new ServiceCollection();

        services.AddDbContext<DevStationDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        services.AddHttpClient("MDN", client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "DevStation/1.0");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISnippetService, SnippetService>();
        services.AddScoped<IMdnSearchService, MdnSearchService>();
        services.AddScoped<IAccountService, AccountService>();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainWindowViewModel>(sp => new MainWindowViewModel(
            sp.GetRequiredService<ICurrentUserService>(),
            sp.GetRequiredService<IAuthService>(),
            sp.GetRequiredService<ISnippetService>(),
            sp.GetRequiredService<IMdnSearchService>(),
            sp.GetRequiredService<IAccountService>(),
            sp.GetRequiredService<Func<LoginWindow>>()));

        services.AddTransient<LoginWindow>(sp =>
        {
            var window = new LoginWindow();
            window.DataContext = sp.GetRequiredService<LoginViewModel>();
            return window;
        });

        services.AddTransient<MainWindow>(sp =>
        {
            var window = new MainWindow();
            window.DataContext = sp.GetRequiredService<MainWindowViewModel>();
            return window;
        });

        // Register factories for circular-safe navigation
        services.AddTransient<Func<LoginWindow>>(sp => () => sp.GetRequiredService<LoginWindow>());
        services.AddTransient<Func<MainWindow>>(sp => () => sp.GetRequiredService<MainWindow>());

        _serviceProvider = services.BuildServiceProvider();

        await EnsureDatabaseAsync();

        // Пробуем восстановить сессию — если успешно, открываем главное окно без логина
        var sessionRestored = false;
        using (var scope = _serviceProvider.CreateScope())
        {
            var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
            sessionRestored = await auth.TryRestoreSessionAsync();
        }

        if (sessionRestored)
            _serviceProvider.GetRequiredService<MainWindow>().Show();
        else
            _serviceProvider.GetRequiredService<LoginWindow>().Show();
    }

    private async Task EnsureDatabaseAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DevStationDbContext>();
        await db.Database.MigrateAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider.Dispose();
        base.OnExit(e);
    }
}
