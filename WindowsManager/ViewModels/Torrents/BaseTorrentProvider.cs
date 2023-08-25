using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Logger;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.Scrappers.CSFD;
using VPlayer.AudioStorage.Scrappers.CSFD.Domain;
using VPlayer.Core.ViewModels.TvShows;

namespace WindowsManager.ViewModels.Torrents
{
  public abstract class BaseTorrentProvider : ITorrentProvider
  {
    protected readonly ICSFDWebsiteScrapper iCsfdWebsiteScrapper;
    protected readonly IViewModelsFactory viewModelsFactory;
    protected readonly ILogger logger;

    public BaseTorrentProvider(ICSFDWebsiteScrapper iCsfdWebsiteScrapper, IViewModelsFactory viewModelsFactory, ILogger logger)
    {
      this.iCsfdWebsiteScrapper = iCsfdWebsiteScrapper ?? throw new ArgumentNullException(nameof(iCsfdWebsiteScrapper));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    #region LoadCsfdForTorrents

    private List<CancellationTokenSource> cancellationTokens = new List<CancellationTokenSource>();
    public async Task LoadCsfdForTorrents(IEnumerable<TorrentViewModel> torrents)
    {
      CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

      CancelDownloads();
      cancellationTokens.Add(cancellationTokenSource);

      foreach (TorrentViewModel torrent in torrents)
      {
        try
        {
          var data = await iCsfdWebsiteScrapper.GetBestFind(torrent.Model.Title, cancellationTokenSource.Token);

          if (data is CSFDTVShow cSFDTVShow &&
              cSFDTVShow.Seasons != null &&
              cSFDTVShow.Seasons.Count(x => x.SeasonEpisodes != null) == 1 &&
              cSFDTVShow.Seasons.First(x => x.SeasonEpisodes != null).SeasonEpisodes.Count == 1)
          {
            data = cSFDTVShow.Seasons.First(x => x.SeasonEpisodes != null).SeasonEpisodes[0];
          }

          Application.Current.Dispatcher.Invoke(() =>
          {
            torrent.ItemExtraData = viewModelsFactory.Create<CSFDItemViewModel>(data);
          });
        }
        catch (Exception ex)
        {
          logger.Log(ex);
        }
      }

    }

    public abstract Task GetMagnetLinks(IEnumerable<TorrentViewModel> videoRargbtTorrentViewModels);
  

    #endregion

    public abstract Task<IEnumerable<TorrentViewModel>> LoadBestTorrents(bool forceLoad = false);

    public void CancelDownloads()
    {
      cancellationTokens.ForEach(x => x.Cancel());
    }

  }
}