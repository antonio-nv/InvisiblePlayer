using System;
using System.IO;
using System.Text.Json;

namespace PS150.UI.Windows
{
    public class AppSettings
    {
        public int Volume { get; set; } = 80;
        public string? LastFolderPath { get; set; }
        public string? LastFilePath { get; set; }
        public long LastPositionMs { get; set; } = 0;

        private static string SettingsFilePath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { /* V případě chyby vrátíme výchozí */ }

            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Chyba při ukládání nastavení: {ex.Message}");
            }
        }

    }
}