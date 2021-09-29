using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;


namespace WindowsManager.ViewModels.Home.Scrapers
{
  public class RargbtScrapper
  {
    public Task<IEnumerable<RargbtTorrent>> ScrapePage(int pIndex)
    {
      return Task.Run(() =>
      {
        using (WebClient client = new WebClient())
        {
          var list = new List<RargbtTorrent>();
          client.Headers.Add("User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:92.0) Gecko/20100101 Firefox/92.0");

#if RELEASE
          string htmlCode = client.DownloadString($"https://rarbg2019.org/torrents.php?order=seeders&by=DESC&page={pIndex}");
#else
          string htmlCode = File.ReadAllText("rartb.txt");
#endif

          var document = new HtmlDocument();

          document.LoadHtml(htmlCode);

          var torrentsNode = document.DocumentNode.SelectNodes("html[1]/body[1]/table[3]/tr[1]/td[2]/table[1]/tr[3]/table[1]/tr");

          int index = 0;
          foreach (var node in torrentsNode)
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

              if(!string.IsNullOrEmpty(name))
              {
                var nameRegex = new Regex(@"(.+)(\d...p)");
                var match = nameRegex.Match(name);

                if (match.Success)
                {
                  parsedName = match.Groups[1].Value?.Replace("."," ");
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
                SizeUnit = sizeUnit,
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
                SizeUnit = sizeUnit
              };
            }

            if (newTorrent is VideoRargbtTorrent newVideoTorrent)
            {
              var parent = list.OfType<VideoRargbtTorrent>().FirstOrDefault(x => x.ParsedName == parsedName);

              if (parent != null)
              {
                if(parent.Qualities == null)
                {
                  parent.Qualities = new List<VideoRargbtTorrent>();
                }

                var qualities = parent.Qualities.ToList();
                qualities.Add(newVideoTorrent);

                parent.Qualities = qualities;
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

          return list.AsEnumerable();

        }
      });
    }
  }
}
