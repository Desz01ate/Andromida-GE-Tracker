using BroadCapture;
using BroadCapture.Models;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using RDapter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Utilities.Enum;

namespace AndroGETracker
{
    class Program
    {
        #region console-hook
        static ConsoleEventDelegate handler;

        private delegate bool ConsoleEventDelegate(int eventType);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);
        static bool ConsoleEventCallback(int eventType)
        {
            if (eventType == 2)
            {
                File.WriteAllText("broadconfig.json", JsonConvert.SerializeObject(Config.Instance, Formatting.Indented));
            }
            return false;
        }

        public static readonly List<DiscordChannel> Channels = new List<DiscordChannel>();
        #endregion
        private static readonly int CURRENT_GAME_MESSAGE = 0x727C40;
        public static DiscordClient DiscordClient { get; private set; }
        public static CommandsNextExtension Commands { get; private set; }
        private static readonly Queue<Task> Queue = new Queue<Task>();
        static readonly List<ulong> shortLiveUidBuffer = new List<ulong>();

        static async Task Main(string[] args)
        {
            try
            {
                DateTime lastUpdated = DateTime.Now;
                bool archiveModeActivated = false;
                handler = new ConsoleEventDelegate(ConsoleEventCallback);
                SetConsoleCtrlHandler(handler, true);
                InitBackupSnapshotTask();
                var task = InitializeDiscordConnector();
                Process GameProcess = Process.GetProcessesByName("ge").FirstOrDefault();
                if (Config.Instance.Maintenance || GameProcess == null)
                {
                    await RunInMaintenanceMode();
                }
                VAMemory vam = new VAMemory("ge");
                vam.ReadInt32(GameProcess.MainModule.BaseAddress);
                Task.Run(ActionThread);
                string latestMessage = string.Empty;
                await InitTextChannels();
                await task;
                var queueHandlerThread = new Thread(QueueExecuteHandler);
                queueHandlerThread.Start();
                var activityObject = new DiscordActivity();
                var embedMessage = new DiscordEmbedBuilder();
                while (true)
                {
                    //archiveModeActivated = (DateTime.Now.Subtract(lastUpdated).TotalMinutes >= 10);
                    //if (archiveModeActivated)
                    //{
                    //    await Task.Delay(1000);
                    //    activityObject.Name = $"In Maintenance Mode";
                    //    await DiscordClient.UpdateStatusAsync(activityObject);
                    //}
                    //else
                    //{
                    var currentMessage = GetCurrentMessage(vam);
                    if (latestMessage != currentMessage && VerifyIfBroadMessage(currentMessage))
                    {
                        lastUpdated = DateTime.Now;
                        var message = $"[{DateTime.Now.AddHours(-7):HH:mm}] {currentMessage}";
                        var type = AndroGETrackerML.Model.ConsumeModel.Predict(currentMessage);
                        var author = ExtractCreateBy(currentMessage);
                        Service.Instance.Message.ManualInsert(currentMessage, (int)type, author);
                        embedMessage.Author = new DiscordEmbedBuilder.EmbedAuthor()
                        {
                            Name = $"{author} - {type.ToString()}"
                        };
                        embedMessage.Title = currentMessage;
                        embedMessage.Timestamp = DateTime.Now;
                        embedMessage.Color = GetColorForMessage(type);
                        lock (Channels)
                        {
                            foreach (var channel in Channels)
                            {
                                Queue.Enqueue(CheckReservation(message, channel));
                                DiscordClient.SendMessageAsync(channel, embed: embedMessage);
                            }
                            shortLiveUidBuffer.Clear();
                        }
                        activityObject.Name = $"Reading {Service.Instance.Message.Count():n0} messages now.";
                        await DiscordClient.UpdateStatusAsync(activityObject);
                        latestMessage = currentMessage;
                    }
                    //}
                    await Task.Delay(300);
                }

            }
            catch (Exception ex)
            {
                Service.Instance.ErrorLog.Insert(new ErrorLog(ex.ToString()));
                Process.Start("BroadCapture.exe");
            }
        }

        private static void InitBackupSnapshotTask()
        {
            //var timer = new System.Timers.Timer();
            //timer.Interval = 60 * 60 * 1000;//60 minutes x 60 seconds x 1000 ms
        }

        private static async Task RunInMaintenanceMode()
        {
            var activityObject = new DiscordActivity();
            while (true)
            {
                await Task.Delay(1000);
                activityObject.Name = $"In Maintenance Mode";
                await DiscordClient.UpdateStatusAsync(activityObject);
            }
        }

        private static async Task InitTextChannels()
        {
            foreach (var channelId in Config.Instance.Discord_TextChannel_Id)
            {
                var channel = await DiscordClient.GetChannelAsync(channelId);
                Channels.Add(channel);
            }
        }

        private static Optional<DiscordColor> GetColorForMessage(AndroGETrackerML.Model.Enum.MessageType type)
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

