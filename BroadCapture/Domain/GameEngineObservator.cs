﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BroadCapture.Domain
{
    public class GameEngineObservator
    {
        private static readonly int CURRENT_GAME_MESSAGE = 0x727C40;
        private readonly VAMemory vam;
        public GameEngineObservator()
        {
            var gameProcess = Process.GetProcessesByName("ge").FirstOrDefault();
            if (gameProcess == null)
                return;
            vam = new VAMemory("ge");
            vam.ReadInt32(gameProcess.MainModule.BaseAddress);
        }
        public string ReadMessage()
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
            catch
            {
                return string.Empty;
            }
        }
        public bool TryReadBroadMessage(out string broadMessage)
        {
            var message = ReadMessage();
            if (verifyIfBroadMessage(message))
            {
                broadMessage = message;
                return true;
            }
            broadMessage = null;
            return true;
        }
        private static bool verifyIfBroadMessage(string currentMessage)
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
    }
}
