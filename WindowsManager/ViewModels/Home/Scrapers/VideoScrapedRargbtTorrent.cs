using System.Collections;
using System.Collections.Generic;

namespace WindowsManager.ViewModels.Home.Scrapers
{
  public class VideoScrapedRargbtTorrent : ScrapedRargbtTorrent
  {
    public string Quality { get; set; }
    public string ParsedName { get; set; }

    public IReadOnlyList<VideoScrapedRargbtTorrent> Qualities { get; set; }
  }
}