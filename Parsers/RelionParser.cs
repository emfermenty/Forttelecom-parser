using ParserFortTelecom.Entity;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Bcpg.Sig;
using System.Text.RegularExpressions;
using testparser.Parsers.Interfaces;

namespace ParserFortTelecom.Parsers
{
    internal class RelionParser : ISwitchParser
    {
        private static readonly string FILEURL = "https://relion-ex.ru/sites/default/files/price/yanvar2025/new/Price_Relion.xlsx";
        private static readonly string FILENAME = "Price_Relion.xlsx";
        private static readonly string FILEPATH = Path.Combine(Directory.GetCurrentDirectory(), FILENAME);
        private const string NAMECOMPANY = "РЕЛИОН";
        private readonly HttpClient _httpClient;
        public RelionParser(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        }
        public async Task<List<SwitchData>> ParseAsync()
        {
            await DownloadFilesAsync();
            return await ParseFile();
        }
        private async Task DownloadFilesAsync()
        {
            try
            {
                byte[] fileBytes = await _httpClient.GetByteArrayAsync(FILEURL);
                await File.WriteAllBytesAsync(FILEPATH, fileBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке файлов: {ex.Message}");
            }
        }
        private async Task<List<SwitchData>> ParseFile()
        {
            List<SwitchData> switches = new List<SwitchData>();
            using FileStream file = new FileStream(FILEPATH, FileMode.Open, FileAccess.Read);
            XSSFWorkbook workbook = new XSSFWorkbook(file);
            ISheet sheet = workbook.GetSheetAt(4);

            for (int row = 5; row < sheet.LastRowNum; row++)
            {
                IRow currentRow = sheet.GetRow(row);
                if (currentRow == null)
                {
                    continue;
                }

                //Console.Write($"[Строка {row + 1}]: ");
                ICell cell = currentRow.GetCell(0);
                string? columnName = cell?.ToString();
                if (columnName.Contains("SW") && columnName.Length < 50 && !columnName.Contains("Gex") && !columnName.Contains("ГЗ"))
                {
                    int sfpcount = 0;
                    var sfpMatch = Regex.Match(columnName, @"(\d+)G");
                    if (sfpMatch.Success && int.TryParse(sfpMatch.Groups[1].Value, out int sfpPorts))
                    {
                        sfpcount = sfpPorts;
                    }
                    int poecount = 0;

                    var poeMatch = Regex.Match(columnName, @"(\d+)Poe", RegexOptions.IgnoreCase);
                    if (poeMatch.Success && int.TryParse(poeMatch.Groups[1].Value, out int poePorts))
                    {
                        poecount = poePorts;
                    }
                    bool hasups = columnName.Contains("UPS", StringComparison.OrdinalIgnoreCase);

                    cell = currentRow.GetCell(2);
                    string stringcell = cell.ToString();
                    int.TryParse(stringcell, out int price);
                    Console.WriteLine(columnName + " " + sfpcount + " " + poecount + " " + hasups + " " + price);
                    switches.Add(SwitchData.CreateSwitch(
                        Company: NAMECOMPANY,
                        Name: columnName,
                        Url: null,
                        Price: price,
                        PoEports: poecount,
                        SFPports: sfpcount,
                        controllable: true,
                        UPS: hasups
                        ));
                }
            }
            return switches;
        }
    }
}
