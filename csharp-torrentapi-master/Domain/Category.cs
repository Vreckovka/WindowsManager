namespace TorrentAPI.Domain
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Runtime.CompilerServices;

  public class Category
  {
    public static IEnumerable<Category> GetCategories()
    {
      Category item = new Category();
      item.Id = 4;
      item.Name = "XXX (18+)";
      List<Category> list1 = new List<Category>();
      list1.Add(item);
      Category category2 = new Category();
      category2.Id = 0x21;
      category2.Name = "Software/PC ISO";
      list1.Add(category2);
      List<Category> list = list1;
      list.AddRange(GetMoviesCategories());
      list.AddRange(GetTvShowCategories());
      list.AddRange(GetMusicCategories());
      list.AddRange(GetGamesCategories());
      foreach (Category category in list)
      {
        category.Url = $"https://dyncdn.me/static/20/images/categories/cat_new{(int)category.Id}.gif";
      }
      return (IEnumerable<Category>)list;
    }

    public static IEnumerable<Category> GetGamesCategories()
    {
      Category item = new Category();
      item.Id = 0x1b;
      item.Name = "Games/PC ISO";
      List<Category> list1 = new List<Category>();
      list1.Add(item);
      Category category2 = new Category();
      category2.Id = 0x1c;
      category2.Name = "Games/PC RIP";
      list1.Add(category2);
      Category category3 = new Category();
      category3.Id = 40;
      category3.Name = "Games/PS3";
      list1.Add(category3);
      Category category4 = new Category();
      category4.Id = 0x20;
      category4.Name = "Games/XBOX-360";
      list1.Add(category4);
      Category category5 = new Category();
      category5.Id = 0x35;
      category5.Name = "Games/PS4";
      list1.Add(category5);
      return (IEnumerable<Category>)list1;
    }

    public static IEnumerable<Category> GetMoviesCategories()
    {
      Category item = new Category();
      item.Id = 14;
      item.Name = "Movies/XVID";
      List<Category> list1 = new List<Category>();
      list1.Add(item);
      Category category2 = new Category();
      category2.Id = 0x30;
      category2.Name = "Movies/XVID/720";
      list1.Add(category2);
      Category category3 = new Category();
      category3.Id = 0x11;
      category3.Name = "Movies/x264";
      list1.Add(category3);
      Category category4 = new Category();
      category4.Id = 0x2c;
      category4.Name = "Movies/x264/1080";
      list1.Add(category4);
      Category category5 = new Category();
      category5.Id = 0x2d;
      category5.Name = "Movies/x264/720";
      list1.Add(category5);
      Category category6 = new Category();
      category6.Id = 0x2f;
      category6.Name = "Movies/x264/3D";
      list1.Add(category6);
      Category category7 = new Category();
      category7.Id = 50;
      category7.Name = "Movies/x264/4k";
      list1.Add(category7);
      Category category8 = new Category();
      category8.Id = 0x33;
      category8.Name = "Movies/x265/4k";
      list1.Add(category8);
      Category category9 = new Category();
      category9.Id = 0x34;
      category9.Name = "Movs/x265/4k/HDR";
      list1.Add(category9);
      Category category10 = new Category();
      category10.Id = 0x2a;
      category10.Name = "Movies/Full BD";
      list1.Add(category10);
      Category category11 = new Category();
      category11.Id = 0x2e;
      category11.Name = "Movies/BD Remux";
      list1.Add(category11);
      Category category12 = new Category();
      category12.Id = 0x36;
      category12.Name = "Movies/x265/1080";
      list1.Add(category12);
      return (IEnumerable<Category>)list1;
    }

    public static IEnumerable<Category> GetMusicCategories()
    {
      Category item = new Category();
      item.Id = 0x17;
      item.Name = "Music/MP3";
      List<Category> list1 = new List<Category>();
      list1.Add(item);
      Category category2 = new Category();
      category2.Id = 0x19;
      category2.Name = "Music/FLAC";
      list1.Add(category2);
      return (IEnumerable<Category>)list1;
    }

    public static IEnumerable<Category> GetTvShowCategories()
    {
      Category item = new Category();
      item.Id = 0x12;
      item.Name = "TV Episodes";
      List<Category> list1 = new List<Category>();
      list1.Add(item);
      Category category2 = new Category();
      category2.Id = 0x29;
      category2.Name = "TV HD Episodes";
      list1.Add(category2);
      Category category3 = new Category();
      category3.Id = 0x31;
      category3.Name = "TV UHD Episodes";
      list1.Add(category3);
      return (IEnumerable<Category>)list1;
    }

    public int Id { get; set; }

    public string Name { get; set; }

    public string Url { get; set; }

    public bool IsVideoCategory =>
        Enumerable.SingleOrDefault<Category>(Enumerable.Concat<Category>(GetMoviesCategories(), GetTvShowCategories()), delegate (Category x) {
          return x.Id == this.Id;
        }) != null;
  }
}
