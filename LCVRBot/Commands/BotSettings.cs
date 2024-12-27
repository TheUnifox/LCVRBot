using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LCVRBot;
using NetCord;

namespace LCVRBot.Commands
{
    public static class BotSettings
    {
        public class Settings
        {
            // the list of macros to be added to, edited, read from, and removed from
            public Dictionary<string, (string macroDescription, string macroText, Color macroColor, string? includedImage)> macroList = [];
        }

        // static settings instance for elsewhere to use
        public static Settings settings = new();

        public static void Load()
        {
            //load settings from file, if it exists
            try
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\LCVRDiscord\\SETTINGS.json";
                string json = File.ReadAllText(path);
                settings = JsonConvert.DeserializeObject<Settings>(json)!;
            }
            catch (Exception e)
            {
                Console.WriteLine($"No settings set, or {e.Message}");
            }
        }

        public static void Save()
        {
            // save settings to file
            string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\LCVRDiscord\\SETTINGS.json";
            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\LCVRDiscord\\"))
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\LCVRDiscord\\");
            File.WriteAllText(path, json);
        }
    }
}
