using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using ParserFortTelecom.Entity;
using testparser.Parsers.Interfaces;

namespace ParserFortTelecom.Parsers
{
    internal class TFortisParser : ISwitchParser
    {
        private readonly string FILEPATH = Path.Combine(Directory.GetCurrentDirectory(), "PriceLists");
        private const string FILENAME_PATTERN = "Прайс TFortis*.xls";
        private const string COMPANY = "TFortis";

        private class SwitchInfo
        {
            public int SfpPorts { get; set; }
            public int PoePorts { get; set; }
            public bool HasUps { get; set; }
        }

        public async Task<List<SwitchData>> ParseAsync()
        {
            try
            {
                string fileName = await FindLastFileAsync();
                if (string.IsNullOrEmpty(fileName))
                {
                    throw new FileNotFoundException("Не удалось найти подходящий файл для парсинга");
                }

                string filePath = Path.Combine(FILEPATH, fileName);
                Console.WriteLine($"Обрабатываем файл: {filePath}");

                return await ParseExcelFileAsync(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при парсинге: {ex}");
                throw;
            }
        }

        private async Task<List<SwitchData>> ParseExcelFileAsync(string filePath)
        {
            var switches = new List<SwitchData>();

            // Используем FileShare.ReadWrite для кроссплатформенной совместимости
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var workbook = new HSSFWorkbook(fileStream))
            {
                ISheet sheet = workbook.GetSheetAt(1);

                for (int i = 0; i <= sheet.LastRowNum; i++)
                {
                    IRow currentRow = sheet.GetRow(i);
                    if (currentRow == null) continue;

                    ProcessRow(currentRow, switches);
                }
            }

            return switches;
        }

        private void ProcessRow(IRow row, List<SwitchData> switches)
        {
            for (int col = 2; col < row.LastCellNum; col++)
            {
                ICell cell = row.GetCell(col);
                if (cell == null) continue;

                string cellValue = cell.ToString()?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(cellValue)) continue;

                if (cellValue.Contains("PSW") && !cellValue.Contains("Кронштейн"))
                {
                    ProcessSwitchCell(row, col, cellValue, switches);
                }
            }
        }

        private void ProcessSwitchCell(IRow row, int col, string cellValue, List<SwitchData> switches)
        {
            string title = cellValue.Replace("Коммутатор", "").Trim();
            Console.Write($"Найден коммутатор: {title} ");

            var switchInfo = ExtractSwitchInfo(cellValue);
            Console.Write($"SFP: {switchInfo.SfpPorts} PoE: {switchInfo.PoePorts} UPS: {switchInfo.HasUps} ");

            ICell priceCell = row.GetCell(col + 1);
            string priceStr = priceCell?.ToString() ?? string.Empty;
            priceStr = Regex.Replace(priceStr, "[^0-9]", ""); // Удаляем все нецифровые символы

            if (int.TryParse(priceStr, out int price))
            {
                Console.WriteLine("Цена: " + price);
                switches.Add(CreateSwitchData(title, price, switchInfo));
            }
            else
            {
                Console.WriteLine("ошибка определения цены");
            }
        }

        private SwitchData CreateSwitchData(string title, int price, SwitchInfo info)
        {
            return SwitchData.CreateSwitch(
                Company: COMPANY,
                Url: null,
                Name: title,
                Price: price,
                PoEports: info.PoePorts,
                SFPports: info.SfpPorts,
                controllable: true,
                UPS: info.HasUps
            );
        }

        private static SwitchInfo ExtractSwitchInfo(string input)
        {
            var switchInfo = new SwitchInfo();
            var regex = new Regex(@"PSW-(\d+)G(\d*)F.*(UPS)?|PSW-(\d+)G.*");
            var match = regex.Match(input);

            if (match.Success)
            {
                switchInfo.SfpPorts = !string.IsNullOrEmpty(match.Groups[1].Value)
                    ? int.Parse(match.Groups[1].Value)
                    : int.Parse(match.Groups[4].Value);

                switchInfo.PoePorts = !string.IsNullOrEmpty(match.Groups[2].Value)
                    ? int.Parse(match.Groups[2].Value)
                    : 4;

                switchInfo.HasUps = input.Contains("UPS");
            }

            return switchInfo;
        }

        private async Task<string> FindLastFileAsync()
        {
            try
            {
                Directory.CreateDirectory(FILEPATH);

                // Кроссплатформенное сравнение имен файлов
                var files = Directory.GetFiles(FILEPATH)
                    .Where(f => Path.GetFileName(f).StartsWith("Прайс TFortis", StringComparison.OrdinalIgnoreCase) &&
                               Path.GetExtension(f).Equals(".xls", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (!files.Any())
                {
                    Console.WriteLine($"Файлы не найдены. Проверьте директорию: {FILEPATH}");
                    return null;
                }

                var latestFile = files
                    .Select(f => new {
                        Path = f,
                        FileName = Path.GetFileName(f),
                        Date = ExtractDateFromFileName(f)
                    })
                    .Where(f => f.Date != null)
                    .OrderByDescending(f => f.Date)
                    .FirstOrDefault();

                return latestFile?.FileName ?? Path.GetFileName(files.First());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при поиске файла: {ex}");
                return null;
            }
        }

        private static DateTime? ExtractDateFromFileName(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            var regex = new Regex(@"(\d{2}\.\d{2}\.\d{4})|(\d{4}-\d{2}-\d{2})");
            var match = regex.Match(fileName);

            if (match.Success && DateTime.TryParse(match.Value, out DateTime date))
            {
                return date;
            }
            return null;
        }
    }
}