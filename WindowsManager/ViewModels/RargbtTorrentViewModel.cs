using System;
using System.Diagnostics;
using System.Windows.Input;
using TorrentAPI;
using VCore.Standard;
using VCore.WPF.Misc;

namespace WindowsManager.ViewModels
{
  public class RargbtTorrentViewModel : ViewModel<RargbtTorrent>
  {
    public RargbtTorrentViewModel(RargbtTorrent model) : base(model)
    {
      Name = model.Title;
    }

    #region TurnOffCommand

    private ActionCommand<string> openInBrowser;
    public ICommand OpenInBrowser
    {
      get
      {
        return openInBrowser ??= new ActionCommand<string>(OnOpenInBrowser);
      }
    }

    private void OnOpenInBrowser(string path)
    {
      if (!string.IsNullOrEmpty(path))
      {
        Process.Start(new System.Diagnostics.ProcessStartInfo()
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




  }

  public class VideoRargbtTorrentViewModel : RargbtTorrentViewModel
  {
    public VideoRargbtTorrentViewModel(VideoRargbtTorrent model) : base(model)
    {
      Name = model.ParsedName;
    }

    public VideoRargbtTorrent VideoRargbtTorrent
    {
      get
      {
        return (VideoRargbtTorrent) Model;
      }
    }
  }
}