using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParserFortTelecom.Parsers;
using testparser.Parsers;
using testparser;
using System.Threading;

class Program
{
    static async Task Main()
    {
        var services = new ServiceCollection();

        services.AddLogging(configure => configure
            .AddConsole()
            .SetMinimumLevel(LogLevel.Debug)
        );
        //регистрация сервисов
        services.AddSingleton<DatabaseConnection>();
        services.AddTransient<DatabaseSaver>();
        services.AddSingleton<HttpClient>();

        services.AddTransient<MasterManParser>();
        services.AddTransient<OsnovoParser>();
        services.AddTransient<NSGateParser>();
        services.AddTransient<TFortisParser>();
        services.AddTransient<RelionParser>();
        services.AddTransient<App>();

        services.AddSingleton<RabbitMqService>();

        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        try
        { //запускамся
            var rabbitMqService = serviceProvider.GetRequiredService<RabbitMqService>();
            //вызываем слушателя
            rabbitMqService.StartListening();

            logger.LogInformation("Приложение запущено и ожидает сообщения 'run' в очереди 'parser.run'...");
            logger.LogInformation("Для остановки нажмите любую клавишу...");

            await Task.Delay(Timeout.Infinite);

            rabbitMqService.StopListening();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Критическая ошибка в приложении: {Message}", ex.Message);
        }
    }
}