using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace testparser.Entity
{
    public class loggerdto
    {
        public string message { get; set; }
        public DateTime DateTime { get; set; }
        public bool success { get; set; }
        private loggerdto(string logtext, DateTime time, bool result) { 
            message = logtext;
            DateTime = time;
            success = result;
        }
        public static loggerdto Create(string logtext, bool result) {
            return new loggerdto(logtext, DateTime.Now, result);
        }
    }
}
