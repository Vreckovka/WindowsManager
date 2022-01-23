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
using WindowsManager.ViewModels.Torrents;
using WindowsManager.Views.Home;
using ChromeDriverScrapper;
using HtmlAgilityPack;
using Logger;
using TorrentAPI;
using TorrentAPI.Domain;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels;
using VPlayer.AudioStorage.Scrappers.CSFD;
using VPlayer.AudioStorage.Scrappers.CSFD.Domain;

namespace WindowsManager.ViewModels.Home
{
  public class HomeViewModel : RegionViewModel<HomeView>
  {
    private readonly ILogger logger;
    private readonly ITorrentProvider torrentProvider;


    public HomeViewModel(
      IRegionProvider regionProvider,
      ILogger logger,
      ScreensManagementViewModel screensManagementViewModel,
      SoundManagerViewModel soundManagerViewModel,
        ITorrentProvider torrentProvider
     ) : base(regionProvider)
    {
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this.torrentProvider = torrentProvider ?? throw new ArgumentNullException(nameof(torrentProvider));

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

    public override void Initialize()
    {
      base.Initialize();


      Task.Run(async () =>
      {
        var torrents = await torrentProvider.LoadBestTorrents();

        if (torrents != null)
        {
          Application.Current.Dispatcher.Invoke(() =>
          {
            RargbtTorrrents = torrents;
          });
        }
      });
    }

  }

}
