using BroadCapture.Models;
using BroadCapture.Repositories.Based;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BroadCapture.Repositories
{
    public class ErrorLogRepository : Repository<ErrorLog>
    {
        private readonly Service service;
        public ErrorLogRepository(Service service) : base(service.Connector)
        {
            this.service = service;
        }
    }
}
