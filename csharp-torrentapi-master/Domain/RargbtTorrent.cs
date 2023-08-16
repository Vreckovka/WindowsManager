using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using VCore.Standard;

namespace TorrentAPI.Domain
{
  public class Torrent
  {
    public string Title { get; set; }
    public long Size { get; set; }
    public int Seeders { get; set; }
    public int Leechers { get; set; }
    public DateTime Created { get; set; }
  }
  public class RargbtTorrent : Torrent
  {
    public string Category { get; set; }
    public string Download { get; set; }
    public string PUpdate { get; set; }
    [JsonProperty("episode_info")]
    public EpisodeInfo EpisodeInfo { get; set; }
    public int Ranked { get; set; }
    [JsonProperty("info_page")]
    public string InfoPage { get; set; }
    public string InfoPageShort { get; set; }
    public string InfoPageParameter { get; set; }
    public Category CategoryObject { get; set; }

    #region InfoPageFakeUrl

    public string InfoPageFakeUrl
    {
      get
      {
        return "https://torrentapi.org/redirect_to_info.php?" + "p=" + InfoPageParameter;
      }
    }

    #endregion

    public VideoRargbtTorrent GetVideoRargbtTorrent()
    {
      return new VideoRargbtTorrent()
      {
        Download = Download,
        Category = Category,
        EpisodeInfo = EpisodeInfo,
        InfoPage = InfoPage,
        InfoPageParameter = InfoPageParameter,
        CategoryObject = CategoryObject,
        InfoPageShort = InfoPageShort,
        Leechers = Leechers,
        ParsedName = null,
        PUpdate = PUpdate,
        Ranked = Ranked,
        Seeders = Seeders,
        Size = Size,
        Title = Title
      };
    }
  }

  public class VideoRargbtTorrent : RargbtTorrent
  {
    public string ParsedName { get; set; }
    public string Quality { get; set; }
    public IEnumerable<VideoRargbtTorrent> Qualities { get; set; }
  }


  public enum SizeUnit
  {
    B,
    KB,
    MB,
    GB,
    TB
  }
}
