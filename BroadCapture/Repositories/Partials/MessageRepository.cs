using BroadCapture.Repositories.Based;
using BroadCapture.Models;
using System;
using RDapter;
using AndroGETrackerML.Model.Enum;

namespace BroadCapture.Repositories
{
    ///<summary>
    /// Data contractor for Message
    ///</summary>
    public partial class MessageRepository : Repository<Message>
    {
        public int ManualInsert(string currentMessage, int type, string CreateBy)
        {
            var (isBuy, isSell, isTrade) = ValidateFlagType(type);
            var sql = "INSERT INTO Message(Id,Content,Type,CreateDate,CreateBy,IsBuy,IsSell,IsTrade) VALUES(null,@content,@type,@createDate,@createBy,@isBuy,@isSell,@isTrade)";
            return this.Connector.ExecuteNonQuery(sql, new
            {
                content = currentMessage,
                type,
                createDate = DateTime.Now,
                createBy = CreateBy,
                isBuy,
                isSell,
                isTrade
            });

        }

        private (bool isBuy, bool isSell, bool isTrade) ValidateFlagType(int type)
        {
            bool isBuy = false, isSell = false, isTrade = false;
            switch ((MessageType)type)
            {
                case MessageType.Buy:
                    isBuy = true;
                    break;
                case MessageType.Sell:
                    isSell = true;
                    break;
                case MessageType.Trade:
                    isTrade = true;
                    break;
                case MessageType.BuyAndSell:
                    isBuy = true;
                    isSell = true;
                    break;
                case MessageType.SellOrTrade:
                    isSell = true;
                    isTrade = true;
                    break;
            }
            return (isBuy, isSell, isTrade);
        }
    }
}

