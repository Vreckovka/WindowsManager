using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Logger;
using TorrentAPI;
using TorrentAPI.Domain;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.Scrappers.CSFD;
using VPlayer.AudioStorage.Scrappers.CSFD.Domain;

namespace WindowsManager.ViewModels.Torrents
{
  public class RargbtTorrentProvider : ITorrentProvider
  {
    #region Fields

    private readonly IRarbgApiClient rarbgApiClient;
    private readonly ILogger logger;
    private readonly IViewModelsFactory viewModelsFactory;
    private readonly ICSFDWebsiteScrapper iCsfdWebsiteScrapper;
    private IEnumerable<RargbtTorrentViewModel> bestTorrents;

    #endregion

    #region Contructors

    public RargbtTorrentProvider(
      IRarbgApiClient rarbgApiClient,
      ILogger logger,
      IViewModelsFactory viewModelsFactory,
      ICSFDWebsiteScrapper iCsfdWebsiteScrapper)
    {
      this.rarbgApiClient = rarbgApiClient ?? throw new ArgumentNullException(nameof(rarbgApiClient));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.iCsfdWebsiteScrapper = iCsfdWebsiteScrapper ?? throw new ArgumentNullException(nameof(iCsfdWebsiteScrapper));
    }

    #endregion

    #region Methods

    #region LoadBestTorrents

    private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

    public async Task<IEnumerable<RargbtTorrentViewModel>> LoadBestTorrents(bool forceLoad = false)
    {
      await semaphoreSlim.WaitAsync();

      try
      {
        if (!forceLoad && bestTorrents != null)
        {
          return bestTorrents;
        }

        var torrents = await GetTorrents(100);

        var vms = CreateViewModels(torrents)?.ToList();

        bestTorrents = vms;

        return bestTorrents;
      }
      catch (Exception ex)
      {
        return null;
      }
      finally 
      {
        semaphoreSlim.Release();
      }
    }

    #endregion

    #region GetTorrents

    private int maxTries = 50;
    public async Task<IEnumerable<RargbtTorrent>> GetTorrents(
      int limit = 50,
      Mode mode = Mode.List,
      Sort sort = Sort.Seeders,
      params Filter[] filters)
    {
      RargbtTorrent[] topTorrents = null;

      if (filters == null || filters.Length == 0)
      {
        filters = new Filter[] { Filter.None };
      }

      for (int i = 0; i < maxTries; i++)
      {
        var settings = new Settings()
        {
          Limit = limit,
          Mode = mode,
          Sort = sort,
          Filters = filters
        };

        var result = await rarbgApiClient.GetResponseAsync(settings);

        if (result.Torrents != null)
        {
          topTorrents = result.Torrents;

          logger.Log(MessageType.Success, $"SUCESS Attempt: {i + 1}");

          break;
        }

        logger.Log(MessageType.Warning, $"FAILED Attempt: {i + 1}");
      }

      return topTorrents;


    }

    #endregion

    #region CreateViewModels

    private IEnumerable<RargbtTorrentViewModel> CreateViewModels(IEnumerable<RargbtTorrent> torrents)
    {
      if (torrents != null)
      {
        var list = torrents.ToList();

        try
        {
          foreach (var torrent in list)
          {
            var origInfoPage = torrent.InfoPage;
            torrent.InfoPageParameter = Regex.Match(origInfoPage, @"p=(.+)").Groups[1].Value;
            torrent.InfoPageShort = Regex.Match(origInfoPage, @"__(.+)").Groups[1].Value;
          }

          var categories = Category.GetCategories().ToList();

          foreach (var torrent in list)
          {
            torrent.CategoryObject = categories.SingleOrDefault(x => x.Name == torrent.Category);
          }

          var videos = list.Where(x => x.EpisodeInfo != null).Select(x => x.GetVideoRargbtTorrent());
          var videoGroups = videos.GroupBy(x => x.EpisodeInfo.Imdb).ToList();
          var videoVms = new List<VideoRargbtTorrentViewModel>();

          foreach (var videoGroup in videoGroups)
          {
            foreach (var qualityVideo in videoGroup)
            {

              var name = qualityVideo.Title;
              string parsedName = null;
              string quality = null;

              if (!string.IsNullOrEmpty(name))
              {
                var nameRegex = new Regex(@"(.+)(\d...p)");
                var match = nameRegex.Match(name);

                if (match.Success)
                {
                  parsedName = match.Groups[1].Value?.Replace(".", " ");
                  quality = match.Groups[2].Value?.Replace(".", " ");
                }
                else
                {
                  nameRegex = new Regex(@"(.+)(\d..p)");
                  match = nameRegex.Match(name);

                  if (match.Success)
                  {
                    parsedName = match.Groups[1].Value?.Replace(".", " ");
                    quality = match.Groups[2].Value?.Replace(".", " ");
                  }
                  else
                  {
                    nameRegex = new Regex(@"(.+)WEBRip");
                    match = nameRegex.Match(name);

                    if (match.Success)
                    {
                      parsedName = match.Groups[1].Value?.Replace(".", " ");
                      quality = "WEBRip";
                    }
                  }

                }
              }

              if (parsedName == null)
              {
                qualityVideo.ParsedName = qualityVideo.Title;
              }
              else
              {
                qualityVideo.ParsedName = parsedName;
              }

              qualityVideo.Quality = quality;
            }

            var first = videoGroup.First();

            if (videoGroup.Count() > 1)
            {
              var otherQualities = videoGroup.Skip(1);

              first.Qualities = otherQualities.ToList();
            }

            var vm = viewModelsFactory.Create<VideoRargbtTorrentViewModel>(first);

            videoVms.Add(vm);
          }

          var other = list
            .Where(x => x.EpisodeInfo == null)
            .Select(x => viewModelsFactory.Create<RargbtTorrentViewModel>(x)).ToList();

          var oreders = other.Concat(videoVms).OrderByDescending(x => x.Model.Seeders).ToList();

          for (int i = 0; i < oreders.Count; i++)
          {
            oreders[i].SeedersOrderIndex = i + 1;
          }

          return oreders;
        }
        catch (Exception ex)
        {
        }
      }

      return null;
    }

    #endregion

    public void CancelDownloads()
    {
      cancellationTokens.ForEach(x => x.Cancel());
    }

    #region LoadCsfdForTorrents

    private List<CancellationTokenSource> cancellationTokens = new List<CancellationTokenSource>();
    public async Task LoadCsfdForTorrents(IEnumerable<VideoRargbtTorrentViewModel> videoRargbtTorrentViewModels)
    {
      CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

      CancelDownloads();
      cancellationTokens.Add(cancellationTokenSource);

      foreach (VideoRargbtTorrentViewModel videoRargbt in videoRargbtTorrentViewModels)
      {
        try
        {
          var data = await iCsfdWebsiteScrapper.GetBestFind(videoRargbt.VideoRargbtTorrent.ParsedName, cancellationTokenSource.Token);

          if (data is CSFDTVShow cSFDTVShow &&
              cSFDTVShow.Seasons != null &&
              cSFDTVShow.Seasons.Count(x => x.SeasonEpisodes != null) == 1 &&
              cSFDTVShow.Seasons.First(x => x.SeasonEpisodes != null).SeasonEpisodes.Count == 1)
          {
            data = cSFDTVShow.Seasons.First(x => x.SeasonEpisodes != null).SeasonEpisodes[0];
          }

          Application.Current.Dispatcher.Invoke(() =>
          {
            videoRargbt.ItemExtraData = data;
          });
        }
        catch (Exception ex)
        {
          logger.Log(ex);
        }
      }

    }

    #endregion

    #endregion
  }
}