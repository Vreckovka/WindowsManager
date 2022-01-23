using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WindowsManager.Modularity;
using WindowsManager.Views;
using VCore.ItemsCollections;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
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
    public RxObservableCollection<RargbtTorrentViewModel> Torrents { get; } = new RxObservableCollection<RargbtTorrentViewModel>();


    public override void Initialize()
    {
      base.Initialize();

      Task.Run(async () =>
      {
        var torrents = (await torrentProvider.LoadBestTorrents())?.ToList();

        if (torrents != null)
        {
          Application.Current.Dispatcher.Invoke(() =>
          {
            Torrents.AddRange(torrents);
          });

          await torrentProvider.LoadCsfdForTorrents(torrents.OfType<VideoRargbtTorrentViewModel>());
        }
      });
    }

  }
}
