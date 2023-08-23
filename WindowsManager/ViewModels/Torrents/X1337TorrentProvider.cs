using ChromeDriverScrapper;
using HtmlAgilityPack;
using Logger;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TorrentAPI.Domain;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VPlayer.AudioStorage.InfoDownloader;
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
      var torrents = (await GetTorrent()).ToList();

      List<TorrentViewModel> list = new List<TorrentViewModel>();

      var otherTorrents = torrents
       .Where(x => !x.CategoryObject.IsVideoCategory)
       .Select(x => viewModelsFactory.Create<TorrentViewModel>(x)).ToList();


      var videoTorrents = torrents
        .Where(x => x.CategoryObject.IsVideoCategory)
        .Select(x => viewModelsFactory.Create<VideoTorrentViewModel>(x)).ToList();

     

      list.AddRange(videoTorrents);
      list.AddRange(otherTorrents);

      return list.OrderBy(x => x.Model.OrderNumber);
    }

    public Task<IEnumerable<Torrent>> GetTorrent()
    {
      var url = "https://1337x.to";

      return Task.Run(() =>
       {
         var html = chromeDriverProvider.SafeNavigate($"{url}/top-100", out var rurl);

         var document = new HtmlDocument();

         document.LoadHtml(html);

         var torrentsNodes = document.DocumentNode.SelectNodes("/html/body/main/div/div/div[2]/div/table/tbody/tr");

         var torrents = torrentsNodes.Select(torrentNode =>
         {
           {
             string category = "";
             int categoryId = 0;

             try
             {
               category = torrentNode.ChildNodes[1].ChildNodes[0].Attributes[0].Value;
               categoryId = int.Parse(Regex.Match(category, @"\d+").Groups[0].Value);
             }
             catch (Exception)
             {
             }

             var categoryObject = new TorrentCategory()
             {
               Url = $"{url}/{category}",
               Id = categoryId,
             };

             var torrent = categoryObject.IsVideoCategory ? new VideoTorrent() : new Torrent();

             var urlParameter = torrentNode.ChildNodes[1].ChildNodes[1].Attributes[0].Value;

             torrent.Title = torrentNode.ChildNodes[1].ChildNodes[1].InnerText;
             torrent.Seeders = int.Parse(torrentNode.ChildNodes[3].InnerText);
             torrent.Leechers = int.Parse(torrentNode.ChildNodes[5].InnerText);
             torrent.Size = FromStringToLongFileSize(torrentNode.ChildNodes[9].ChildNodes[0].InnerText);
             torrent.InfoPage = $"{url}{urlParameter}";
             torrent.CreatedString = torrentNode.ChildNodes[7].InnerText;

             torrent.InfoPageShort = Regex.Match(urlParameter, @"(.+)\/.+\/").Groups[1].Value;

             torrent.CategoryObject = categoryObject;

             if (torrent.CategoryObject.IsVideoCategory)
             {
               torrent.CategoryObject.Url =$"https://rargb.to/static/images/categories/cat_new{18}.gif";
             } 
             else if(torrent.CategoryObject.Id != 48)
             {
               torrent.CategoryObject.Url = $"https://rargb.to/static/images/categories/cat_new{(int)0x21}.gif";
             }
             else
             {
               torrent.CategoryObject.Url = $"https://rargb.to/static/images/categories/cat_new4.gif";
             }

             return torrent;
           }
         }).ToList();

         var i = 0;

         torrents.ForEach(z =>
         {
           z.OrderNumber = ++i;
         });

         var videoTorrents = torrents.OfType<VideoTorrent>().ToList();

         foreach (var videoTorrent in videoTorrents)
         {
           var name = videoTorrent.Title;
           string parsedName = "";
           string quality = "";

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
                 else
                 {
                   nameRegex = new Regex(@"(.+)WEB");
                   match = nameRegex.Match(name);

                   if (match.Success)
                   {
                     parsedName = match.Groups[1].Value?.Replace(".", " ");
                     quality = "WEB";
                   }
                 }
               }
             }
           }

           videoTorrent.Quality = quality;
           videoTorrent.ParsedName = parsedName;
         }

         var videoGroups = videoTorrents.GroupBy(x => AudioInfoDownloader.GetClearName(x.ParsedName.ToLower()));

         foreach (var group in videoGroups)
         {
           var first = group.First();
           var groupL = group.ToList();

           first.Qualities = groupL.Skip(1).Take(groupL.Count - 1);
         }

         return torrents.OrderBy(x => x.OrderNumber).AsEnumerable();
       });

    }

    private long FromStringToLongFileSize(string value)
    {
      var split = value.Split(" ");

      double doubleValue = 0;
      double multiplicator = 0;

      if (split.Length > 1 && double.TryParse(split[0], out doubleValue))
      {
        string sizeUnit = split[1].ToLower();

        if (sizeUnit == "b")
        {
          multiplicator = 1;
        }
        else if (sizeUnit == "kb")
        {
          multiplicator = 1000.0;
        }
        else if (sizeUnit == "mb")
        {
          multiplicator = 1000000.0;
        }
        else if (sizeUnit == "gb")
        {
          multiplicator = 1000000000.0;
        }
      }


      return (long)(doubleValue * multiplicator);
    }


    public void CancelDownloads()
    {
      throw new NotImplementedException();
    }
  }
}