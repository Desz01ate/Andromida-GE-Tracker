using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BroadCapture.Helpers
{
    public static class DiscordColorHelpers
    {
        public static Optional<DiscordColor> GetColorForMessage(AndroGETrackerML.Model.Enum.MessageType type)
        {
            DiscordColor color;
            switch (type)
            {
                case AndroGETrackerML.Model.Enum.MessageType.Buy:
                    color = new DiscordColor(255, 196, 93);
                    break;
                case AndroGETrackerML.Model.Enum.MessageType.Sell:
                    color = new DiscordColor(6, 194, 88);
                    break;
                case AndroGETrackerML.Model.Enum.MessageType.Trade:
                    color = new DiscordColor(55, 146, 203);
                    break;

                case AndroGETrackerML.Model.Enum.MessageType.BuyAndSell:
                    color = new DiscordColor(255, 255, 255);
                    break;
                case AndroGETrackerML.Model.Enum.MessageType.SellOrTrade:
                    color = new DiscordColor(0, 0, 0);
                    break;
                case AndroGETrackerML.Model.Enum.MessageType.Other:
                default:
                    color = new DiscordColor(255, 0, 0);
                    break;
            }
            return new Optional<DiscordColor>(color);
        }
    }
}
