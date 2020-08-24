using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BroadCapture.Models
{
    public class Preferences
    {
        public long userid { get; set; }
        public int? searchrange { get; set; }
        public int? searchlimit { get; set; }
    }
}
