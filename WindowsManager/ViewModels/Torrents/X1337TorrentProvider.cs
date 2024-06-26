﻿using ChromeDriverScrapper;
using HtmlAgilityPack;
using Logger;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TorrentAPI.Domain;
using VCore;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.WPF;
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

    public override Task GetMagnetLinks(IEnumerable<TorrentViewModel> videoRargbtTorrentViewModels)
    {
      return Task.Run(() =>
      {
        foreach (var torrent in videoRargbtTorrentViewModels)
        {

          torrent.Model.Download = GetMagnetLink(torrent.Model.InfoPage);

          if (torrent is VideoTorrentViewModel videoTorrent)
          {
            foreach (var qualityTorrent in videoTorrent.Qualities)
            {
              qualityTorrent.Model.Download = GetMagnetLink(qualityTorrent.Model.InfoPage);

              VSynchronizationContext.PostOnUIThread(() => qualityTorrent.downloadCommand?.RaiseCanExecuteChanged());
            }
          }

          VSynchronizationContext.PostOnUIThread(() => torrent.downloadCommand?.RaiseCanExecuteChanged());
        }
      });
    }

    private string GetMagnetLink(string url)
    {
      var html = new WebClient().DownloadString(url);

      var document = new HtmlDocument();

      document.LoadHtml(html);

      var magnetNode = document.DocumentNode.SelectSingleNode("/html/body/main/div/div/div/div[2]/div[1]/ul[1]/li[1]/a");

      return magnetNode.Attributes.SingleOrDefault(x => x.Name == "href")?.Value;
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

      videoTorrents.Where(x => x.VideoTorrent.Qualities.Any()).ForEach(x => { x.Qualities = x.VideoTorrent.Qualities.Select(y => viewModelsFactory.Create<VideoTorrentViewModel>(y)); });

      list.AddRange(videoTorrents);
      list.AddRange(otherTorrents);

      return list;
    }

    public Task<IEnumerable<Torrent>> GetTorrent()
    {
      var url = "https://1337x.to";

      return Task.Run(() =>
      {
        var link = $"{url}/top-100";
        var html = new WebClient().DownloadString(link);
        //var html = chromeDriverProvider.SafeNavigate(link, out var rurl);

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

            torrent.Title = Uri.UnescapeDataString(torrentNode.ChildNodes[1].ChildNodes[1].InnerText);
            torrent.Seeders = int.Parse(torrentNode.ChildNodes[3].InnerText);
            torrent.Leechers = int.Parse(torrentNode.ChildNodes[5].InnerText);
            torrent.Size = FromStringToLongFileSize(torrentNode.ChildNodes[9].ChildNodes[0].InnerText);
            torrent.InfoPage = $"{url}{urlParameter}";
            torrent.CreatedString = torrentNode.ChildNodes[7].InnerText;

            torrent.InfoPageShort = Regex.Match(urlParameter, @"(.+)\/.+\/").Groups[1].Value;

            torrent.CategoryObject = categoryObject;

            if (torrent.CategoryObject.IsVideoCategory)
            {
              torrent.CategoryObject.Url = $"https://rargb.to/static/images/categories/cat_new{18}.gif";
            }
            else if (torrent.CategoryObject.Id != 48)
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
              var matchName = match.Groups[1].Value.Replace(" ", ".");
              nameRegex = new Regex(@"(.+)\..*?(\d+)");
              var nameMatch = nameRegex.Match(matchName);

              if (nameMatch.Groups[2].Value?.Replace(".", " ").Length == 4)
              {
                parsedName = nameMatch.Groups[1].Value?.Replace(".", " ");
              }
              else
              {
                parsedName = match.Groups[1].Value?.Replace(".", " ");
              }

              quality = match.Groups[2].Value?.Replace(".", " ");
            }
            else
            {
              nameRegex = new Regex(@"(.+)(\d..p)");
              match = nameRegex.Match(name);

              if (match.Success)
              {
                var matchName = match.Groups[1].Value.Replace(" ", ".");
                nameRegex = new Regex(@"(.+)\..*?(\d+)");
                var nameMatch = nameRegex.Match(matchName);

                if (nameMatch.Groups[2].Value?.Replace(".", " ").Length == 4)
                {
                  parsedName = nameMatch.Groups[1].Value?.Replace(".", " ");
                }
                else
                {
                  parsedName = match.Groups[1].Value?.Replace(".", " ");
                }

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
          videoTorrent.ParsedName = parsedName.Replace("(", "");
        }

        var videoGroups = videoTorrents.GroupBy(x => StringHelper.GetClearString(x.ParsedName.ToLower()));

        var duplicates = new List<VideoTorrent>();

        foreach (var group in videoGroups)
        {
          var first = group.First();
          var groupL = group.ToList();

          first.Qualities = groupL.Skip(1).Take(groupL.Count - 1);
          duplicates.AddRange(first.Qualities);
        }

        return torrents.Where(x => !duplicates.Contains(x)).AsEnumerable();
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
  }
}