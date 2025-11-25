#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace GaussianSplatting.Editor
{
    [Serializable]
    public class SpzAsset
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("browser_download_url")]
        public string BrowserDownloadUrl { get; set; }
    }

    [Serializable]
    public class SpzVersionResponse
    {
        [JsonProperty("tag_name")]
        public string TagName { get; set; }

        [JsonProperty("assets")]
        public SpzAsset[] Assets { get; set; }
    }

    public static class SpzUpdater
    {
        private static readonly string RepoUrl = "https://api.github.com/repos/404-Repo/spz/releases/latest";
        private static readonly string VersionFilePath = Path.Combine(Application.dataPath, "Editor/SPZ/spz_version.txt");

        private static readonly HttpClient http = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = true
        });

        static SpzUpdater()
        {
            http.DefaultRequestHeaders.Add("User-Agent", "Unity-SPZ-Updater");
        }

        private static string GetAssetName()
        {
            switch (SystemInfo.operatingSystemFamily)
            {
                case OperatingSystemFamily.Windows:
                    return "spz-windows.zip";
                case OperatingSystemFamily.MacOSX:
                    return "spz-macos.zip";
                case OperatingSystemFamily.Linux:
                    return "spz-linux.zip";
                default:
                    throw new Exception("Unsupported platform for SPZ");
            }
        }

        public static async Task<bool> NeedUpdate()
        {
            var latest = await GetLatestVersion();
            var current = GetCurrentVersion();
            return string.IsNullOrEmpty(current) || current != latest.TagName;
        }

        public static async Task Update()
        {
            try
            {
                var latest = await GetLatestVersion();
                string current = GetCurrentVersion();

                Debug.Log($"Current SPZ version: {current}");
                Debug.Log($"Latest SPZ version: {latest.TagName}");

                if (current == latest.TagName)
                {
                    Debug.Log("SPZ is already up to date.");
                    return;
                }

                string assetName = GetAssetName();
                var asset = Array.Find(latest.Assets, a => a.Name == assetName);
                if (asset == null)
                    throw new Exception($"Asset {assetName} not found in latest release.");

                string pluginDir = Path.Combine(Application.dataPath, "Editor/SPZ");
                if (!Directory.Exists(pluginDir))
                    Directory.CreateDirectory(pluginDir);
                string zipPath = Path.Combine(pluginDir, assetName);

                using (var response = await http.GetAsync(asset.BrowserDownloadUrl))
                {
                    response.EnsureSuccessStatusCode();
                    await using (var fs = File.Create(zipPath))
                        await response.Content.CopyToAsync(fs);
                }

                ZipFile.ExtractToDirectory(zipPath, pluginDir, overwriteFiles: true);
                File.Delete(zipPath);

                File.WriteAllText(VersionFilePath, latest.TagName);
                Debug.Log($"SPZ updated to version {latest.TagName}");

                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error updating SPZ: {ex.Message}");
            }
        }

        private static string GetCurrentVersion()
        {
            if (!File.Exists(VersionFilePath))
                return null;
            return File.ReadAllText(VersionFilePath).Trim();
        }

        private static async Task<SpzVersionResponse> GetLatestVersion()
        {
            var response = await http.GetStringAsync(RepoUrl);
            var data = JsonConvert.DeserializeObject<SpzVersionResponse>(response);
            return data;
        }
    }
}
#endif
