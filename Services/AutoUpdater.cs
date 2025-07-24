using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace CivilProcessERP.Services
{
    public class AutoUpdater
    {
        private readonly string _updateUrl = "https://api.github.com/repos/Great-Lakes-Civil-Services/GLCSERP/releases/latest";
        private readonly string _currentVersion = "1.0.0";
        
        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "GLERP-AutoUpdater");
                
                var response = await client.GetStringAsync(_updateUrl);
                var release = JsonSerializer.Deserialize<GitHubRelease>(response);
                
                if (release?.tag_name != null && IsNewerVersion(release.tag_name))
                {
                    return new UpdateInfo
                    {
                        Version = release.tag_name.Replace("v", ""),
                        DownloadUrl = GetDownloadUrl(release),
                        ReleaseNotes = release.body ?? "",
                        ReleaseDate = release.published_at
                    };
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update check failed: {ex.Message}");
                return null;
            }
        }
        
        private bool IsNewerVersion(string newVersion)
        {
            try
            {
                var current = Version.Parse(_currentVersion);
                var latest = Version.Parse(newVersion.Replace("v", ""));
                return latest > current;
            }
            catch
            {
                return false;
            }
        }
        
        private string GetDownloadUrl(GitHubRelease release)
        {
            var asset = release.assets?.FirstOrDefault(a => 
                a.name.Contains("Standalone") && a.name.EndsWith(".zip"));
            return asset?.browser_download_url ?? "";
        }
        
        public async Task<bool> DownloadAndInstallUpdateAsync(UpdateInfo updateInfo)
        {
            try
            {
                var tempPath = Path.GetTempPath();
                var zipPath = Path.Combine(tempPath, $"GLERP-{updateInfo.Version}.zip");
                
                // Download update
                using var client = new HttpClient();
                var zipData = await client.GetByteArrayAsync(updateInfo.DownloadUrl);
                await File.WriteAllBytesAsync(zipPath, zipData);
                
                // Extract and install
                var extractPath = Path.Combine(tempPath, $"GLERP-{updateInfo.Version}");
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath);
                
                // Launch updater
                var updaterPath = Path.Combine(extractPath, "CivilProcessERP.exe");
                if (File.Exists(updaterPath))
                {
                    Process.Start(new ProcessStartInfo(updaterPath)
                    {
                        Arguments = $"/update \"{Process.GetCurrentProcess().MainModule?.FileName}\"",
                        UseShellExecute = true
                    });
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update installation failed: {ex.Message}");
                return false;
            }
        }
    }
    
    public class UpdateInfo
    {
        public string Version { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
        public DateTime? ReleaseDate { get; set; }
    }
    
    public class GitHubRelease
    {
        public string? tag_name { get; set; }
        public string? body { get; set; }
        public DateTime? published_at { get; set; }
        public List<GitHubAsset>? assets { get; set; }
    }
    
    public class GitHubAsset
    {
        public string? name { get; set; }
        public string? browser_download_url { get; set; }
        public long size { get; set; }
    }
} 