        private static async Task ActionThread()
        {
            while (true)
            {
                var key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.O:
                        Process.Start("Local.db");
                        break;
                }
                await Task.Delay(100);
            }
        }

        private static string ExtractCreateBy(string currentMessage)
        {
            var group = Regex.Match(currentMessage, @"(- \[.*?\])", RegexOptions.Compiled);
            var writer = Regex.Replace(group.Value, @"[-\[\]]", "", RegexOptions.Compiled);
            return writer;
        }

        private static async void QueueExecuteHandler()
        {
            foreach (var q in Queue)
            {
                await q;
            }
        }
        private static async Task CheckReservation(string message, DiscordChannel channel)
        {
            var guild = channel.Guild;
            foreach (var reserve in Service.Instance.Reservation)
            {
                if (reserve.Expired)
                {
                    await Service.Instance.Reservation.DeleteAsync(reserve);
                    continue;
                }
                if (message.ToLower().Contains(reserve.Keyword))
                {
                    var member = await guild.GetMemberAsync(reserve.OwnerId);
                    if (member != null && !shortLiveUidBuffer.Contains(reserve.OwnerId))
                    {
                        shortLiveUidBuffer.Add(reserve.OwnerId);
                        var embed = CommandsHandler.generateNewEmbed(reserve.Keyword, message);
                        var dm = await member.CreateDmChannelAsync();
                        await dm.SendMessageAsync(embed: embed);
                    }
                }
            }
        }

        private static bool VerifyIfBroadMessage(string currentMessage)
        {
            var condition1 = currentMessage.Contains("- [") && currentMessage.EndsWith("]");
            var condition2 = true;
            foreach (var blockedName in Config.Instance.ExcludeMessageFrom)
            {
                if (currentMessage.Contains($"[{blockedName}]"))
                {
                    condition2 = false;
                    break;
                }
            }
            return condition2 && condition1;
        }
        private static string GetCurrentMessage(VAMemory vam)
        {
            var currentMsg = vam.ReadStringASCII((IntPtr)(vam.getBaseAddress + CURRENT_GAME_MESSAGE), 255);
            try
            {
                string res;
                if (currentMsg.Contains("($?)"))
                {
                    var replace = Regex.Replace(currentMsg, "([a-zA-Z][0-9]+)|[?$]", "", RegexOptions.Compiled);
                    replace = replace.Replace("()", "");
                    res = replace;
                }
                else
                {
                    res = currentMsg;
                }
                var terminateIndex = res.IndexOf('\0');
                if (terminateIndex == -1)
                {
                    return res;
                }
                var result = res.Substring(0, res.IndexOf('\0'));
                return result;
            }
            catch (Exception ex)
            {
                Service.Instance.ErrorLog.Insert(new ErrorLog($"{ex}\nSource : {currentMsg}"));
                return string.Empty;
            }
        }


        private static async Task InitializeDiscordConnector()
        {
            DiscordClient = new DiscordClient(new DiscordConfiguration()
            {
                Token = Config.Instance.DiscordBotToken,
                TokenType = TokenType.Bot,
                LogLevel = LogLevel.Info,
                UseInternalLogHandler = true,
                AutoReconnect = true
            });
            DiscordClient.GuildDownloadCompleted += DiscordClient_GuildDownloadCompleted;
            DiscordClient.GuildCreated += DiscordClient_GuildCreatedCompleted;
            DiscordClient.GuildDeleted += DiscordClient_GuildDeletedCompleted;
            DiscordClient.ChannelCreated += DiscordClient_ChannelCreated;
            DiscordClient.ChannelUpdated += DiscordClient_ChannelUpdated;
            DiscordClient.ChannelDeleted += DiscordClient_ChannelDeleted;
            Commands = DiscordClient.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = new[] { Config.Instance.CommandPrefix },
                EnableDms = true,
                EnableMentionPrefix = true,
            });
            Commands.RegisterCommands<CommandsHandler>();
            Commands.CommandErrored += CommandErrorHandler;
            await DiscordClient.ConnectAsync();
        }

        private static Task DiscordClient_ChannelDeleted(ChannelDeleteEventArgs e)
        {
            var channel = e.Channel;
            var existingChannel = Channels.SingleOrDefault(x => x.Id == channel.Id);
            if (existingChannel != null)
            {
                Channels.Remove(existingChannel);
            }
            return Task.CompletedTask;
        }

        private static Task DiscordClient_ChannelUpdated(ChannelUpdateEventArgs e)
        {
            var channel = e.ChannelAfter;
            if (channel.Type == ChannelType.Text && channel.Name.Contains("broad") && !Channels.Any(x => x.Id == channel.Id))
            {
                Channels.Add(channel);
            }
            return Task.CompletedTask;
        }

        private static Task DiscordClient_ChannelCreated(ChannelCreateEventArgs e)
        {
            var channel = e.Channel;
            if (channel.Type == ChannelType.Text && channel.Name.Contains("broad"))
            {
                Channels.Add(channel);
            }
            return Task.CompletedTask;
        }

        private static Task DiscordClient_GuildDeletedCompleted(GuildDeleteEventArgs e)
        {
            var guild = e.Guild;
            var channel = Channels.SingleOrDefault(x => x.GuildId == guild.Id);
            Channels.Remove(channel);
            return Task.CompletedTask;
        }

        private static Task DiscordClient_GuildCreatedCompleted(GuildCreateEventArgs e)
        {
            var guild = e.Guild;
            var channel = guild.Channels.Where(x => x.Value.Name.Contains("broad")).Select(x => x.Value).FirstOrDefault();
            if (channel != null && !Channels.Any(x => x.Id == channel.Id))
            {
                lock (Channels)
                {
                    Channels.Add(channel);
                }
            }

            return Task.CompletedTask;
        }



        private static Task DiscordClient_GuildDownloadCompleted(DSharpPlus.EventArgs.GuildDownloadCompletedEventArgs e)
        {
            var guilds = e.Client.Guilds;
            foreach (var guild in guilds)
            {
                var channel = guild.Value.Channels.Where(x => x.Value.Name.Contains("broad")).Select(x => x.Value).FirstOrDefault();
                if (channel != null && !Channels.Any(x => x.Id == channel.Id))
                {
                    Channels.Add(channel);
                }
            }
            return Task.CompletedTask;
        }

        private static async Task CommandErrorHandler(CommandErrorEventArgs e)
        {
            Service.Instance.ErrorLog.Insert(new ErrorLog(e.Exception.ToString()));
        }
    }
}
