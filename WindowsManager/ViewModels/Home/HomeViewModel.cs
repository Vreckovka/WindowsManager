using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WindowsManager.Modularity;
using WindowsManager.ViewModels.Home.Scrapers;
using WindowsManager.ViewModels.ScreenManagement;
using WindowsManager.Views.Home;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VPlayer.AudioStorage.Scrappers.CSFD;

namespace WindowsManager.ViewModels.Home
{
  public class HomeViewModel : RegionViewModel<HomeView>
  {
    private readonly RargbtScrapper rargbtScrapper;
    private readonly ICSFDWebsiteScrapper iCsfdWebsiteScrapper;

    public HomeViewModel(
      RargbtScrapper rargbtScrapper, 
      IRegionProvider regionProvider, 
      ICSFDWebsiteScrapper iCsfdWebsiteScrapper,
      ScreensManagementViewModel screensManagementViewModel,
      SoundManagerViewModel soundManagerViewModel) : base(regionProvider)
    {
      this.rargbtScrapper = rargbtScrapper ?? throw new ArgumentNullException(nameof(rargbtScrapper));
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
      var topTorrents = await rargbtScrapper.ScrapePage(1);

      Application.Current.Dispatcher.Invoke(() =>
      {
        RargbtTorrrents = topTorrents.Select(x => new RargbtTorrentViewModel(x)).ToList();
        DateOfData = rargbtScrapper.DateOfData;

        Task.Run(() =>
        {
          OnGetTorrents();
        });
      });
    }

    private async void OnGetTorrents()
    {
      if (RargbtTorrrents != null)
      {
        foreach (var item in RargbtTorrrents.Where(x => x.Model is VideoRargbtTorrent))
        {
          if (item.Model is VideoRargbtTorrent videoRargbt)
          {
            var data = await iCsfdWebsiteScrapper.GetBestFind(videoRargbt.ParsedName, CancellationToken.None);

            Application.Current.Dispatcher.Invoke(() =>
            {
              item.ItemExtraData = data;
            });
          }
        }
      }
    }
  }
}
