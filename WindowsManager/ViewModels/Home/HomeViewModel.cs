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
using System.Windows.Input;
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
using VCore.WPF;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels;
using VPlayer.AudioStorage.Scrappers.CSFD;
using VPlayer.AudioStorage.Scrappers.CSFD.Domain;

namespace WindowsManager.ViewModels.Home
{
  public class HomeViewModel : RegionViewModel<HomeView>
  {
    private readonly ILogger logger;
    private readonly TorrentsViewModel torrentsViewModel;
    private readonly ITorrentProvider torrentProvider;


    public HomeViewModel(
      IRegionProvider regionProvider,
      ILogger logger,
      ScreensManagementViewModel screensManagementViewModel,
      SoundManagerViewModel soundManagerViewModel,
      TorrentsViewModel torrentsViewModel,
        ITorrentProvider torrentProvider
     ) : base(regionProvider)
    {
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this.torrentsViewModel = torrentsViewModel ?? throw new ArgumentNullException(nameof(torrentsViewModel));
      this.torrentProvider = torrentProvider ?? throw new ArgumentNullException(nameof(torrentProvider));

      ScreensManagementViewModel = screensManagementViewModel;
      SoundManagerViewModel = soundManagerViewModel;

      torrentsViewModel.Torrents.CollectionChanged += Torrents_CollectionChanged;
    }

    private void Torrents_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      VSynchronizationContext.PostOnUIThread(() => { RargbtTorrrents = torrentsViewModel.Torrents; });
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


    #region RefreshTorrents

    private ActionCommand refreshTorrents;

    public ICommand RefreshTorrents
    {
      get
      {
        return refreshTorrents ??= new ActionCommand(OnRefreshTorrents);
      }
    }


    private void OnRefreshTorrents()
    {
      RargbtTorrrents = null;
      torrentsViewModel.LoadTorrents(true);
    }

    #endregion

    #region RemoveSoundItem

    private ActionCommand<BlankSoundDeviceViewModel> removeSoundItem;

    public ICommand RemoveSoundItem
    {
      get
      {
        return removeSoundItem ??= new ActionCommand<BlankSoundDeviceViewModel>(OnRemoveSoundItem);
      }
    }


    #endregion

    private void OnRemoveSoundItem(BlankSoundDeviceViewModel blankSoundDeviceViewModel)
    {
      SoundManagerViewModel?.RemoveKnownDevice(blankSoundDeviceViewModel);
    }
  }
}
