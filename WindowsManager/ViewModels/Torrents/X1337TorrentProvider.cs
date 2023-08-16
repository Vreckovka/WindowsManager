using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChromeDriverScrapper;
using HtmlAgilityPack;
using Logger;
using TorrentAPI.Domain;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.Scrappers.CSFD;

namespace WindowsManager.ViewModels.Torrents
{
  public class X1337TorrentProvider : BaseTorrentProvider
  {
    private readonly IChromeDriverProvider chromeDriverProvider;
    private readonly IViewModelsFactory viewModelsFactory;
     
    public X1337TorrentProvider(ICSFDWebsiteScrapper iCSFDWebsiteScrapper, IChromeDriverProvider chromeDriverProvider, IViewModelsFactory viewModelsFactory, ILogger logger) : 
      base(iCSFDWebsiteScrapper, viewModelsFactory, logger)
    {
      this.chromeDriverProvider = chromeDriverProvider ?? throw new ArgumentNullException(nameof(chromeDriverProvider));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
    }

    public override async Task<IEnumerable<TorrentViewModel>> LoadBestTorrents(bool forceLoad = false)
    {
      var torrents = await GetTorrent();

      return torrents.Select(x => viewModelsFactory.Create<TorrentViewModel>(x));
    }

    public Task<IEnumerable<Torrent>> GetTorrent()
    {
      return Task.Run(() =>
       {
         var html = chromeDriverProvider.SafeNavigate("https://1337x.to/top-100", out var url);

         var document = new HtmlDocument();

         document.LoadHtml(html);

         var torrentsNodes = document.DocumentNode.SelectNodes("/html/body/main/div/div/div[2]/div/table/tbody/tr");

         foreach (var torrentNode in torrentsNodes)
         {
           var Created = torrentNode.ChildNodes[7].InnerText;
           var Size = torrentNode.ChildNodes[9].ChildNodes[0].InnerText;
         }

         var torrents = torrentsNodes.Select(torrentNode => new Torrent()
         {
           Title = torrentNode.ChildNodes[1].ChildNodes[1].InnerText,
           Seeders = int.Parse(torrentNode.ChildNodes[3].InnerText),
           Leechers = int.Parse(torrentNode.ChildNodes[5].InnerText)
         });

         return torrents;
       });

    }

    public void CancelDownloads()
    {
      throw new NotImplementedException();
    }
  }
}