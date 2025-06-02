using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParserFortTelecom.Parsers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using testparser.Parsers.Interfaces;
using testparser.Parsers;

namespace testparser
{
    public class App
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        public App(IServiceProvider serviceProvider, ILogger<App> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task Run()
        {
            var dbSaver = _serviceProvider.GetRequiredService<DatabaseSaver>();

            dbSaver.falseall();
            Console.WriteLine("Данные false");

            await ProcessParser<MasterManParser>(dbSaver);
            await ProcessParser<OsnovoParser>(dbSaver);
            await ProcessParser<NSGateParser>(dbSaver);
            await ProcessParser<TFortisParser>(dbSaver);
            await ProcessParser<RelionParser>(dbSaver);
        }

        private async Task ProcessParser<TParser>(DatabaseSaver dbSaver)
            where TParser : ISwitchParser
        {
            var parserName = typeof(TParser).Name;
            try
            {
                var parser = _serviceProvider.GetRequiredService<TParser>();
                var switches = await parser.ParseAsync();
                dbSaver.SaveSwitches(switches);
                _logger.LogInformation(
                    "Парсер {ParserName} успешно обработан",
                    parserName,
                    switches.Count
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Ошибка в парсере {ParserName}: {Message}",
                    parserName,
                    ex.Message
                );
            }
        }
    }
}