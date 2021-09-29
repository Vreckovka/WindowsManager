using System;

namespace WindowsManager.ViewModels.Home.Scrapers
{
  public class RargbtTorrent
  {
    public int? Category { get; set; }
    public string CategoryImagePath { get; set; }
    public string Name { get; set; }
    public string Href { get; set; }

    #region Link

    public string Link
    {
      get
      {
        if (!string.IsNullOrEmpty(Href))
        {
          return "https://rarbg2019.org" + Href;
        }

        return null;
      }
    }

    #endregion

    public string ImagePath { get; set; }
    public double? Size { get; set; }
    public string SizeUnit { get; set; }
    public int? Seeders { get; set; }
    public int? Leechers { get; set; }
    public int? SeedersOrderIndex { get; set; }
    public DateTime? Created { get; set; }
  }
}