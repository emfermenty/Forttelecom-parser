using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NPOI.HSSF.UserModel;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using ParserFortTelecom.Entity;
using testparser.Parsers.Interfaces;

namespace ParserFortTelecom.Parsers
{
    internal class TFortisParser : ISwitchParser
    {
        private string FILEPATH = Directory.GetCurrentDirectory();
        private string FILENAME = "Прайс TFortis *.xls";
        private const string COMPANY = "TFortis";
        private class SwitchInfo //для распарсинга строчки
        {
            public int SfpPorts { get; set; }
            public int PoePorts { get; set; }
            public bool HasUps { get; set; }
        }
        public async Task<List<SwitchData>> ParseAsync()
        {
            string filepath_withfile = Path.Combine(FILEPATH, FindLastFile());
            IWorkbook workbook;
            var switches = new List<SwitchData>();
            using (FileStream file = new FileStream(filepath_withfile, FileMode.Open, FileAccess.Read))
            {
                workbook = new HSSFWorkbook(file);
                ISheet sheet = workbook.GetSheetAt(1);
                int row = sheet.PhysicalNumberOfRows;
                for (int i = 0; i < row; i++)
                {
                    IRow current = sheet.GetRow(i);
                    //List<string> mas = new List<string>();
                    if (current != null)
                    {
                        for (int col = 2; col < current.PhysicalNumberOfCells; col++)
                        {
                            ICell cell = current.GetCell(col);
                            if (cell != null) // если ячейка не пустая
                            {
                                string? cellValue = cell.ToString();
                                string title = cellValue.Replace("Коммутатор", "").Trim();
                                //поиск коммутаторов
                                if (!string.IsNullOrEmpty(cellValue) && (cellValue.Contains("PSW") && !cellValue.Contains("Кронштейн")))
                                {
                                    Console.Write(title + " ");
                                    var structData = ExtractSwitchInfo(cellValue); // приватный класс
                                    Console.Write(structData.SfpPorts + " " + structData.PoePorts + " " + structData.HasUps);
                                    //Console.Write(" цена: " + current.GetCell(col + 1));
                                    string? priceString = current.GetCell(col + 1).ToString();
                                    priceString = priceString.Replace(" ", "");
                                    int intValue = 0;
                                    if (decimal.TryParse(priceString, out decimal result))
                                    {

                                        intValue = (int)result; // Преобразуем decimal в int, округляя вниз
                                        Console.WriteLine(" ЦЕНА: " + intValue);
                                    }
                                    switches.Add(new SwitchData
                                    {
                                        Company = COMPANY,
                                        Name = title,
                                        Price = intValue,
                                        PoEports = structData.PoePorts,
                                        SFPports = structData.SfpPorts,
                                        controllable = true,
                                        UPS = structData.HasUps,
                                        dateload = DateTime.Now.ToString("yyyy.MM.dd"),
                                    });
                                    Console.WriteLine();
                                }
                            }
                        }
                    }
                }
                return switches;
            }
        }
        private static SwitchInfo ExtractSwitchInfo(string input)
        {
            var switchInfo = new SwitchInfo();

            var regex = new Regex(@"PSW-(\d+)G(\d*)F.*(UPS)?|PSW-(\d+)G.*");
            var match = regex.Match(input);

            if (match.Success)
            {
                // SFP
                if (!string.IsNullOrEmpty(match.Groups[1].Value))
                {
                    switchInfo.SfpPorts = int.Parse(match.Groups[1].Value);
                }
                else if (!string.IsNullOrEmpty(match.Groups[4].Value))
                {
                    switchInfo.SfpPorts = int.Parse(match.Groups[4].Value);
                }

                // PoE
                if (!string.IsNullOrEmpty(match.Groups[2].Value))
                {
                    switchInfo.PoePorts = int.Parse(match.Groups[2].Value);
                }
                else  // Если нет PoE порта, по умолчанию ставим 4
                {
                    switchInfo.PoePorts = 4;
                }

                // UPS
                switchInfo.HasUps = input.Contains("UPS");
            }

            return switchInfo;
        }


        private string FindLastFile()
        {
            var latestFile = Directory.GetFiles(FILEPATH, FILENAME)
                .Select(f => new { FileName = Path.GetFileName(f), Date = ExtractDateFromFileName(f) })
                .Where(f => f.Date != null)
                .OrderByDescending(f => f.Date)
                .FirstOrDefault();
            if (latestFile != null)
            {
                Console.WriteLine(latestFile.FileName);
                return latestFile.FileName;
            }
            else
            {
                Console.WriteLine("Файлы не найдены.");
                return null;
            }
        }
        private static DateTime? ExtractDateFromFileName(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            Regex regex = new Regex(@"\d{2}\.\d{2}\.\d{4}");
            System.Text.RegularExpressions.Match match = regex.Match(fileName);

            if (match.Success && DateTime.TryParse(match.Value, out DateTime date))
            {
                return date;
            }
            return null;
        }
    }
}
