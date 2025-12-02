using Clio.Utilities.Collections;
using Magitek.Commands;
using Magitek.Models.MagitekApi;
using Magitek.Utilities;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace Magitek.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class MagitekApi
    {
        private static MagitekApi _instance;
        private readonly HttpClient _webClient = new HttpClient();
        private const string GithubAddress = "https://api.github.com";
        private const string VersionUrl = "https://github.com/MagitekRB/MagitekRoutine/releases/latest/download/Version.txt";
        private DateTime _lastFetchTime = DateTime.MinValue;

        public static MagitekApi Instance => _instance ?? (_instance = new MagitekApi());
        public bool SpinnerVisible { get; set; } = false;
        public AsyncObservableCollection<MagitekNews> NewsList { get; set; }
        public MagitekVersion MagitekVersion { get; set; }
        public ICommand RefreshNewsList => new DelegateCommand(UpdateNews);

        private class Release
        {
            public string tag_name { get; set; }
            public string name { get; set; }
            public string body { get; set; }
            public DateTime created_at { get; set; }
            public bool prerelease { get; set; }
        }

        public MagitekApi()
        {
            SpinnerVisible = true;
            try
            {
                NewsList = new AsyncObservableCollection<MagitekNews>();
                UpdateVersion();
                UpdateNews();
            }
            catch (Exception)
            { }
            SpinnerVisible = false;
        }

        public async void UpdateVersion()
        {
            var local = "UNKNOWN";
            var distant = "UNKNOWN";

            try
            {
                local = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, $@"Routines\Magitek\Version.txt"));
            }
            catch
            {
                Logger.Error("Can't read local Magitek version. Please reinstall it");
            }

            try
            {
                distant = await _webClient.GetStringAsync(VersionUrl);
            }
            catch
            {
                Logger.Error("Can't read distant Magitek version. Please reinstall it");
            }

            // Trim whitespace and compare versions
            var localTrimmed = local?.Trim() ?? string.Empty;
            var distantTrimmed = distant?.Trim() ?? string.Empty;
            var isOutOfSync = !string.IsNullOrEmpty(localTrimmed) &&
                             !string.IsNullOrEmpty(distantTrimmed) &&
                             localTrimmed != distantTrimmed &&
                             localTrimmed != "UNKNOWN" &&
                             distantTrimmed != "UNKNOWN";

            MagitekVersion = new MagitekVersion()
            {
                LocalVersion = local,
                DistantVersion = distant,
                IsOutOfSync = isOutOfSync
            };
        }

        private async void UpdateNews()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri(GithubAddress);
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Anything");
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var response = await httpClient.GetFromJsonAsync<List<Release>>("repos/MagitekRB/MagitekRoutine/releases");

                    if (response == null)
                        return;

                    NewsList.Clear();
                    response.ForEach(x =>
                    {
                        if (x?.prerelease == true)
                            return;

                        // Process the body to remove commit hashes and PR links
                        var bodyLines = x.body?.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

                        var filteredBodyLines = new List<string>();
                        var isInWhatsChanged = false;
                        var isInChanges = false;

                        foreach (var line in bodyLines)
                        {
                            // Check for "## What's Changed" (PR merge format)
                            if (line.StartsWith("## What's Changed"))
                            {
                                isInWhatsChanged = true;
                                continue;
                            }

                            // Check for "## Changes" (direct commit format)
                            if (line.StartsWith("## Changes"))
                            {
                                isInChanges = true;
                                continue;
                            }

                            // Handle "## What's Changed" section (PR merges)
                            if (isInWhatsChanged)
                            {
                                if (line.StartsWith("##") || line.StartsWith("**"))
                                {
                                    break; // We've hit the next section
                                }

                                if (line.StartsWith("* "))
                                {
                                    // Remove the bullet point
                                    var lineWithoutBullet = line.Substring(2).Trim();
                                    // Keep the contributor but remove the PR link
                                    var lineWithoutPRLink = Regex.Replace(lineWithoutBullet, @" in https:\/\/github\.com\/[^ ]+$", "");
                                    filteredBodyLines.Add(lineWithoutPRLink.Trim());
                                }
                            }

                            // Handle "## Changes" section (direct commits)
                            if (isInChanges)
                            {
                                if (line.StartsWith("##") || line.StartsWith("**"))
                                {
                                    break; // We've hit the next section
                                }

                                // Add all non-empty lines in the Changes section
                                if (!string.IsNullOrWhiteSpace(line))
                                {
                                    filteredBodyLines.Add(line.Trim());
                                }
                            }
                        }

                        // If no changes were found, add the no details message
                        if (!filteredBodyLines.Any())
                        {
                            filteredBodyLines.Add("No detailed changes available.");
                        }

                        var filteredBody = string.Join(Environment.NewLine, filteredBodyLines);

                        NewsList.Add(new MagitekNews
                        {
                            Created = x.created_at.ToString("d"),
                            Title = x.name,
                            Message = filteredBody
                        });
                    });

                    _lastFetchTime = DateTime.Now;
                }
                ;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }
            SpinnerVisible = false;
        }

        public void RefreshIfNeeded()
        {
            // Always refresh both news and version when UI is shown to ensure it's up to date
            UpdateNews();
            UpdateVersion();
        }
    }
}
