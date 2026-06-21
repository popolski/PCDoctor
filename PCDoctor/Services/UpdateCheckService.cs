using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace PCDoctor.Services
{
    public record UpdateInfo(string Version, string Url);

    public class UpdateCheckService
    {
        private const string CurrentVersion = "0.9"; // TEST ONLY — remettre "1.0" après
        private const string ApiUrl = "https://api.github.com/repos/popolski/PCDoctor/releases/latest";

        public async Task<UpdateInfo?> CheckAsync()
        {
            try
            {
                using var http = new HttpClient();
                http.Timeout = TimeSpan.FromSeconds(5);
                http.DefaultRequestHeaders.UserAgent.ParseAdd("PCDoctor/" + CurrentVersion);

                var release = await http.GetFromJsonAsync<GitHubRelease>(ApiUrl);
                if (release is null) return null;

                var latest = release.TagName?.TrimStart('v') ?? "";
                if (IsNewer(latest, CurrentVersion))
                    return new UpdateInfo(latest, release.HtmlUrl ?? "");

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsNewer(string latest, string current)
        {
            return Version.TryParse(latest, out var l)
                && Version.TryParse(current, out var c)
                && l > c;
        }

        private sealed class GitHubRelease
        {
            public string? TagName  { get; init; }
            public string? HtmlUrl  { get; init; }
        }
    }
}
