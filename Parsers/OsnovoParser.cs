using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using ParserFortTelecom.Entity;
using testparser.Parsers.Interfaces;

class OsnovoParser : ISwitchParser
{
    private static readonly string FILEURL = Uri.EscapeUriString("https://osnovo.ru/general/Уличные коммутаторы.xls");
    private static readonly string FILENAME = "Уличные коммутаторы.xls";
    private static readonly string FILEPATH = Path.Combine(Directory.GetCurrentDirectory(), FILENAME);
    private static readonly string PRICEURL = "https://osnovo.ru/files/osnovo-price.xlsx";
    private static readonly string PRICEFILE = "osnovo-price.xlsx";
    private static readonly string PRICEFILEPATH = Path.Combine(Directory.GetCurrentDirectory(), PRICEFILE);
    private const string NAMECOMPANY = "OSNOVO";
    private readonly HttpClient _httpClient;

    public OsnovoParser(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
    }

    public async Task<List<SwitchData>> ParseAsync()
    {
        await DownloadFilesAsync();
        return ParseFile();
    }

    private async Task DownloadFilesAsync()
    {
        try
        {
            byte[] fileBytes = await _httpClient.GetByteArrayAsync(FILEURL);
            await File.WriteAllBytesAsync(FILEPATH, fileBytes);

            byte[] priceBytes = await _httpClient.GetByteArrayAsync(PRICEURL);
            await File.WriteAllBytesAsync(PRICEFILEPATH, priceBytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при загрузке файлов: {ex.Message}");
        }
    }

    private List<SwitchData> ParseFile()
    {
        List<SwitchData> switches = new List<SwitchData>();

        if (!File.Exists(FILEPATH))
        {
            Console.WriteLine($"Ошибка: файл {FILEPATH} не найден!");
            return switches;
        }

        try
        {
            using FileStream file = new FileStream(FILEPATH, FileMode.Open, FileAccess.Read);
            HSSFWorkbook workbook = new HSSFWorkbook(file);
            ISheet sheet = workbook.GetSheetAt(0);

            int lastColumn = sheet.GetRow(0).LastCellNum;
            for (int col = 1; col < lastColumn; col++)
            {
                string? title = sheet.GetRow(0)?.GetCell(col)?.ToString();
                string? totalPorts = sheet.GetRow(4)?.GetCell(col)?.ToString();
                if (totalPorts == "-") break;
                string? sfpPorts = sheet.GetRow(10)?.GetCell(col)?.ToString();
                string? management = sheet.GetRow(18)?.GetCell(col)?.ToString();
                bool controllable = management != "-";
                string? ups = sheet.GetRow(35)?.GetCell(col)?.ToString();
                bool UPS = ups != "-";
                string? price = GetPriceFromPriceList(title);

                if (!string.IsNullOrEmpty(title))
                {
                    Console.WriteLine();
                    Console.WriteLine("Имя: " + title);
                    //Console.WriteLine("Описание: " + description);
                    Console.WriteLine("UPS: " + ups);
                    Console.WriteLine("uplink: " + sfpPorts);
                    Console.WriteLine($"Количество портов: {totalPorts}");
                    Console.WriteLine("Контролируемый: " + controllable);
                    Console.WriteLine("цена: " + price);
                    switches.Add(SwitchData.CreateSwitch(
                        Company: NAMECOMPANY,
                        Name: title,
                        Url: FILEURL,
                        Price: int.TryParse(price, out int parsedPrice) ? parsedPrice : 0,
                        PoEports: int.TryParse(totalPorts, out int total) ? total : (int?)null,
                        SFPports: int.TryParse(sfpPorts, out int sfp) ? sfp : (int?)null,
                        controllable: controllable,
                        UPS: UPS
                        ));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при разборе файла: {ex.Message}");
        }
        return switches;
    }

    private string? GetPriceFromPriceList(string title)
    {
        if (!File.Exists(PRICEFILEPATH))
        {
            Console.WriteLine($"Ошибка: файл {PRICEFILEPATH} не найден!");
            return null;
        }

        try
        {
            using FileStream file = new FileStream(PRICEFILEPATH, FileMode.Open, FileAccess.Read);
            XSSFWorkbook workbook = new XSSFWorkbook(file);
            ISheet sheet = workbook.GetSheetAt(1);

            for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                IRow row = sheet.GetRow(rowIndex);
                if (row == null) continue;

                string? productName = row.GetCell(1)?.ToString();
                string? price = row.GetCell(5)?.ToString();

                if (!string.IsNullOrEmpty(productName) && productName.Trim() == title.Trim())
                {
                    return price;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при поиске цены: {ex.Message}");
        }
        return null;
    }
}