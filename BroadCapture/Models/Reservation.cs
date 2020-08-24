using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BroadCapture.Models
{
    public partial class Reservation
    {
        public long ownerid { get; set; }
        public string keyword { get; set; }
        public DateTime createdate { get; set; }
        public bool expired => !(DateTime.Now <= createdate.AddMinutes(expireinminute));
        public int expireinminute { get; set; }
    }
}
