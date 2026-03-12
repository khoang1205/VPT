using System;
using System.IO;
using System.Text.Json;

public class AppConfig
{
    public bool HasBoss { get; set; }
    public int BossX { get; set; }
    public int BossY { get; set; }

    public bool HasSuGia { get; set; }
    public int SuGiaX { get; set; }
    public int SuGiaY { get; set; }

    public static string PathFile =>
        System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(PathFile))
            {
                string json = File.ReadAllText(PathFile);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
        }
        catch { }

        return new AppConfig();
    }

    public void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(PathFile, json);
        }
        catch { }
    }
}
