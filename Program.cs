using Microsoft.Extensions.DependencyInjection;
using ParserFortTelecom.Parsers;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using testparser.Parsers.Interfaces;
using testparser.Parsers;
using testparser;
using Microsoft.Extensions.Logging;

class Program
{
    static async Task Main()
    {
        var services = new ServiceCollection();

        services.AddLogging(configure => configure
            .AddConsole() 
            .SetMinimumLevel(LogLevel.Debug) 
        );

        services.AddSingleton<DatabaseConnection>(); 
        services.AddTransient<DatabaseSaver>();     
        services.AddSingleton<HttpClient>();

        services.AddTransient<MasterManParser>();
        //services.AddTransient<ISwitchParser, MasterManParser>();
        services.AddTransient<OsnovoParser>();
       // services.AddTransient<ISwitchParser, OsnovoParser>();
        services.AddTransient<NSGateParser>();
        //services.AddTransient<ISwitchParser, NSGateParser>();
        services.AddTransient<TFortisParser>();
        //services.AddTransient<ISwitchParser, TFortisParser>();
        services.AddTransient<RelionParser>();
        //services.AddTransient<ISwitchParser, RelionParser>();
        services.AddTransient<App>();

        var serviceProvider = services.BuildServiceProvider();

        try
        {
            var app = serviceProvider.GetRequiredService<App>();
            await app.Run();
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogCritical(ex, "Критическая ошибка в приложении: {Message}", ex.Message);
        }
    }
}