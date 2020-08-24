using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BroadCapture.Models
{
    public class BotRequestLog
    {
        public long uid { get; set; }
        public string username { get; set; }
        public string commandtype { get; set; }
        public string fullcommand { get; set; }
        public DateTime createdate { get; set; }
        public BotRequestLog()
        {
            createdate = DateTime.Now;
        }
    }
}
