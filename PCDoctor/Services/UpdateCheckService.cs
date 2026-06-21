using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace PCDoctor.Services
{
    public record UpdateInfo(string Version, string Url, string? AssetUrl);

    public class UpdateCheckService
    {
        private const string CurrentVersion = "0.9"; // TEST ONLY — remettre "1.0" après
        private const string ApiUrl = "https://api.github.com/repos/popolski/PCDoctor/releases/latest";

        public async Task<(UpdateInfo? Info, string? Error)> CheckAsync()
        {
            try
            {
                using var http = new HttpClient();
                http.Timeout = TimeSpan.FromSeconds(8);
                http.DefaultRequestHeaders.UserAgent.ParseAdd("PCDoctor/" + CurrentVersion);

                var release = await http.GetFromJsonAsync<GitHubRelease>(ApiUrl);
                if (release is null) return (null, "Réponse vide de l'API GitHub.");

                var latest = release.TagName?.TrimStart('v') ?? "";
                if (!IsNewer(latest, CurrentVersion)) return (null, null);

                string? assetUrl = null;
                foreach (var asset in release.Assets ?? [])
                {
                    if (asset.Name?.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        assetUrl = asset.DownloadUrl;
                        break;
                    }
                }

                return (new UpdateInfo(latest, release.HtmlUrl ?? "", assetUrl), null);
            }
            catch (Exception e)
            {
                return (null, e.Message);
            }
        }

        public async Task DownloadAsync(string url, string destPath,
            IProgress<int> progress, CancellationToken ct = default)
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("PCDoctor/" + CurrentVersion);

            using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            long total = response.Content.Headers.ContentLength ?? -1;
            using var src  = await response.Content.ReadAsStreamAsync(ct);
            using var dest = File.Create(destPath);

            var buffer = new byte[81920];
            long downloaded = 0;
            int read;
            while ((read = await src.ReadAsync(buffer, ct)) > 0)
            {
                await dest.WriteAsync(buffer.AsMemory(0, read), ct);
                downloaded += read;
                if (total > 0)
                    progress.Report((int)(downloaded * 100 / total));
            }
        }

        private static bool IsNewer(string latest, string current) =>
            Version.TryParse(latest, out var l)
            && Version.TryParse(current, out var c)
            && l > c;

        private sealed class GitHubRelease
        {
            [JsonPropertyName("tag_name")]
            public string? TagName  { get; init; }

            [JsonPropertyName("html_url")]
            public string? HtmlUrl  { get; init; }

            [JsonPropertyName("assets")]
            public List<GitHubAsset>? Assets { get; init; }
        }

        private sealed class GitHubAsset
        {
            [JsonPropertyName("name")]
            public string? Name { get; init; }

            [JsonPropertyName("browser_download_url")]
            public string? DownloadUrl { get; init; }
        }
    }
}
