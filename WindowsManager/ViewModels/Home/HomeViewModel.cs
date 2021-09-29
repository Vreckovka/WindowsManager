using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using WindowsManager.Modularity;
using WindowsManager.ViewModels.Home.Scrapers;
using WindowsManager.Views.Home;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;

namespace WindowsManager.ViewModels.Home
{
  public class HomeViewModel : RegionViewModel<HomeView>
  {
    private readonly RargbtScrapper rargbtScrapper;

    public HomeViewModel(RargbtScrapper rargbtScrapper, IRegionProvider regionProvider) : base(regionProvider)
    {
      this.rargbtScrapper = rargbtScrapper ?? throw new ArgumentNullException(nameof(rargbtScrapper));
    }

    public override string RegionName { get; protected set; } = RegionNames.MainContent;

    public override string Header => "Home";

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

      GetTorrents();
    }

    private async Task GetTorrents()
    {
      var topTorrents = await rargbtScrapper.ScrapePage(1);

      Application.Current.Dispatcher.Invoke(() =>
      {
        RargbtTorrrents = topTorrents.Select(x => new RargbtTorrentViewModel(x)).ToList();
      });
    }


  }
}
