using System.Collections;
using System.Collections.Generic;

namespace WindowsManager.ViewModels.Home.Scrapers
{
  public class VideoRargbtTorrent : RargbtTorrent
  {
    public string Quality { get; set; }
    public string ParsedName { get; set; }

    public IReadOnlyList<VideoRargbtTorrent> Qualities { get; set; }
  }
}