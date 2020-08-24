using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BroadCapture.Models
{
    public class ErrorLog
    {
        public string errordetail { get; set; }
        public DateTime createdate { get; set; }
        public ErrorLog()
        {
            createdate = DateTime.Now;
        }
        public ErrorLog(string message)
        {
            errordetail = message;
            createdate = DateTime.Now;
        }
    }
}
