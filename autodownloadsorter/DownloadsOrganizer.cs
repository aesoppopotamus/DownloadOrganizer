// --- File: DownloadsOrganizer.cs ---
#nullable enable
using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Text;

namespace SortDownloadsTrayApp
{
    public static class DownloadsOrganizer
    {
        private const string ConfigFileName = "sortdownloads_rules.json";
        private static readonly string downloadsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)!,
            "Downloads"
        );

        // File sorting rules
        private static readonly Dictionary<string, string> fileRules = new()
        {
            { ".pdf", "Documents" },
            { ".drawio", "Documents" },
            { ".pptx", "Documents" },
            { ".docx", "Documents" },
            { ".xlsx", "Spreadsheets" },
            { ".csv", "Spreadsheets" },
            { ".exe", "Installers" },
            { ".msi", "Installers" },
            { ".zip", "ZIP Files" },
            { ".iso", "Installers" },
            { ".jpg", "Images" },
            { ".jpeg", "Images" },
            { ".png", "Images" },
            { ".gif", "GIFs" },
            { ".mp4", "Videos" },
            { ".mp3", "Audio" },
            { ".wav", "Audio" },
            { ".m4a", "Audio" },
            { ".html", "WebDownloads" },
            { ".htm", "WebDownloads" },
            { ".json", "WebDownloads" },
            { ".3mf", "BambuStudio" }
        };

        // File watcher for real-time sorting
        private static FileSystemWatcher? watcher;

        // Logging setup
        private static readonly string logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)!,
            "SortDownloadsTrayApp",
            "Logs"
        );
        private static readonly string logFilePath = Path.Combine(logDirectory, "sortdownloads.log");
        private static readonly object logLock = new object();

        static DownloadsOrganizer()
        {
            Directory.CreateDirectory(logDirectory);
        }

        private static void Log(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var line = $"{timestamp} {message}{Environment.NewLine}";
            lock (logLock)
            {
                File.AppendAllText(logFilePath, line, Encoding.UTF8);
            }
        }

        /// <summary>
        /// Runs a one-time sort of files in the Downloads folder and returns how many were moved.
        /// </summary>
        public static int Run()
        {
            Log("Starting manual sort");
            int moved = 0;
            foreach (var file in Directory.GetFiles(downloadsPath))
            {
                var ext = Path.GetExtension(file)?.ToLowerInvariant() ?? string.Empty;
                if (fileRules.TryGetValue(ext, out var folderName))
                {
                    var destDir = Path.Combine(downloadsPath, folderName);
                    Directory.CreateDirectory(destDir);
                    var fileName = Path.GetFileName(file)!;
                    var destFile = Path.Combine(destDir, fileName);

                    // Auto-rename duplicates
                    if (File.Exists(destFile))
                    {
                        var nameOnly = Path.GetFileNameWithoutExtension(fileName);
                        var extOnly = Path.GetExtension(fileName);
                        int count = 1;
                        string candidate;
                        do
                        {
                            candidate = Path.Combine(destDir, $"{nameOnly} ({count}){extOnly}");
                            count++;
                        } while (File.Exists(candidate));
                        destFile = candidate;
                    }

                    try
                    {
                        File.Move(file, destFile);
                        moved++;
                        Log($"Moved '{file}' to '{destFile}'");
                    }
                    catch (Exception ex)
                    {
                        Log($"Error moving '{file}': {ex.Message}");
                    }
                }
            }
            Log($"Manual sort complete, {moved} file(s) moved");
            return moved;
        }

        public static void StartWatcher()
        {
            if (watcher != null) return;
            watcher = new FileSystemWatcher(downloadsPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };
            watcher.Created += OnFileCreated;
            Log("Watcher started");
        }

        private static void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(e.FullPath)) return;
            var ext = Path.GetExtension(e.Name)?.ToLowerInvariant() ?? string.Empty;
            if (fileRules.TryGetValue(ext, out var folderName))
            {
                var destDir = Path.Combine(downloadsPath, folderName);
                Directory.CreateDirectory(destDir);
                var fileName = e.Name ?? Path.GetFileName(e.FullPath)!;
                var destFile = Path.Combine(destDir, fileName);

                // Auto-rename duplicates for watcher
                if (File.Exists(destFile))
                {
                    var nameOnly = Path.GetFileNameWithoutExtension(fileName);
                    var extOnly = Path.GetExtension(fileName);
                    int count = 1;
                    string candidate;
                    do
                    {
                        candidate = Path.Combine(destDir, $"{nameOnly} ({count}){extOnly}");
                        count++;
                    } while (File.Exists(candidate));
                    destFile = candidate;
                }

                try
                {
                    File.Move(e.FullPath, destFile);
                    Log($"Watcher moved '{e.FullPath}' to '{destFile}'");
                }
                catch (Exception ex)
                {
                    Log($"Watcher error moving '{e.FullPath}': {ex.Message}");
                }
            }
        }

        public static void StopWatcher()
        {
            if (watcher == null) return;
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
            watcher = null;
            Log("Watcher stopped");
        }

        public static Dictionary<string, string> GetRules() => new(fileRules);

        public static void SetRules(Dictionary<string, string> newRules)
        {
            foreach (var kv in newRules)
            {
                var ext = kv.Key;
                var folder = kv.Value?.Trim();
                if (string.IsNullOrEmpty(folder))
                    fileRules.Remove(ext);
                else
                    fileRules[ext] = folder;
            }
        }

        public static void LoadRulesFromFile()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)!;
            var configPath = Path.Combine(appData, ConfigFileName);
            if (!File.Exists(configPath)) return;
            var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(configPath));
            if (loaded != null) SetRules(loaded);
        }

        public static void SaveRulesToFile()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)!;
            var configPath = Path.Combine(appData, ConfigFileName);
            File.WriteAllText(configPath, JsonSerializer.Serialize(fileRules, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}