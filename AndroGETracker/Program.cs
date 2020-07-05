using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace AndroGETracker
{
    class Program
    {
        static ConsoleEventDelegate handler;

        private delegate bool ConsoleEventDelegate(int eventType);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        const string MAP_CONFIG = "map.json";
        private static readonly int FAMILY_NAME = 0x7393FE;
        private static readonly int CURRENT_MAP = 0x739395;
        private static readonly int FAMILY_LEVEL = 0x750DEC;
        private static readonly int CURRENT_PLAYING_BGM = 0x6EF2E0;
        private static System.Timers.Timer procmon;
        static async Task Main(string[] args)
        {
            try
            {
                var handle = GetConsoleWindow();
                ShowWindow(handle, SW_HIDE);
                VerifyIfGameDirectory();
                StartGame();
                SetupMapFileWatcher();
                while (Process.GetProcessesByName("ge").FirstOrDefault() == null)
                {
                    await Task.Delay(100);
                }
                procmon = new System.Timers.Timer
                {
                    Interval = 300
                };
                procmon.Elapsed += Procmon_WatchDog;
                procmon.Start();

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
                presence.largeImageText = "https://github.com/Desz01ate/Andromida-GE-Tracker/releases";
                string latestMap = string.Empty;
                while (true)
                {

                    var familyName = GetFamilyName(vam);
                    var currentMap = GetCurrentMap(vam);
                    presence.details = familyName;
                    presence.state = GetMapDescription(currentMap);
                    if (latestMap != currentMap)
                    {
                        latestMap = currentMap;
                        presence.startTimestamp = ToUtcUnixTime(DateTime.Now);
                    }
                    DiscordRPC.UpdatePresence(presence);
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                AppendLog(ex);
            }
        }

        private static void AppendLog(Exception ex)
        {
            var logfile = "error.log";
            if (!File.Exists(logfile))
            {
                File.Create(logfile).Close();
            }
            File.AppendAllText(logfile, $"[{DateTime.Now}] {ex.ToString()}\n");
        }

        private static void SetupMapFileWatcher()
        {
            var watcher = new FileSystemWatcher();
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Path = Directory.GetCurrentDirectory();
            watcher.Filter = MAP_CONFIG;
            watcher.Changed += FileChangedConfigHandler;
            watcher.EnableRaisingEvents = true;
        }

        private static void FileChangedConfigHandler(object sender, FileSystemEventArgs e)
        {
            mapdict = null;
        }

        private static string GetCurrentMap(VAMemory vam)
        {
            var currentMap = vam.ReadStringASCII((IntPtr)(vam.getBaseAddress + CURRENT_MAP), 255);
            return currentMap.Substring(0, currentMap.IndexOf('\0'));
        }

        private static void StartGame()
        {
            var gameStartInfo = new ProcessStartInfo();
            gameStartInfo.FileName = "Granado Espada.exe";
            gameStartInfo.CreateNoWindow = true;
            gameStartInfo.UseShellExecute = false;
            Process.Start(gameStartInfo);
        }

        private static void Procmon_WatchDog(object sender, ElapsedEventArgs e)
        {
            if (Process.GetProcessesByName("ge").FirstOrDefault() == null)
            {
                Environment.Exit(0);
            }
        }

        private static void VerifyIfGameDirectory()
        {
            var expectedFile = "Granado Espada.exe";
            if (!File.Exists(expectedFile))
            {
                Console.WriteLine("This program must be place with launcher (Granado Espada.exe) in order to run.");
                Thread.Sleep(3000);
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
                if (File.Exists(MAP_CONFIG))
                {
                    mapdict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(MAP_CONFIG));
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
                File.WriteAllText(MAP_CONFIG, JsonConvert.SerializeObject(mapdict));
            }
            return false;
        }
    }
}
