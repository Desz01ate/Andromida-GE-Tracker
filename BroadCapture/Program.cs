using BroadCapture;
using BroadCapture.Domain;
using BroadCapture.Helpers;
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
        #endregion
        private static readonly Queue<Task> Queue = new Queue<Task>();
        static readonly List<ulong> shortLiveUidBuffer = new List<ulong>();

        static async Task Main(string[] args)
        {
            try
            {
                handler = new ConsoleEventDelegate(ConsoleEventCallback);
                SetConsoleCtrlHandler(handler, true);
                InitBackupSnapshotTask();
                Task.Run(ActionThread);
                var queueHandlerThread = new Thread(QueueExecuteHandler);
                queueHandlerThread.Start();
                var activityObject = new DiscordActivity();
                var embedMessage = new DiscordEmbedBuilder();

                var discordClientFactory = await DiscordClientFactory.CreateAsync();
                var runner = new BroadCaptureRunner();
                runner.BroadCaptured += async (broadMessage) =>
                {
                    var type = AndroGETrackerML.Model.ConsumeModel.Predict(broadMessage);
                    if (type == AndroGETrackerML.Model.Enum.MessageType.Other && Config.Instance.DisabledOtherMessage)
                        return;
                    var author = StringHelpers.ExtractCreateBy(broadMessage);
                    await Service.Instance.Message.ManualInsertAsync(broadMessage, (int)type, author);
                    embedMessage.Author = new DiscordEmbedBuilder.EmbedAuthor()
                    {
                        Name = $"{author} - {type.ToString()}"
                    };
                    embedMessage.Title = broadMessage;
                    embedMessage.Timestamp = DateTime.Now;
                    embedMessage.Color = DiscordColorHelpers.GetColorForMessage(type);
                    foreach (var channel in discordClientFactory.GetDiscordChannels())
                    {
                        Queue.Enqueue(CheckReservation(broadMessage, channel, type));
                        discordClientFactory.Client.SendMessageAsync(channel, embed: embedMessage);
                    }
                    shortLiveUidBuffer.Clear();
                    activityObject.Name = $"Reading {Service.Instance.Message.Count():n0} messages now.";
                    await discordClientFactory.Client.UpdateStatusAsync(activityObject);
                    await Task.Delay(300);
                };
                await runner.RunAsync(new CancellationTokenSource());
                await Task.Delay(-1);
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

        private static async void QueueExecuteHandler()
        {
            foreach (var q in Queue)
            {
                await q;
            }
        }
        private static async Task CheckReservation(string message, DiscordChannel channel, AndroGETrackerML.Model.Enum.MessageType messageType)
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
                        var embed = DiscordEmbedHelpers.GenerateEmbedMessage($"Notification for {reserve.Keyword}", message, "Brought to you by Coalescense with love <3", "https://cdn.discordapp.com/avatars/322051347505479681/87eb411421d1f89dc9f29196ac670862.png?size=64", DiscordColorHelpers.GetColorForMessage(messageType));
                        var dm = await member.CreateDmChannelAsync();
                        await dm.SendMessageAsync(embed: embed);
                    }
                }
            }
        }
    }
}
