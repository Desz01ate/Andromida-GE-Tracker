using System;
using BroadCapture.Repositories;
using System.Data.SQLite;
using System.IO;
using BroadCapture.Repositories.Based;
using BroadCapture.Models;
using RDapter.Extends;
using System.Linq;
using Npgsql;

namespace BroadCapture
{
    public partial class Service : IDisposable
    {
        internal protected readonly NpgsqlConnection Connector;
        public static Service Instance { get; } = new Service();
        public Service(string connectionString)
        {
            Connector = new NpgsqlConnection(connectionString);
            ServiceCheckUp();
        }
        public Service()
        {
            Connector = new NpgsqlConnection("Server=arjuna.db.elephantsql.com;Database=wxhsmfts;User ID=wxhsmfts;Password=sVdPIcZo15cGD3W48SoayAyFNxazjMFp;Port=5432;");
            //Connector = new SQLiteConnection($@"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Local.db")};Version=3;");
            ServiceCheckUp();
        }
        private MessageRepository _Message { get; set; }
        public MessageRepository Message
        {
            get
            {
                if (_Message == null)
                {
                    _Message = new MessageRepository(this);
                }
                return _Message;
            }
        }
        private ReservationRepository _Reservation { get; set; }
        public ReservationRepository Reservation
        {
            get
            {
                if (_Reservation == null)
                {
                    _Reservation = new ReservationRepository(this);
                }
                return _Reservation;
            }
        }
        private ErrorLogRepository _ErrorLog { get; set; }
        public ErrorLogRepository ErrorLog
        {
            get
            {
                if (_ErrorLog == null)
                {
                    _ErrorLog = new ErrorLogRepository(this);
                }
                return _ErrorLog;
            }
        }
        private Repository<Preferences> _Preferences { get; set; }
        public Repository<Preferences> Preferences
        {
            get
            {
                return (_Preferences ?? new Repository<Preferences>(this.Connector));
            }
        }
        private Repository<BotRequestLog> _BotLog { get; set; }
        public Repository<BotRequestLog> BotRequestLogs
        {
            get
            {
                return (_BotLog ?? new Repository<BotRequestLog>(this.Connector));
            }
        }
        public void Dispose()
        {
            Connector?.Dispose();
        }
    }
}
