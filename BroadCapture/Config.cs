﻿using Newtonsoft.Json;
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
        public List<ulong> Discord_TextChannel_Id { get; set; }
        public string[] ExcludeMessageFrom { get; set; }
        public string DiscordBotToken { get; set; }
        public string CommandPrefix { get; set; }
        public static Config Instance { get; }
        public bool Maintenance { get; set; }

        static Config()
        {
            var content = File.ReadAllText("broadconfig.json");
            var inst = JsonConvert.DeserializeObject<Config>(content);
            Instance = inst;
        }
        public Config()
        {

        }
    }
}
