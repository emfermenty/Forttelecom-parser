using HtmlAgilityPack;
using ParserFortTelecom.Entity;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using testparser.Parsers.Interfaces;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Graphics;

namespace testparser.Parsers
{
    internal class NSGateParser : ISwitchParser
    {
        private const string URL = "https://nsgate.ru/nsboxes.shtml";
        private const string TITLE_COMPANY = "NSGate";
        private const string URL_PRICE = "http://www.nsgate.ru/doc/NSGate_EndUser.pdf";
        private const string PRICEFILE = "osnovo-price.xlsx";
        private static readonly string PRICEFILEPATH = Path.Combine(Directory.GetCurrentDirectory(), PRICEFILE);
        private readonly HttpClient _httpClient = new HttpClient();

        public NSGateParser(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        }

        public async Task<List<SwitchData>> ParseAsync()
        {
            try
            {
                // все кодировки
                List<SwitchData> switches = new List<SwitchData>();
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var response = await _httpClient.GetAsync(URL);
                // дебаг
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Ошибка при получении страницы. Код ошибки: " + response.StatusCode);
                    return null;
                }
                // получаем кодировку 
                var contentType = response.Content.Headers.ContentType;
                var encoding = contentType?.CharSet != null ? Encoding.GetEncoding(contentType.CharSet) : Encoding.GetEncoding("windows-1251");
                // получаем страничку(байты)
                var byteArray = await response.Content.ReadAsByteArrayAsync();
                var html = encoding.GetString(byteArray); // раскодируем

                // дебажу
                //Console.WriteLine("Полученный HTML:");
                //Console.WriteLine(html);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // необходимые таблички
                var rows = doc.DocumentNode.SelectNodes("//tr[td[contains(text(), 'Узел Доступа')]]");
                Console.WriteLine("\n" + "ВОТ ТУТ НАЧАЛО" + "\n");
                if (rows != null)
                {
                    await DownloadPDF();
                    foreach (var row in rows)
                    {
                        var cells = row.SelectNodes(".//td");
                        if (cells != null && cells.Count > 1)
                        {
                            var nameNode = cells[0].SelectSingleNode(".//a");
                            string name = "";
                            //если у нас есть тег <a>
                            if (nameNode != null)
                            {
                                name = nameNode.InnerText.Trim();
                            }
                            else //если нет тега <a>, берем br
                            {
                                var fullText = cells[0].InnerHtml;
                                var parts = fullText.Split(new[] { "<br>" }, StringSplitOptions.None);
                                name = parts[0].Trim();
                            }
                            // описание
                            var description = cells[1].InnerText.Trim();
                            // вытягиваем порты
                            Match portMatch = Regex.Match(description, @"(\d+)\s+порт");
                            int portCount = portMatch.Success ? int.Parse(portMatch.Groups[1].Value) : 0;
                            Match uplinkMatch = Regex.Match(description,
                            @"(?:^|\s)(1|2)\s+(?:TP порт uplink|1G/2\.5G SFP uplink|SFP/1G uplink|SFP uplink|uplink порт)\b");
                            if (!uplinkMatch.Success)
                            {
                                // Попробуем альтернативный вариант, если первый не сработал
                                uplinkMatch = Regex.Match(description, @"(\d+)\s*uplink");
                            }
                            int uplinkCount = uplinkMatch.Success ? int.Parse(uplinkMatch.Groups[1].Value) : 0;

                            bool ups = name.Contains("R") ? true : false;
                            int control = name.Count(char.IsDigit);
                            bool conrolable = control == 4 ? true : false;
                            Console.WriteLine("Имя: " + name);
                            //Console.WriteLine("Описание: " + description);
                            Console.WriteLine("UPS: " + ups);
                            Console.WriteLine("uplink: " + uplinkCount);
                            Console.WriteLine($"Количество портов: {portCount}");
                            Console.WriteLine("Контролируемый: " + conrolable);
                            Console.WriteLine("цена: " + await GetPriceAsync(name));
                            switches.Add(new SwitchData
                            {
                                Company = TITLE_COMPANY,
                                Name = name,
                                Url = URL,
                                Price = await GetPriceAsync(name),
                                PoEports = portCount,
                                SFPports = uplinkCount,
                                controllable = conrolable,
                                dateload = DateTime.Now.ToString("yyyy.MM.dd"),
                                UPS = ups
                            });
                        }
                    }
                    return switches;
                }
                else
                {
                    Console.WriteLine("не получается епта");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ошибка: " + ex.Message);
            }
            return null;
        }
        private async Task<int> GetPriceAsync(string name)
        {
            using var document = PdfDocument.Open(PRICEFILEPATH);

            // Начинаем с третьей страницы (индекс 3)
            for (int i = 3; i < document.NumberOfPages; i++)
            {
                var page = document.GetPage(i);
                string text = page.Text; // Извлекаем текст страницы
                string[] lines = text.Split('\n'); // Разбиваем на строки

                foreach (string line in lines)
                {
                    if (line.Contains(name)) // Ищем строку с нужным устройством
                    {
                        //Console.WriteLine($"Найдено: {line}"); // Отладка
                        var match = Regex.Match(line, $"{Regex.Escape(name)}.*?(\\d+\\s*\\d{{3}},\\d{{2}})");
                        if (match.Success)
                        {
                            string cleanedInput = match.Groups[1].Value.Replace(" ", "").Replace(",", ".");
                            if (decimal.TryParse(cleanedInput, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                            {
                                return (int)result; // Преобразуем в int
                            }
                            //return match.Groups[1].Value; // Извлекаем найденную цену
                        }
                        Console.WriteLine(line);
                    }
                    //Console.WriteLine(line + "\n");
                }
            }
            return -1;
        }

        private static string FindPriceInLine(string line)
        {
            var priceMatch = System.Text.RegularExpressions.Regex.Match(line, @"\d{1,6}(\s?\d{3})*(,\d{2})?");
            return priceMatch.Success ? priceMatch.Value : "";
        }
        private async Task DownloadPDF()
        {
            var response = await _httpClient.GetAsync(URL_PRICE);
            if (response.IsSuccessStatusCode)
            {
                await using var fs = new FileStream(PRICEFILEPATH, FileMode.Create);
                await response.Content.CopyToAsync(fs);
                Console.WriteLine("пошла возьня");
            }
            else
            {
                Console.WriteLine($"Ошибка скачивания: {response.StatusCode}");
            }
        }
    }
}