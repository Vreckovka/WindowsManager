using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using WindowsManager.ViewModels.TurnOff;
using WindowsManager.Windows;
using SoundManagement;
using VCore;
using VCore.Standard.Helpers;
using VCore.ViewModels;

namespace WindowsManager.ViewModels
{
  public class WindowManagerMainWindowViewModel : BaseMainWindowViewModel
  {
    public WindowManagerMainWindowViewModel(
      ScreensManagementViewModel screensManagementViewModel,
      TurnOffViewModel turnOffViewModel)
    {
      ScreensManagementViewModel = screensManagementViewModel;
      TurnOffViewModel = turnOffViewModel;
    }

    #region Properties

    public override string Title => "Window manager";
    public ScreensManagementViewModel ScreensManagementViewModel { get; set; }
    public TurnOffViewModel TurnOffViewModel { get; set; }

    #endregion

    public ICommand SwitchBehaviorCommand { get; set; }

    protected override void OnClose(Window window)
    {
      window.WindowState = WindowState.Minimized;
    }

    #region SwitchScreenCommand

    private ActionCommand switchScreenCommand;

    public ICommand SwitchScreenCommand
    {
      get
      {
        return switchScreenCommand ??= new ActionCommand(SwitchScreen);
      }
    }


    private void SwitchScreen()
    {
      SwitchBehaviorCommand?.Execute(null);

    
    }

    #endregion

  }
}
