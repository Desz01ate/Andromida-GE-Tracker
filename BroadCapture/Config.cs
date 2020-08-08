using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BroadCapture
{
    public class Config
    {
        private const string CONFIG_FILE = "broadconfig.json";
        public List<ulong> Discord_TextChannel_Id { get; set; }
        public string[] ExcludeMessageFrom { get; set; }
        public string DiscordBotToken { get; set; }
        public string CommandPrefix { get; set; }
        public static Config Instance { get; private set; }
        public bool Maintenance { get; set; }
        public bool DisabledOtherMessage { get; set; }
        static Config()
        {
            InitInstance();
        }

        private static void InitInstance()
        {
            var content = File.ReadAllText(CONFIG_FILE);
            var inst = JsonConvert.DeserializeObject<Config>(content);
            Instance = inst;
        }

        public Config()
        {

        }
        private static void SetupConfigWatcher()
        {
            var watcher = new FileSystemWatcher();
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Path = Directory.GetCurrentDirectory();
            watcher.Filter = CONFIG_FILE;
            watcher.Changed += FileChangedConfigHandler;
            watcher.EnableRaisingEvents = true;
        }

        private static void FileChangedConfigHandler(object sender, FileSystemEventArgs e)
        {
            InitInstance();
            Console.WriteLine($"[{DateTime.Now}] Configuration file changed detected.");
        }
    }
}
