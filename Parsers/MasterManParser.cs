using HtmlAgilityPack;
using System.Text.RegularExpressions;
using ParserFortTelecom.Entity;
using testparser.Parsers.Interfaces;

public class MasterManParser : ISwitchParser
{
    private const string NAMECOMPANY = "MASTERMANN";
    private const string URL = "https://mastermann.ru/setevoe-oborudovanie/upravlyaemyie-kommutatoryi/";
    private readonly HttpClient httpClient = new HttpClient();

    public MasterManParser(HttpClient _httpClient)
    {
        httpClient = _httpClient;
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
    }

    public async Task<List<SwitchData>> ParseAsync()
    {
        List<SwitchData> switches = new();
        string html = await httpClient.GetStringAsync(URL);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var productNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'name')]/a[contains(@class, 'ani')]");
        if (productNodes == null) return switches;

        foreach (var node in productNodes)
        {
            string name = node.InnerText.Trim();
            string link = node.GetAttributeValue("href", "");

            if (!name.Contains("Коммутатор", StringComparison.OrdinalIgnoreCase)) continue;

            var details = await ParseSwitchDetails(link, name);
            if (details != null)
            {
                details.Company = NAMECOMPANY;
                switches.Add(details);
            }
        }

        return switches;
    }

    private async Task<SwitchData?> ParseSwitchDetails(string url, string name)
    {
        try
        {
            string html = await httpClient.GetStringAsync(url);
            var document = new HtmlDocument();
            document.LoadHtml(html);

            int? PoE = null, SFP = null;
            bool isUPS = name.Contains("UPS", StringComparison.OrdinalIgnoreCase);
            int price = 0;

            var ulNodes = document.DocumentNode.SelectNodes("//div[contains(@class, 'shortdescription')]//ul");
            if (ulNodes != null)
            {
                foreach (var ul in ulNodes)
                {
                    var liNodes = ul.SelectNodes(".//li");
                    if (liNodes != null)
                    {
                        foreach (var li in liNodes)
                        {
                            string feature = li.InnerText.Trim();
                            if (feature.Contains("PoE")) PoE = int.TryParse(Regex.Match(feature, @"\d+").Value, out int poe) ? poe : null;
                            if (feature.Contains("SFP")) SFP = int.TryParse(Regex.Match(feature, @"\d+").Value, out int sfp) ? sfp : null;
                        }
                    }
                }
            }

            var priceNode = document.DocumentNode.SelectSingleNode("//div[contains(@class, 'price')]");
            if (priceNode != null)
            {
                string rawPrice = priceNode.InnerText.Trim();
                //Console.WriteLine($"Сырые данные о цене: '{rawPrice}'"); 
                rawPrice = Regex.Replace(rawPrice, @"\s+", "");
                Match match = Regex.Match(rawPrice, @"\d+");
                if (match.Success)
                {
                    price = int.Parse(match.Value);
                }
                Console.WriteLine($"цена: {price}");
            }
            name.Replace("Коммутатор уличный Mastermann", name);
            return new SwitchData
            {
                Name = name,
                Url = url,
                Price = price,
                PoEports = PoE,
                SFPports = SFP,
                dateload = DateTime.Now.ToString("yyyy.MM.dd"),
                UPS = isUPS
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка парсинга: {ex.Message}");
            return null;
        }
    }
}