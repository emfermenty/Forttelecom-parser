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
    }
}