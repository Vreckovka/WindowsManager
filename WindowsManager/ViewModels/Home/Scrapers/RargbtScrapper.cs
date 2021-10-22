using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ChromeDriverScrapper;
using HtmlAgilityPack;
using VCore;
using VPlayer.AudioStorage.Scrappers.CSFD;


namespace WindowsManager.ViewModels.Home.Scrapers
{
  public class RargbtScrapper
  {
    private readonly IChromeDriverProvider chromeDriverProvider;

    public RargbtScrapper(IChromeDriverProvider chromeDriverProvider)
    {
      this.chromeDriverProvider = chromeDriverProvider ?? throw new ArgumentNullException(nameof(chromeDriverProvider));
    }

    private string baseFolder = "Data\\Rargbt";

    public DateTime? DateOfData { get; set; }

    #region GetTorrentNodes

    private HtmlNodeCollection GetTorrentNodes(HtmlDocument document)
    {
      //html[1]/body[1]/table[3]/tr[1]/td[2]/table[1]/tr[3]/table[1]/tr
      return document.DocumentNode.SelectNodes("/html/body/table[3]/tbody/tr/td[2]/div/table/tbody/tr[2]/td/table[2]/tbody/tr");
    }

    #endregion

    #region ScrapePage

    public Task<IEnumerable<RargbtTorrent>> ScrapePage(int pageNumber)
    {
      return Task.Run(async () =>
      {
        using (WebClient client = new WebClient())
        {
          var list = new List<RargbtTorrent>();

          client.Headers.Add("User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:92.0) Gecko/20100101 Firefox/92.0");

          var htmlCode = await LoadTorrents(pageNumber);

          if (htmlCode != null)
          {
            var document = new HtmlDocument();

            document.LoadHtml(htmlCode);

            var torrentsNodes = GetTorrentNodes(document);

            int index = 0;

            if (torrentsNodes != null)
            {
              foreach (var node in torrentsNodes)
              {
                if (index == 0)
                {
                  index++;
                  continue;
                }


                int? category = null;
                string categoryImagePath = null;
                string name = null;
                string href = null;
                string imagePath = null;
                double? size = null;
                string sizeUnit = null;
                int? seeders = null;
                int? leechers = null;
                DateTime? created = null;

                //Movie
                string parsedName = null;
                string quality = null;

                //CATEGORY
                var categoryNode = node.SelectNodes("td[1]/a[1]")?.SingleOrDefault();

                if (categoryNode != null)
                {
                  var hrefValue = categoryNode.Attributes?.SingleOrDefault(x => x.Name == "href")?.Value;

                  if (hrefValue != null)
                  {
                    var categoryRegex = new Regex(@"category=(\d.+)");

                    var match = categoryRegex.Match(hrefValue);

                    if (match.Success)
                    {
                      category = int.Parse(match.Groups[1].Value);
                    }
                  }

                  var categoryImageNode = categoryNode.ChildNodes?.FirstOrDefault();

                  if (categoryImageNode != null)
                  {
                    var srcValue = categoryImageNode.Attributes?.SingleOrDefault(x => x.Name == "src")?.Value;

                    if (srcValue != null)
                    {
                      categoryImagePath = srcValue;
                    }
                  }
                }

                //Name
                var nameNode = node.SelectNodes("td[2]/a[1]")?.SingleOrDefault();

                if (!string.IsNullOrEmpty(nameNode?.InnerHtml))
                {
                  href = nameNode.Attributes.SingleOrDefault(x => x.Name == "href")?.Value;
                  imagePath = nameNode.Attributes.SingleOrDefault(x => x.Name == "onmouseover")?.Value;
                  name = nameNode.InnerHtml;

                  if (imagePath != null)
                  {
                    var categoryRegex = new Regex(@"https:\/\/.+.jpg");

                    var match = categoryRegex.Match(imagePath);

                    if (match.Success)
                    {
                      imagePath = match.Value;
                    }
                  }

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
                    }
                  }
                }

                //Added
                var addedNode = node.SelectNodes("td[3]")?.SingleOrDefault();

                if (!string.IsNullOrEmpty(addedNode?.InnerHtml))
                {
                  if (DateTime.TryParse(addedNode.InnerHtml, out var createdParsed))
                  {
                    created = createdParsed;
                  }
                }

                //Size
                var sizeNode = node.SelectNodes("td[4]")?.SingleOrDefault();

                if (!string.IsNullOrEmpty(addedNode?.InnerHtml))
                {
                  var split = sizeNode.InnerHtml.Split(" ");

                  if (split.Length >= 2 && double.TryParse(
                    split[0],
                    NumberStyles.Any,
                    new CultureInfo("en-US"),
                    out var sizeParsed))
                  {
                    size = sizeParsed;
                    sizeUnit = split[1];
                  }
                }

                //Seeders
                var seedersNode = node.SelectNodes("td[5]/font")?.SingleOrDefault();

                if (!string.IsNullOrEmpty(seedersNode?.InnerHtml))
                {
                  if (int.TryParse(seedersNode.InnerHtml, out var parsedSeeders))
                  {
                    seeders = parsedSeeders;
                  }
                }

                //Leechers
                var leechersNode = node.SelectNodes("td[6]")?.SingleOrDefault();

                if (!string.IsNullOrEmpty(leechersNode?.InnerHtml))
                {
                  if (int.TryParse(leechersNode.InnerHtml, out var parsedLeechers))
                  {
                    leechers = parsedLeechers;
                  }
                }

                RargbtTorrent newTorrent = null;

                if (parsedName != null)
                {
                  newTorrent = new VideoRargbtTorrent()
                  {
                    Category = category,
                    CategoryImagePath = categoryImagePath,
                    Created = created,
                    Href = href,
                    ImagePath = imagePath,
                    Leechers = leechers,
                    Name = name,
                    Seeders = seeders,
                    SeedersOrderIndex = index,
                    Size = size,
                    SizeUnit = GetSizeUnit(sizeUnit),
                    ParsedName = parsedName,
                    Quality = quality
                  };
                }
                else
                {
                  newTorrent = new RargbtTorrent()
                  {
                    Category = category,
                    CategoryImagePath = categoryImagePath,
                    Created = created,
                    Href = href,
                    ImagePath = imagePath,
                    Leechers = leechers,
                    Name = name,
                    Seeders = seeders,
                    SeedersOrderIndex = index,
                    Size = size,
                    SizeUnit = GetSizeUnit(sizeUnit)
                  };
                }

                if (newTorrent is VideoRargbtTorrent newVideoTorrent)
                {
                  var parent = list.OfType<VideoRargbtTorrent>().FirstOrDefault(x => x.ParsedName == parsedName);

                  if (parent != null)
                  {
                    if (parent.Qualities == null)
                    {
                      parent.Qualities = new List<VideoRargbtTorrent>();
                    }

                    var qualities = parent.Qualities.ToList();
                    qualities.Add(newVideoTorrent);

                    parent.Qualities = qualities.OrderBy(x => x.SizeUnit).ThenBy(x => x.Size).ToList();
                  }
                  else
                  {
                    list.Add(newTorrent);
                  }
                }
                else
                {
                  list.Add(newTorrent);
                }


                index++;
              }
            }
          }


          return list.AsEnumerable();
        }
      });
    }

    #endregion

    #region LoadTorrents

    private async Task<string> LoadTorrents(int pageNumber)
    {
      var pathToFile = GetSavedFilePath(DateTime.Now);

      if (File.Exists(pathToFile))
      {
        DateOfData = DateTime.Now;

        Console.WriteLine("Loading torrents from file " + pathToFile);
        return File.ReadAllText(pathToFile);
      }
      else
      {
#if RELEASE
        if (!chromeDriverProvider.Initialize())
        {
          return null;
        }

        chromeDriverProvider.ChromeDriver.Url = "https://rarbg2019.org/torrents.php?order=seeders&by=DESC&page={pageNumber}";
        chromeDriverProvider.ChromeDriver.Navigate();

        await Task.Delay(2500);

        var html = chromeDriverProvider.ChromeDriver.PageSource;



#else
        var html = File.ReadAllText("rartb.txt");
#endif

        var document = new HtmlDocument();

        document.LoadHtml(html);

        var torrentsNodes = GetTorrentNodes(document);

        if (torrentsNodes == null)
        {
          for (int i = 1; i < 6; i++)
          {
            pathToFile = GetSavedFilePath(DateTime.Now.AddDays(-i));

            Console.WriteLine("Loading torrents from file " + pathToFile);

            if (File.Exists(pathToFile))
            {
              DateOfData = DateTime.Now.AddDays(-i);

              return File.ReadAllText(pathToFile);
            }
          }

          return null;
        }
        else
        {
          DateOfData = DateTime.Now;

          SaveHtml(html);


          return html;
        }
      }
    }

    #endregion

    #region SaveHtml

    private void SaveHtml(string html)
    {
      var fileName = GetSavedFilePath(DateTime.Now);

      fileName.EnsureDirectoryExists();

      File.WriteAllText(fileName, html);
    }

    #endregion

    #region SaveTorrents

    private void SaveTorrents(IEnumerable<RargbtTorrent> rargbtTorrents)
    {
      var json = JsonSerializer.Serialize(rargbtTorrents);

      var fileName = GetSavedFilePath(DateTime.Now);

      fileName.EnsureDirectoryExists();

      File.WriteAllText(fileName, json);
    }

    #endregion

    #region GetSavedFilePath

    private string GetSavedFilePath(DateTime date)
    {
      var newName = $"rargbt_torrents_{date.ToString("dd.MM.yyyy")}.txt";
      var path = Path.Combine(baseFolder, newName);
      return path;
    }

    #endregion

    private SizeUnit? GetSizeUnit(string unit)
    {
      switch (unit)
      {
        case "B":
          {
            return SizeUnit.B;
          }
        case "KB":
          {
            return SizeUnit.KB;
          }
        case "MB":
          {
            return SizeUnit.MB;
          }
        case "GB":
          {
            return SizeUnit.GB;
          }
        case "TB":
          {
            return SizeUnit.TB;
          }
      }

      return null;
    }

  }
}
