using BroadCapture;
using DSharpPlus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace AndroGETracker
{
    class Program
    {
        private static readonly int CURRENT_GAME_MESSAGE = 0x727C40;
        private static System.Timers.Timer procmon;
        public static DiscordClient DiscordClient { get; private set; }
        static async Task Main(string[] args)
        {
            try
            {
                var task = InitializeDiscordConnector();

                Process GameProcess = Process.GetProcessesByName("ge").FirstOrDefault();
                if (GameProcess == null) Environment.Exit(0);
                VAMemory vam = new VAMemory("ge");
                vam.ReadInt32(GameProcess.MainModule.BaseAddress);

                string latestMessage = string.Empty;
                await task;
                var channel = await DiscordClient.GetChannelAsync(Config.Instance.Discord_TextChannel_Id);
                while (true)
                {
                    var currentMessage = GetCurrentMessage(vam);
                    if (latestMessage != currentMessage && VerifyIfBroadMessage(currentMessage))
                    {
                        var message = $"[{DateTime.Now.AddHours(-7).ToString("HH:mm")}] {currentMessage}";
                        await DiscordClient.SendMessageAsync(channel, message);
                        latestMessage = currentMessage;
                    }
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                Environment.Exit(0);
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
            //bool isTradingMessage = false;
            //foreach (var word in required)
            //{
            //    if (currentMsg.Contains(word))
            //    {
            //        isTradingMessage = true;
            //        break;
            //    }
            //}
            //if (!isTradingMessage) return "";
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
            var result = res.Substring(0, res.IndexOf('\0'));
            return result;
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
            await DiscordClient.ConnectAsync();
        }
        private static void HandleReadyCallback() { }
        private static void HandleErrorCallback(int errorCode, string message) { }
        private static void HandleDisconnectedCallback(int errorCode, string message) { }
    }
}
