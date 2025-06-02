using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserFortTelecom.Entity
{
    public class SwitchData
    {
        public string Company { get; set; }
        public string Name { get; set; }
        public string? Url { get; set; }
        public int Price { get; set; }
        public int? PoEports { get; set; }
        public int? SFPports { get; set; }
        public bool controllable { get; set; }
        public bool? UPS { get; set; }
        public string dateload { get; set; }
        private SwitchData(string Company, string Name, string? Url, int Price, int? PoEports, int? SFPports, bool controllable, bool? UPS, string dateload)
        {
            this.Company = Company;
            this.Name = Name;
            this.Url = Url;
            this.Price = Price;
            this.PoEports = PoEports;
            this.SFPports = SFPports;
            this.controllable = controllable;
            this.UPS = UPS;
            this.dateload = dateload;
        }
        public static SwitchData CreateSwitch(string Company, string Name, string? Url, int Price, int? PoEports, int? SFPports, bool controllable, bool? UPS)
        {
            string date = DateTime.Now.ToString("yyyy.MM.dd");
            return new SwitchData(Company, Name, Url, Price, PoEports, SFPports, controllable, UPS, date);
        }
    }
}