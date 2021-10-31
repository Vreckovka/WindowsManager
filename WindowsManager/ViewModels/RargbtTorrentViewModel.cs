using System;
using System.Diagnostics;
using System.Net;
using System.Windows.Input;
using ChromeDriverScrapper;
using TorrentAPI;
using VCore.Standard;
using VCore.WPF.Misc;

namespace WindowsManager.ViewModels
{
  public class RargbtTorrentViewModel : ViewModel<RargbtTorrent>
  {
    private readonly IRarbgApiClient rarbgApiClient;


    public RargbtTorrentViewModel(RargbtTorrent model, IRarbgApiClient rarbgApiClient) : base(model)
    {
      this.rarbgApiClient = rarbgApiClient ?? throw new ArgumentNullException(nameof(rarbgApiClient));

      Name = model.Title;
    }

    #region OpenInfoPage

    private ActionCommand openInfoPage;
    public ICommand OpenInfoPage
    {
      get
      {
        return openInfoPage ??= new ActionCommand(async () =>
        {
          if (!string.IsNullOrEmpty(Model.InfoPageParameter))
          {
            var path = await rarbgApiClient.GetInfoPageLink(Model.InfoPageParameter);

            OnOpenInBrowser(path);
          }
        });
      }
    }

    #endregion

    #region Download

    private ActionCommand download;

    public ICommand Download
    {
      get
      {
        return download ??= new ActionCommand(() => OnOpenInBrowser(Model.Download));
      }
    }

    #endregion

    #region OnOpenInBrowser

    private void OnOpenInBrowser(string path)
    {
      if (!string.IsNullOrEmpty(path))
      {
        Process.Start(new ProcessStartInfo()
        {
          FileName = path,
          UseShellExecute = true,
          Verb = "open"
        });
      }
    }

    #endregion


    #region ItemExtraData

    private object itemExtraData;

    public object ItemExtraData
    {
      get { return itemExtraData; }
      set
      {
        if (value != itemExtraData)
        {
          itemExtraData = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Name

    private string name;

    public string Name
    {
      get { return name; }
      set
      {
        if (value != name)
        {
          name = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region SeedersOrderIndex

    private int seedersOrderIndex;

    public int SeedersOrderIndex
    {
      get { return seedersOrderIndex; }
      set
      {
        if (value != seedersOrderIndex)
        {
          seedersOrderIndex = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ImageUrl

    private string imageUrl;

    public string ImageUrl
    {
      get { return imageUrl; }
      set
      {
        if (value != imageUrl)
        {
          imageUrl = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Created

    private DateTime? created;

    public DateTime? Created
    {
      get { return created; }
      set
      {
        if (value != created)
        {
          created = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public string InfoPageFakeUrl
    {
      get
      {
        return "https://torrentapi.org/redirect_to_info.php?" + "p=" + Model?.InfoPageParameter;
      }
    }


  }

  public class VideoRargbtTorrentViewModel : RargbtTorrentViewModel
  {
    public VideoRargbtTorrentViewModel(VideoRargbtTorrent model, IRarbgApiClient rarbgApiClient) : base(model, rarbgApiClient)
    {
      Name = model.ParsedName;
    }

    public VideoRargbtTorrent VideoRargbtTorrent
    {
      get
      {
        return (VideoRargbtTorrent)Model;
      }
    }
  }
}