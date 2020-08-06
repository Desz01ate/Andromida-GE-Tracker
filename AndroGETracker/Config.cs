using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroGETracker
{
    public class Config
    {
        public class CommandCtl
        {
            public string[] Pre { get; set; }
            public string[] Post { get; set; }
        }
        public CommandCtl Commands { get; set; }
        public static Config Instance { get; }
        static Config()
        {
            var content = File.ReadAllText("androgetracker.json");
            var inst = JsonConvert.DeserializeObject<Config>(content);
            Instance = inst;
        }
        public Config()
        {

        }
    }
}
