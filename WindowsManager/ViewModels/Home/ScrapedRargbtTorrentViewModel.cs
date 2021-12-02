using System.Diagnostics;
using System.Windows.Input;
using WindowsManager.ViewModels.Home.Scrapers;
using VCore;
using VCore.Standard;
using VCore.WPF.Misc;

namespace WindowsManager.ViewModels.Home
{
  public class ScrapedRargbtTorrentViewModel : ViewModel<ScrapedRargbtTorrent>
  {
    public ScrapedRargbtTorrentViewModel(ScrapedRargbtTorrent model) : base(model)
    {
    }

    #region OpenInBrowser

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

  }
}