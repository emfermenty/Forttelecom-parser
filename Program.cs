using ParserFortTelecom.Parsers;
using testparser.Parsers;


class Program
{
    static async Task Main()
    {
        var dbConnection = new DatabaseConnection(); //подключение к бд
        var dbSaver = new DatabaseSaver(dbConnection);
        using HttpClient client = new HttpClient();

        dbSaver.falseall();
        Console.WriteLine("Данные false");

        var parsermasterman = new MasterManParser(client); //обьявляем парсер
        var switchesmasterman = await parsermasterman.ParseAsync(); // получаем данные
        dbSaver.SaveSwitches(switchesmasterman); // сохраняем в бд

        var parseOSNOVO = new OsnovoParser(client);
        var switchesOSNOVO = await parseOSNOVO.ParseAsync();
        dbSaver.SaveSwitches(switchesOSNOVO);
        switchesOSNOVO.Clear();

        var parseNSGATE = new NSGateParser(client);
        var switchesNSGATE = await parseNSGATE.ParseAsync();
        dbSaver.SaveSwitches(switchesNSGATE);

        var parseTFortis = new TFortisParser();
        var switchesTFortis = await parseTFortis.ParseAsync();
        dbSaver.SaveSwitches(switchesTFortis);

        var parseRelion = new RelionParser(client);
        var switchesRelion = await parseRelion.ParseAsync();
        dbSaver.SaveSwitches(switchesRelion);
    }
}