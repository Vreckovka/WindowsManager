using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WindowsManager.Modularity;
using WindowsManager.ViewModels.Home.Scrapers;
using WindowsManager.ViewModels.ScreenManagement;
using WindowsManager.Views.Home;
using Logger;
using TorrentAPI;
using TorrentAPI.Domain;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels;
using VPlayer.AudioStorage.Scrappers.CSFD;

namespace WindowsManager.ViewModels.Home
{
  public class HomeViewModel : RegionViewModel<HomeView>
  {
    private readonly RargbtScrapper rargbtScrapper;
    private readonly ILogger logger;
    private readonly IViewModelsFactory viewModelsFactory;
    private readonly ICSFDWebsiteScrapper iCsfdWebsiteScrapper;

    public HomeViewModel(
      RargbtScrapper rargbtScrapper,
      IRegionProvider regionProvider,
      ILogger logger,
      IViewModelsFactory viewModelsFactory,
      ICSFDWebsiteScrapper iCsfdWebsiteScrapper,
      ScreensManagementViewModel screensManagementViewModel,
      SoundManagerViewModel soundManagerViewModel) : base(regionProvider)
    {
      this.rargbtScrapper = rargbtScrapper ?? throw new ArgumentNullException(nameof(rargbtScrapper));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.iCsfdWebsiteScrapper = iCsfdWebsiteScrapper ?? throw new ArgumentNullException(nameof(iCsfdWebsiteScrapper));
      ScreensManagementViewModel = screensManagementViewModel;
      SoundManagerViewModel = soundManagerViewModel;
    }

    public override string RegionName { get; protected set; } = RegionNames.MainContent;

    public override string Header => "Home";

    public ScreensManagementViewModel ScreensManagementViewModel { get; }

    public SoundManagerViewModel SoundManagerViewModel { get; }

    #region RargbtTorrrents

    private IEnumerable<RargbtTorrentViewModel> rargbtTorrrents;

    public IEnumerable<RargbtTorrentViewModel> RargbtTorrrents
    {
      get { return rargbtTorrrents; }
      set
      {
        if (value != rargbtTorrrents)
        {
          rargbtTorrrents = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region DateOfData

    private DateTime? dateOfData;

    public DateTime? DateOfData
    {
      get { return dateOfData; }
      set
      {
        if (value != dateOfData)
        {
          dateOfData = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion


    public override void Initialize()
    {
      base.Initialize();

      GetTorrents();
    }

    private async Task GetTorrents()
    {
      //var topTorrents = await rargbtScrapper.ScrapePage(1);
      RargbtTorrent[] topTorrents = null;
      var categories = Category.GetCategories();

      for (int i = 0; i < 50; i++)
      {
        var client = new RarbgApiClient("https://torrentapi.org/pubapi_v2.php", "my_App_ID");

        var settings = new Settings()
        {
          Limit = 50,
          Mode = Mode.List,
          Filters = new[] { Filter.None },
          Sort = Sort.Seeders
        };

        var result = await client.GetResponseAsync(settings);

        if (result.Torrents != null)
        {
          topTorrents = result.Torrents;
          logger.Log(MessageType.Success, $"SUCESS Attempt: {i + 1}");

          break;
        }

        logger.Log(MessageType.Warning, $"FAILED Attempt: {i + 1}");
      }

      if (topTorrents != null)
      {
        Application.Current.Dispatcher.Invoke(() =>
        {
          try
          {
            var categories = Category.GetCategories().ToList();

            foreach(var torrent in topTorrents)
            {
              torrent.CategoryObject = categories.SingleOrDefault(x => x.Name == torrent.Category);
            }

            var videos = topTorrents.Where(x => x.EpisodeInfo != null).Select(x => new VideoRargbtTorrent(x));
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
                   
                  }
                }

                if(parsedName == null)
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

            var other = topTorrents
              .Where(x => x.EpisodeInfo == null)
              .Select(x => new RargbtTorrentViewModel(x)).ToList();

            var oreders = other.Concat(videoVms).OrderByDescending(x => x.Model.Seeders).ToList();

            for (int i = 0; i < oreders.Count; i++)
            {
              oreders[i].SeedersOrderIndex = i + 1;
            }

            RargbtTorrrents = oreders; 

            DateOfData = rargbtScrapper.DateOfData;

            Task.Run(() =>
            {
              OnGetTorrents();
            });
          }
          catch (Exception ex)
          {
          }
        });
      }
    }

    #region OnGetTorrents

    private async void OnGetTorrents()
    {
      if (RargbtTorrrents != null)
      {
        foreach (VideoRargbtTorrentViewModel videoRargbt in RargbtTorrrents.OfType<VideoRargbtTorrentViewModel>())
        {
          try
          {
            var data = await iCsfdWebsiteScrapper.GetBestFind(videoRargbt.VideoRargbtTorrent.ParsedName, CancellationToken.None);

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

    }

    #endregion


  }

}
