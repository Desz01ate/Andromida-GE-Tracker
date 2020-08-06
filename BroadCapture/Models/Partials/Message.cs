using AndroGETrackerML.Model.Enum;
using System;
namespace BroadCapture.Models
{
    public partial class Message
    {
        internal void UpdateFlag()
        {
            if (!this.Type.HasValue) return;
            switch ((MessageType)this.Type)
            {
                case MessageType.Buy:
                    this.IsBuy = true;
                    break;
                case MessageType.Sell:
                    this.IsSell = true;
                    break;
                case MessageType.Trade:
                    this.IsTrade = true;
                    break;
                case MessageType.BuyAndSell:
                    this.IsBuy = true;
                    this.IsSell = true;
                    break;
                case MessageType.SellOrTrade:
                    this.IsSell = true;
                    this.IsTrade = true;
                    break;
                default:
                    return;
            }
        }
    }
}

