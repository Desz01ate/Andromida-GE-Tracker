using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BroadCapture.Models
{
    public class Preferences
    {
        public ulong UserId { get; set; }
        public int? SearchRange { get; set; }
        public int? SearchLimit { get; set; }
    }
}
