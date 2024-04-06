using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WindowsManager.Modularity;
using WindowsManager.Views;
using VCore.ItemsCollections;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.WPF;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels;

namespace WindowsManager.ViewModels.Torrents
{
  public class TorrentsViewModel : RegionViewModel<TorrentsView>
  {
    private readonly ITorrentProvider torrentProvider;

    public TorrentsViewModel(
      IRegionProvider regionProvider,
      ITorrentProvider torrentProvider) : base(regionProvider)
    {
      this.torrentProvider = torrentProvider ?? throw new ArgumentNullException(nameof(torrentProvider));
    }

    public override string RegionName { get; protected set; } = RegionNames.MainContent;
    public override string Header => "Torrents";
    public RxObservableCollection<TorrentViewModel> Torrents { get; } = new RxObservableCollection<TorrentViewModel>();


    public override void Initialize()
    {
      base.Initialize();

//#if RELEASE
      LoadTorrents();
//#endif

    }

    public void LoadTorrents(bool clear = false)
    {
      Task.Run(async () =>
      {
        if (clear)
        {
          VSynchronizationContext.PostOnUIThread(() =>
          {
            Torrents.Clear();
          });
        }

        var torrents = (await torrentProvider.LoadBestTorrents())?.OrderByDescending(x => x.Model.Seeders).ToList();

        if (torrents != null)
        {
          VSynchronizationContext.PostOnUIThread(() =>
          {
            Torrents.AddRange(torrents);
          });

          torrentProvider.GetMagnetLinks(torrents);
          torrentProvider.LoadCsfdForTorrents(torrents);
        }
      });
    }

  }
}
