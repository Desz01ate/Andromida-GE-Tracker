using BroadCapture.Models;
using BroadCapture.Repositories.Based;
using RDapter;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BroadCapture.Repositories
{
    public class ReservationRepository : Repository<Reservation>
    {
        private Service service;
        public ReservationRepository(Service service) : base(service.OnlineConnector)
        {
            this.service = service;
        }
        public IEnumerable<Reservation> GetReservationByKeyword(string keyword)
        {
            var param = keyword.ToLower();
            var query = $@"SELECT * FROM Reservation WHERE lower(Keyword) LIKE @keyword";
            return service.OnlineConnector.ExecuteReader<Reservation>(query, new
            {
                keyword = $"%{param}%"
            });
        }
    }
}
