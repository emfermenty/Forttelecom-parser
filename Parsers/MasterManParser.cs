using HtmlAgilityPack;
using System.Text.RegularExpressions;
using ParserFortTelecom.Entity;
using testparser.Parsers.Interfaces;
using PuppeteerSharp;

public class MasterManParser : ISwitchParser
{
    private const string NAMECOMPANY = "MASTERMANN";
    private const string URL = "https://mastermann.ru/catalog/mounting-cabinets/kompozitnyie-shkafyi/kommutatory-ulichnye/";
    private readonly HttpClient httpClient = new HttpClient();

    public MasterManParser(HttpClient _httpClient)
    {
        httpClient = _httpClient;
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
    }

    public async Task<List<SwitchData>> ParseAsync()
    {
        return await ParseSwitchDetails();
    }

    private async Task<List<SwitchData>> ParseSwitchDetails()
    {
        var switches = new List<SwitchData>();
        var browserFetcher = new BrowserFetcher();

        Console.WriteLine("Скачиваем браузер...");
        await browserFetcher.DownloadAsync();

        string executablePath = browserFetcher.GetExecutablePath(
            browserFetcher.GetInstalledBrowsers().First().BuildId
        );

        Console.WriteLine("Запускаем браузер...");
        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            ExecutablePath = executablePath,
            Timeout = 30000
        });
        Console.WriteLine("Открываем страницу...");
        await using var page = await browser.NewPageAsync();

        await page.GoToAsync(
            URL,
            new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle2 } }
        );
        await Task.Delay(5000);

        string html = await page.GetContentAsync();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var productNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'catalog-block__inner')]");

        if (productNodes == null)
            throw new Exception("Товары не найдены");

        foreach (var node in productNodes)
        {
            string fine = node.InnerText;

            Match nameMatch = Regex.Match(fine, @"Коммутатор уличный Mastermann [^\n<]+");
            string name = nameMatch.Success ? nameMatch.Value.Trim() : "Название не найдено";

            Match priceMatch = Regex.Match(fine, @"(\d+)(?:&nbsp;)?(\d+)");
            string price = priceMatch.Success
                ? $"{priceMatch.Groups[1].Value} {priceMatch.Groups[2].Value}".Trim().Replace(" ", "")
                : "Цена не найдена";

            // 3. Выводим результат
            if (name != "Название не найдено" && price != "Цена не найдено")
            {
                Console.WriteLine($"ahaha: {name} | : {price} ");
            }
            var match = Regex.Match(name,
                @"MM(\d+)G(PoE)?-(\d+)SFP(-UPS)?",
                RegexOptions.IgnoreCase);
            if (match.Success)
            {
                int portsCount = int.Parse(match.Groups[1].Value);
                bool hasPoE = match.Groups[2].Success;
                int sfpCount = int.Parse(match.Groups[3].Value);
                bool hasUPS = match.Groups[4].Success;

                Console.WriteLine($"Портов: {portsCount}");
                Console.WriteLine($"PoE: {hasPoE}");
                Console.WriteLine($"SFP: {sfpCount}");
                Console.WriteLine($"UPS: {hasUPS}");
                int finprice = int.Parse(price);
                switches.Add(SwitchData.CreateSwitch(
                                Company: NAMECOMPANY,
                                Name: name,
                                Url: URL,
                                Price: finprice,
                                PoEports: portsCount,
                                SFPports: sfpCount,
                                controllable: true,
                                UPS: hasUPS));
            }
            else
            {
                Console.WriteLine("Формат модели не распознан");
            }
        }
        return switches;
    }
    
}