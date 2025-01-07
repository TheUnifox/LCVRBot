using System.Text.Json;

namespace LCVRBot.Commands
{
    public static class BotSettings
    {
        public class Settings
        {
            // the list of macros to be added to, edited, read from, and removed from
            public Dictionary<string, (string macroDescription, string macroText, string[] attachments)> macroList = [];
        }

        // static settings instance for elsewhere to use
        public static Settings settings = new();

        public static string settingsPath = Program.appdataPath + "SETTINGS.json";

        public static void Load()
        {
            //load settings from file, if it exists
            try
            {
                string json = File.ReadAllText(settingsPath);
                settings = JsonSerializer.Deserialize<Settings>(json, new JsonSerializerOptions() { IncludeFields = true, WriteIndented = true })!;
            }
            catch (Exception e)
            {
                Console.WriteLine($"No settings set, or {e.Message}");
            }
        }

        public static void Save()
        {
            // save settings to file
            string json = JsonSerializer.Serialize(settings, typeof(Settings), new JsonSerializerOptions() { IncludeFields = true, WriteIndented = true,  } );
            if (!Directory.Exists(Program.appdataPath))
                Directory.CreateDirectory(Program.appdataPath);
            File.WriteAllText(settingsPath, json);
        }
    }
}
