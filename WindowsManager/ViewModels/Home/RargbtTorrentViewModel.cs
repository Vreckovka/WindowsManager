using System.Diagnostics;
using System.Windows.Input;
using WindowsManager.ViewModels.Home.Scrapers;
using VCore;
using VCore.Standard;

namespace WindowsManager.ViewModels.Home
{
  public class RargbtTorrentViewModel : ViewModel<RargbtTorrent>
  {
    public RargbtTorrentViewModel(RargbtTorrent model) : base(model)
    {
    }

    #region TurnOffCommand

    private ActionCommand openInBrowser;
    public ICommand OpenInBrowser
    {
      get
      {
        return openInBrowser ??= new ActionCommand(OnOpenInBrowser);
      }
    }

    private void OnOpenInBrowser()
    {
      if (!string.IsNullOrEmpty(Model.Link))
      {
        Process.Start(new System.Diagnostics.ProcessStartInfo()
        {
          FileName = Model.Link,
          UseShellExecute = true,
          Verb = "open"
        });
      }
    }

    #endregion
  }
}