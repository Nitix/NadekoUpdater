using System;
using Newtonsoft.Json;

namespace NadekoUpdater.json
{
    public class GithubReleaseModel
    {
        [JsonProperty("published_at")]
        public DateTime PublishedAt { get; set; }

        [JsonProperty("tag_name")]
        public string VersionName { get; set; }

        [JsonProperty("body")]
        public string PatchNotes { get; set; }

        public Asset[] Assets { get; set; }

        public class Asset
        {
            [JsonProperty("browser_download_url")]
            public string DownloadLink { get; set; }
        }
    }
}
