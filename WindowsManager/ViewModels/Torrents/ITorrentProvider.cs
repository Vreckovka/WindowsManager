using System.Collections.Generic;
using System.Threading.Tasks;
using TorrentAPI;
using TorrentAPI.Domain;

namespace WindowsManager.ViewModels.Torrents
{
  public interface ITorrentProvider
  {
    Task<IEnumerable<RargbtTorrentViewModel>>  LoadBestTorrents(bool forceLoad = false);

    Task<IEnumerable<RargbtTorrent>> GetTorrents(
      int limit = 50,
      Mode mode = Mode.List,
      Sort sort = Sort.Seeders,
      params Filter[] filters);

    Task LoadCsfdForTorrents(IEnumerable<VideoRargbtTorrentViewModel> videoRargbtTorrentViewModels);

    void CancelDownloads();
  }
}