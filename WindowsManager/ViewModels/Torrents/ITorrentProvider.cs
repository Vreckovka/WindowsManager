using System.Collections.Generic;
using System.Threading.Tasks;
using TorrentAPI;
using TorrentAPI.Domain;

namespace WindowsManager.ViewModels.Torrents
{
  public interface ITorrentProvider
  {
    Task<IEnumerable<TorrentViewModel>> LoadBestTorrents(bool forceLoad = false);

    Task LoadCsfdForTorrents(IEnumerable<TorrentViewModel> videoRargbtTorrentViewModels);

    void CancelDownloads();
  }
}