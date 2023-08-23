namespace TorrentAPI.Domain
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Runtime.CompilerServices;

  public class TorrentCategory
  {
    public int Id { get; set; }

    public string Name { get; set; }

    public string Url { get; set; }

    public virtual bool IsVideoCategory
    {
      get
      {
        return X1337Category.IsVideoCategory(Id);
      }
    }
  }

  public static class X1337Category
  {
    public static bool IsVideoCategory(int id)
    {
      return id == 42 || id == 54 || id == 73 || id == 75 || id == 41 || id == 1 || id == 4 || id == 76 || id == 70 || id == 71;
    }


  }
  public class RargbtCategory : TorrentCategory
  {
    public static IEnumerable<RargbtCategory> GetCategories()
    {
      RargbtCategory item = new RargbtCategory();
      item.Id = 4;
      item.Name = "XXX (18+)";
      List<RargbtCategory> list1 = new List<RargbtCategory>();
      list1.Add(item);
      RargbtCategory category2 = new RargbtCategory();
      category2.Id = 0x21;
      category2.Name = "Software/PC ISO";
      list1.Add(category2);
      List<RargbtCategory> list = list1;
      list.AddRange(GetMoviesCategories());
      list.AddRange(GetTvShowCategories());
      list.AddRange(GetMusicCategories());
      list.AddRange(GetGamesCategories());
      foreach (RargbtCategory category in list)
      {
        category.Url = $"https://rargb.to/static/images/categories/cat_new{(int)category.Id}.gif";
      }
      return (IEnumerable<RargbtCategory>)list;
    }

    public static IEnumerable<RargbtCategory> GetGamesCategories()
    {
      RargbtCategory item = new RargbtCategory();
      item.Id = 0x1b;
      item.Name = "Games/PC ISO";
      List<RargbtCategory> list1 = new List<RargbtCategory>();
      list1.Add(item);
      RargbtCategory category2 = new RargbtCategory();
      category2.Id = 0x1c;
      category2.Name = "Games/PC RIP";
      list1.Add(category2);
      RargbtCategory category3 = new RargbtCategory();
      category3.Id = 40;
      category3.Name = "Games/PS3";
      list1.Add(category3);
      RargbtCategory category4 = new RargbtCategory();
      category4.Id = 0x20;
      category4.Name = "Games/XBOX-360";
      list1.Add(category4);
      RargbtCategory category5 = new RargbtCategory();
      category5.Id = 0x35;
      category5.Name = "Games/PS4";
      list1.Add(category5);
      return (IEnumerable<RargbtCategory>)list1;
    }

    public static IEnumerable<RargbtCategory> GetMoviesCategories()
    {
      RargbtCategory item = new RargbtCategory();
      item.Id = 14;
      item.Name = "Movies/XVID";
      List<RargbtCategory> list1 = new List<RargbtCategory>();
      list1.Add(item);
      RargbtCategory category2 = new RargbtCategory();
      category2.Id = 0x30;
      category2.Name = "Movies/XVID/720";
      list1.Add(category2);
      RargbtCategory category3 = new RargbtCategory();
      category3.Id = 0x11;
      category3.Name = "Movies/x264";
      list1.Add(category3);
      RargbtCategory category4 = new RargbtCategory();
      category4.Id = 0x2c;
      category4.Name = "Movies/x264/1080";
      list1.Add(category4);
      RargbtCategory category5 = new RargbtCategory();
      category5.Id = 0x2d;
      category5.Name = "Movies/x264/720";
      list1.Add(category5);
      RargbtCategory category6 = new RargbtCategory();
      category6.Id = 0x2f;
      category6.Name = "Movies/x264/3D";
      list1.Add(category6);
      RargbtCategory category7 = new RargbtCategory();
      category7.Id = 50;
      category7.Name = "Movies/x264/4k";
      list1.Add(category7);
      RargbtCategory category8 = new RargbtCategory();
      category8.Id = 0x33;
      category8.Name = "Movies/x265/4k";
      list1.Add(category8);
      RargbtCategory category9 = new RargbtCategory();
      category9.Id = 0x34;
      category9.Name = "Movs/x265/4k/HDR";
      list1.Add(category9);
      RargbtCategory category10 = new RargbtCategory();
      category10.Id = 0x2a;
      category10.Name = "Movies/Full BD";
      list1.Add(category10);
      RargbtCategory category11 = new RargbtCategory();
      category11.Id = 0x2e;
      category11.Name = "Movies/BD Remux";
      list1.Add(category11);
      RargbtCategory category12 = new RargbtCategory();
      category12.Id = 0x36;
      category12.Name = "Movies/x265/1080";
      list1.Add(category12);
      return (IEnumerable<RargbtCategory>)list1;
    }

    public static IEnumerable<RargbtCategory> GetMusicCategories()
    {
      RargbtCategory item = new RargbtCategory();
      item.Id = 0x17;
      item.Name = "Music/MP3";
      List<RargbtCategory> list1 = new List<RargbtCategory>();
      list1.Add(item);
      RargbtCategory category2 = new RargbtCategory();
      category2.Id = 0x19;
      category2.Name = "Music/FLAC";
      list1.Add(category2);
      return (IEnumerable<RargbtCategory>)list1;
    }

    public static IEnumerable<RargbtCategory> GetTvShowCategories()
    {
      RargbtCategory item = new RargbtCategory();
      item.Id = 0x12;
      item.Name = "TV Episodes";
      List<RargbtCategory> list1 = new List<RargbtCategory>();
      list1.Add(item);
      RargbtCategory category2 = new RargbtCategory();
      category2.Id = 0x29;
      category2.Name = "TV HD Episodes";
      list1.Add(category2);
      RargbtCategory category3 = new RargbtCategory();
      category3.Id = 0x31;
      category3.Name = "TV UHD Episodes";
      list1.Add(category3);
      return (IEnumerable<RargbtCategory>)list1;
    }


    public override bool IsVideoCategory =>
      Enumerable.SingleOrDefault<RargbtCategory>(Enumerable.Concat<RargbtCategory>(GetMoviesCategories(), GetTvShowCategories()), delegate (RargbtCategory x)
      {
        return x.Id == this.Id;
      }) != null;
  }
}
