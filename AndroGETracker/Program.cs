using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AndroGETracker
{
    class Program
    {
        private static readonly int FAMILY_NAME = 0x7393FE;
        private static readonly int CURRENT_MAP = 0x739395;
        private static readonly int FAMILY_LEVEL = 0x750DEC;
        private static readonly int CURRENT_PLAYING_BGM = 0x6EF2E0;
        static async Task Main(string[] args)
        {
            try
            {
                handler = new ConsoleEventDelegate(ConsoleEventCallback);
                SetConsoleCtrlHandler(handler, true);
                Process GameProcess = Process.GetProcessesByName("ge").FirstOrDefault();
                if (GameProcess == null) Environment.Exit(0);
                VAMemory vam = new VAMemory("ge");
                vam.ReadInt32(GameProcess.MainModule.BaseAddress);
                DiscordRPC.EventHandlers handlers = new DiscordRPC.EventHandlers
                {
                    readyCallback = HandleReadyCallback,
                    errorCallback = HandleErrorCallback,
                    disconnectedCallback = HandleDisconnectedCallback
                };
                DiscordRPC.Initialize("720242655103156235", ref handlers, true, null);
                var presence = new DiscordRPC.RichPresence
                {
                    largeImageKey = "gicon",
                };
                presence.startTimestamp = ToUtcUnixTime(DateTime.Now);//DateTimeOffset.Now.
                while (true)
                {

                    var familyName = GetFamilyName(vam);
                    var currentMap = vam.ReadStringASCII((IntPtr)(vam.getBaseAddress + CURRENT_MAP), 255);
                    currentMap = currentMap.Substring(0, currentMap.IndexOf('\0'));
                    presence.details = $"{familyName}";
                    presence.state = $"{GetMapDescription(currentMap)}";
                    DiscordRPC.UpdatePresence(presence);
                    await Task.Delay(1000);
                }
            }
            catch
            {
                Environment.Exit(0);
            }
        }

        private static int GetFamilyLevel(VAMemory vam)
        {
            var res = vam.ReadInt32((IntPtr)(vam.getBaseAddress + FAMILY_LEVEL));
            return res;
        }

        private static string GetFamilyName(VAMemory vam)
        {
            var res = vam.ReadStringASCII((IntPtr)(vam.getBaseAddress + FAMILY_NAME), 255);
            res = res.Substring(0, res.IndexOf('\0'));

            if (string.IsNullOrWhiteSpace(res))
            {
                return "At Login Screen";
            }
            return $"{res} Family";
        }

        private static void HandleReadyCallback() { }
        private static void HandleErrorCallback(int errorCode, string message) { }
        private static void HandleDisconnectedCallback(int errorCode, string message) { }
        public static readonly DateTime UNIXTIME_ZERO_POINT = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static long ToUtcUnixTime(DateTime value)
        {
            return (long)value.ToUniversalTime().Subtract(UNIXTIME_ZERO_POINT).TotalSeconds;
        }
        static Dictionary<string, string> mapdict;
        private static string GetMapDescription(string mapcode)
        {
            if (mapdict == null)
            {
                if (File.Exists("map.json"))
                {
                    mapdict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("map.json"));
                }
                else
                {
                    Console.WriteLine($"Config file map.json is missing.");
                }
            }
            if (mapdict.TryGetValue(mapcode, out var desc))
            {
                return desc;
            }
            else
            {
                if (!mapdict.ContainsKey(mapcode))
                {
                    mapdict.Add(mapcode, "Unknown Map");
                }
                return mapcode;
            }
        }

        static bool ConsoleEventCallback(int eventType)
        {
            if (eventType == 2)
            {
                File.WriteAllText("map.json", JsonConvert.SerializeObject(mapdict));
            }
            return false;
        }
        static ConsoleEventDelegate handler;   

        private delegate bool ConsoleEventDelegate(int eventType);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);
    }
}
