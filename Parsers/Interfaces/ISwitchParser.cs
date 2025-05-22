using ParserFortTelecom.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace testparser.Parsers.Interfaces
{
    interface ISwitchParser
    {
        Task<List<SwitchData>> ParseAsync();
    }
}
