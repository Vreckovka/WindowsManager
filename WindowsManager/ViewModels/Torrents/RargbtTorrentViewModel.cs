using System;
using System.Diagnostics;
using System.Windows.Input;
using TorrentAPI;
using TorrentAPI.Domain;
using VCore.Standard;
using VCore.WPF.Misc;
using VPlayer.AudioStorage.Scrappers.CSFD.Domain;
using VPlayer.Core.ViewModels.TvShows;

namespace WindowsManager.ViewModels.Torrents
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

    private ActionCommand<string> openInfoPage;
    public ICommand OpenInfoPage
    {
      get
      {
        return openInfoPage ??= new ActionCommand<string>(async (parameter) =>
        {
          if (!string.IsNullOrEmpty(parameter))
          {
            var path = await rarbgApiClient.GetInfoPageLink(parameter);

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

    #region CsfdData

    private CSFDItemViewModel itemExtraData;

    public CSFDItemViewModel ItemExtraData
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