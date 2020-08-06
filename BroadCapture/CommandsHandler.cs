using AndroGETrackerML.Model.Enum;
using BroadCapture;
using BroadCapture.Extensions;
using BroadCapture.Models;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using RDapter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Threading.Tasks;
using Utilities.Shared;

namespace AndroGETracker
{
    public partial class CommandsHandler : BaseCommandModule
    {
        [Command("sell")]
        [Description("search for specific sell from all broads available.")]
        public async Task Sell(CommandContext ctx, [RemainingText] string keyword)
        {
            await Service.Instance.BotRequestLogs.InsertAsync(new BotRequestLog()
            {
                Uid = ctx.Message.Author.Id,
                Username = ctx.Message.Author.Username,
                CommandType = "SELL",
                FullCommand = $"!sell {keyword}"
            });
            await performOperationTask(ctx, keyword, MessageType.Sell);
        }
        [Command("buy")]
        [Description("search for specific buy from all broads available.")]
        public async Task Buy(CommandContext ctx, [RemainingText] string keyword)
        {
            await Service.Instance.BotRequestLogs.InsertAsync(new BotRequestLog()
            {
                Uid = ctx.Message.Author.Id,
                Username = ctx.Message.Author.Username,
                CommandType = "BUY",
                FullCommand = $"!buy {keyword}"
            });
            await performOperationTask(ctx, keyword, MessageType.Buy);
        }
        [Command("trade")]
        [Description("search for specific trade from all broads available.")]
        public async Task Trade(CommandContext ctx, [RemainingText] string keyword)
        {
            await Service.Instance.BotRequestLogs.InsertAsync(new BotRequestLog()
            {
                Uid = ctx.Message.Author.Id,
                Username = ctx.Message.Author.Username,
                CommandType = "TRADE",
                FullCommand = $"!trade {keyword}"
            });
            await performOperationTask(ctx, keyword, MessageType.Trade);
        }
        [Command("search")]
        [Description("search for specific broads within search keyword criteria")]
        public async Task Search(CommandContext ctx, [RemainingText] string keyword)
        {
            await Service.Instance.BotRequestLogs.InsertAsync(new BotRequestLog()
            {
                Uid = ctx.Message.Author.Id,
                Username = ctx.Message.Author.Username,
                CommandType = "SEARCH",
                FullCommand = $"!search {keyword}"
            });
            await performOperationTask(ctx, keyword, MessageType.Other);
        }
        [Command("subscribe")]
        [Description("Subscribe to specific keyword and notify when related item is showing up in broad.")]
        public async Task Reserve(CommandContext ctx, [RemainingText] string keyword)
        {
            await Service.Instance.BotRequestLogs.InsertAsync(new BotRequestLog()
            {
                Uid = ctx.Message.Author.Id,
                Username = ctx.Message.Author.Username,
                CommandType = "SUBSCRIBE",
                FullCommand = $"!subscribe {keyword}"
            });
            var author = ctx.Message.Author;
            var id = author.Id;
            int expireIn = 180;
            if (string.IsNullOrWhiteSpace(keyword))
            {
                await ctx.RespondAsync("subscribe item keyword must not be empty.");
                return;
            }
            keyword = keyword.ToLower();
            var dm = await ctx.Member.CreateDmChannelAsync();
            var isKeywordExist = Service.Instance.Reservation.Any(x => x.OwnerId == ctx.Member.Id && !x.Expired);
            if (isKeywordExist)
            {
                if (keyword == "cancel")
                {
                    Service.Instance.Reservation.Delete(ctx.Member.Id);
                    await dm.SendMessageAsync("Previous subscription has been cancelled.");
                }
                else
                {
                    await dm.SendMessageAsync($"There is already existing subscription in queue, please cancel it first and try again later.");
                }
                return;
            }
            if (keyword == "cancel")
            {
                return;
            }
            Service.Instance.Reservation.Insert(new BroadCapture.Models.Reservation()
            {
                OwnerId = ctx.Member.Id,
                Keyword = keyword,
                CreateDate = DateTime.Now,
                ExpireInMinute = expireIn
            });
            await ctx.Channel.SendDisposableMessageAsync($"{ctx.User.Mention} please check your direct message.");
            await dm.SendMessageAsync($"{keyword} has been registered, subscription will be expires in {expireIn} minutes.");
            //await ctx.Message.DeleteAsync();
        }
        [Command("preference")]
        [Description("Set preferences that is fit for your personal usage.")]
        public async Task Preferences(CommandContext ctx, string key, string value)
        {
            await Service.Instance.BotRequestLogs.InsertAsync(new BotRequestLog()
            {
                Uid = ctx.Message.Author.Id,
                Username = ctx.Message.Author.Username,
                CommandType = "PREFERENCES",
                FullCommand = $"!preference {key} {value}"
            });
            try
            {
                Preferences preferences = Service.Instance.Preferences.Query(x => x.UserId == ctx.Message.Author.Id).FirstOrDefault();
                if (preferences == null)
                {
                    preferences = new Preferences();
                    preferences.UserId = ctx.Message.Author.Id;
                    Service.Instance.Preferences.Insert(preferences);
                }
                switch (key)
                {
                    case "range":
                        preferences.SearchRange = int.Parse(value);
                        break;
                    case "limit":
                        preferences.SearchLimit = int.Parse(value);
                        break;
                    default:
                        break;
                }
                Service.Instance.Preferences.Update(preferences);
                await ctx.RespondAsync("Preferences saved.");
            }
            catch (Exception ex)
            {
                await Service.Instance.ErrorLog.InsertAsync(new ErrorLog(ex.ToString()));
                await ctx.RespondAsync($"There is an error occured when processing command.");
            }
        }
        private async Task performOperationTask(CommandContext ctx, string keyword, MessageType type)
        {
            Service.Instance.ErrorLog.Insert(new BroadCapture.Models.ErrorLog()
            {
                ErrorDetail = $"{ctx.Message.Author.Username} : {ctx.Message}"
            });
            var author = ctx.Message.Author;
            var id = author.Id;
            string appendMessage = string.Empty;
            try
            {
                var stw = new System.Diagnostics.Stopwatch();
                stw.Start();
                string filter, broadType;
                System.Text.StringBuilder keywordsFilter = new System.Text.StringBuilder();
                switch (type)
                {
                    case MessageType.Sell:
                        filter = $"Type IN ({(int)MessageType.Sell},{(int)MessageType.SellOrTrade},{(int)MessageType.BuyAndSell})";
                        broadType = "sell messages";
                        break;
                    case MessageType.Buy:
                        filter = $"Type IN ({(int)MessageType.Buy},{(int)MessageType.BuyAndSell})";
                        broadType = "buy messages";
                        break;
                    case MessageType.Trade:
                        filter = $"Type IN ({(int)MessageType.Trade},{(int)MessageType.SellOrTrade})";
                        broadType = "trade messages";
                        break;
                    default:
                        filter = "1 = 1";
                        broadType = "everything";
                        break;
                }
                var preference = Service.Instance.Preferences.Query(x => x.UserId == author.Id).FirstOrDefault();
                var keywords = keyword.Split(',');
                var range = preference?.SearchRange ?? 1;
                var limit = preference?.SearchLimit ?? 10;
                var param = new List<RDapter.Entities.DatabaseParameter>();
                var firstAppend = true;
                for (var i = 0; i < keywords.Length; i++)
                {
                    var bindingKey = string.Empty;
                    if (firstAppend)
                    {
                        firstAppend = false;
                        bindingKey = "AND";
                    }
                    else
                    {
                        bindingKey = "OR";
                    }
                    var kw = keywords[i].Trim();
                    keywordsFilter.AppendLine($" {bindingKey} LOWER(content) LIKE @keyword{i}");
                    param.Add(new RDapter.Entities.DatabaseParameter($"keyword{i}", $"%{kw.ToLower()}%"));
                }
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    await ctx.RespondAsync("search keyword must not be empty.");
                    return;
                }
                var retry = 0;
                List<Message> result;
                var query = $@"SELECT L.*
                                   FROM Message L
                                   INNER JOIN (
                                   	SELECT Id,MAX(CreateDate) AS Latest
                                   	FROM Message
                                   	WHERE {filter}
                                       {keywordsFilter}
                                       AND CreateDate >= @date
                                   	GROUP BY CreateBy
                                   ) R
                                   ON L.CreateDate = R.Latest AND L.Id = R.Id
                                   ORDER BY CreateDate DESC 
                                   LIMIT {limit}";
                do
                {
                    var copyParam = param.ConvertAll(x => x);
                    range += (retry * 30);
                    copyParam.Add(new RDapter.Entities.DatabaseParameter("date", DateTime.Today.AddDays(-range)));
                    result = Service.Instance.Connector.ExecuteReader<BroadCapture.Models.Message>(new RDapter.Entities.ExecutionCommand(query, copyParam)).GroupBy(x => x.Content).Select(x => x.First()).AsList();
                } while (result.Count == 0 && retry++ < 2);
                if (retry == 1)
                {
                    appendMessage = " (Automatic Extended Range)";
                }
                if (result.Count() == 0)
                {
                    await ctx.Channel.SendDisposableMessageAsync($"No data related to '{keyword}' found.");
                    return;
                }
                stw.Stop();
                Action<DiscordEmbedBuilder> replyAction;
                if (ctx.Member != null)
                {
                    var dmChannel = await ctx.Member.CreateDmChannelAsync();
                    replyAction = async (s) =>
                    {
                        try
                        {
                            await dmChannel.SendMessageAsync("", embed: s);
                        }
                        catch
                        {
                            await ctx.RespondAsync("", embed: s);
                        }
                    };
                }
                else
                {
                    replyAction = async (s) =>
                    {
                        await ctx.RespondAsync("", embed: s);
                    };
                }
                var now = DateTime.Now;
                var embed = generateNewEmbed(broadType, stw, range, limit, appendMessage);
                foreach (var broad in result)
                {
                    var prefixTime = string.Empty;
                    var diff = now.Subtract(broad.CreateDate.Value);
                    if (diff.Days > 0)
                    {
                        prefixTime = $"{diff.Days} days ago";
                    }
                    else if (diff.Hours > 0)
                    {
                        prefixTime = $"{diff.Hours} hours ago";
                    }
                    else
                    {
                        prefixTime = $"{diff.Minutes} minutes ago";
                    }
                    if (embed.Fields.Count == 25)
                    {
                        replyAction(embed);
                        embed = generateNewEmbed(broadType, stw, range, limit, appendMessage);
                    }
                    embed.AddField($"{prefixTime} by {broad.CreateBy}", broad.Content);
                }
                replyAction(embed);
            }
            finally
            {
                //await ctx.Message.DeleteAsync();
            }
            DiscordEmbedBuilder generateNewEmbed(string broadType, Stopwatch stw, int range, int limit, string extendMessage = "")
            {
                var rnd = new Random();
                var embed = new DiscordEmbedBuilder();
                var r = Random(0, 255);
                var g = Random(0, 255);
                var b = Random(0, 255);
                embed.Color = new Optional<DiscordColor>(new DiscordColor(r, g, b));
                embed.Title = $"Result for {broadType} related to {keyword} :";
                embed.Footer = new DiscordEmbedBuilder.EmbedFooter()
                {
                    Text = $"Brought to you by Coalescense with love <3 ({(float)stw.ElapsedMilliseconds / 1000} seconds processed)\nwith {range} days{extendMessage} {limit} messages preset.",
                    IconUrl = "https://cdn.discordapp.com/avatars/322051347505479681/87eb411421d1f89dc9f29196ac670862.png?size=64"
                };
                return embed;
            }

        }
        readonly static Random rand = new Random();
        static byte Random(byte min, byte max)
        {
            return (byte)((rand.NextDouble() * (max - min)) + min);
        }
    }
}