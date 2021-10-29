using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsManager.Modularity;
using WindowsManager.Views;
using TorrentAPI;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels;

namespace WindowsManager.ViewModels
{
  public class TorrentsViewModel : RegionViewModel<TorrentsView>
  {
    private readonly IViewModelsFactory viewModelsFactory;

    public TorrentsViewModel(IRegionProvider regionProvider, IViewModelsFactory viewModelsFactory) : base(regionProvider)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
    }

    public override string RegionName { get; protected set; } = RegionNames.MainContent;

    public override string Header => "Torrents";



    #region Torrents

    public ObservableCollection<RargbtTorrentViewModel> Torrents { get; set; } = new ObservableCollection<RargbtTorrentViewModel>();
    
    #endregion




    public override void Initialize()
    {
      base.Initialize();

      //LoadTorrents();
    }

    private async Task LoadTorrents()
    {
      //while (true)
      //{
      //  var client = new RarbgApiClient("https://torrentapi.org/pubapi_v2.php", "my_App_ID");
      //  var settings = new Settings()
      //  {
      //    Limit = 100,
      //    Mode = Mode.List,
      //    Filters = new[] { Filter.None },
      //    Sort = Sort.Seeders
      //  };
      //  var result = await client.GetResponseAsync(settings);

      //  if (result.Torrents != null)
      //  {
      //    Torrents.AddRange(result.Torrents.Select(x => viewModelsFactory.Create<RargbtTorrentViewModel>(x)));

      //    break;
      //  }
      //}

    }
  }
}
