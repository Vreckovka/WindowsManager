using Newtonsoft.Json;

namespace TorrentAPI.Domain
{
    public class TorrentResults
    {
        [JsonProperty("torrent_results")]
        public RargbtTorrent[] Torrents { get; set; }
        public string Error { get; set; }
        [JsonProperty("error_code")]
        public int ErrorCode { get; set; }
    }
}
