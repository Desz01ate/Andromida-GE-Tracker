using BroadCapture.Models;
using RDapter;
using RDapter.Extends;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BroadCapture
{
    public partial class Service
    {
        private void ServiceCheckUp()
        {
            var messageTableCheck = CheckTableExists("Message");
            if (!messageTableCheck)
            {
                this.Connector.CreateTable<Message>();
            }
            var reservationTableCheck = CheckTableExists("Reservation");
            if (!reservationTableCheck)
            {
                this.Connector.CreateTable<Reservation>();
            }
            var errorLogTableCheck = CheckTableExists("ErrorLog");
            if (!errorLogTableCheck)
            {
                this.Connector.CreateTable<ErrorLog>();
            }
            var preferencesTableCheck = CheckTableExists("Preferences");
            if (!preferencesTableCheck)
            {
                this.Connector.CreateTable<Preferences>();
            }
            var botRequestLogCheck = CheckTableExists(nameof(BotRequestLog));
            if (!botRequestLogCheck)
            {
                this.Connector.CreateTable<BotRequestLog>();
            }
            RDapter.Global.SetSchemaConstraint<Message>(x =>
            {
                x.SetPrimaryKey<Message>(y => y.Id);
            });
            RDapter.Global.SetSchemaConstraint<Reservation>(x =>
            {
                x.SetPrimaryKey<Reservation>(y => y.OwnerId);
            });
            RDapter.Global.SetSchemaConstraint<Preferences>(x =>
            {
                x.SetPrimaryKey<Preferences>(y => y.UserId);
            });
        }

        private bool CheckTableExists(string tableName)
        {
            var checker = this.Connector.ExecuteScalar($"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}';");
            return checker != null;
        }
    }
}
