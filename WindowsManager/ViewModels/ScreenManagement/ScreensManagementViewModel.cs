using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Win32;
using VCore;
using VCore.ItemsCollections;
using VCore.Standard;
using VCore.Standard.Helpers;

namespace WindowsManager.ViewModels
{
  public class ScreensManagementViewModel : ViewModel
  {
   

    public ScreensManagementViewModel()
    {
     
    }

    #region Properties

    #region Screens

    private RxObservableCollection<ScreenViewModel> screens = new RxObservableCollection<ScreenViewModel>();

    public RxObservableCollection<ScreenViewModel> Screens
    {
      get { return screens; }
      set
      {
        if (value != screens)
        {
          screens = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsTurnOffScreensButActive

    private bool isTurnOffScreensButActivear;

    public bool IsTurnOffScreensButActive
    {
      get { return isTurnOffScreensButActivear; }
      set
      {
        if (value != isTurnOffScreensButActivear)
        {
          isTurnOffScreensButActivear = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public enum MonitorState
    {
      MonitorStateOn = -1,
      MonitorStateOff = 2,
      MonitorStateStandBy = 1
    }


    #endregion

    #region TurnOffCommand

    private ActionCommand turnOffButActive;
    public ICommand TurnOffButActive
    {
      get
      {
        return turnOffButActive ??= new ActionCommand(TurnOffScreensButActive);
      }
    }



    #endregion

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      var screensArry = System.Windows.Forms.Screen.AllScreens;

      var scresnsVm = screensArry.Select(x => new ScreenViewModel(x));

      Screens.AddRange(scresnsVm);

      Screens.ItemUpdated.Where(x => x.EventArgs.PropertyName == nameof(ScreenViewModel.IsDimmed)).Subscribe(x => OnDimmedChanged()).DisposeWith(this);


      foreach (var screen in Screens)
      {
        screen.Initialize();
      }

      UpdateActualScreen();

      Observable.Interval(TimeSpan.FromSeconds(0.2)).Subscribe(x => UpdateActualScreen()).DisposeWith(this);

      Application.Current.MainWindow.Closing += MainWindow_Closing;

     
    }

    #region MainWindow_Closing

    private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      foreach (var screen in Screens.Where(x => x.IsDimmed))
      {
        screen.DimmOrUnDimm();
      }
    }

    #endregion

    #endregion

    #region TurnOffScreensButActive

    private void TurnOffScreensButActive()
    {
      UpdateActualScreen();

      IEnumerable<ScreenViewModel> screens = null;

      if (IsTurnOffScreensButActive)
      {
        screens = Screens.Where(x => x.IsDimmed && x != actualScreen);
      }
      else
      {
        screens = Screens.Where(x => !x.IsDimmed && x != actualScreen);
      }

      foreach (var screen in screens)
      {
        screen.DimmOrUnDimm();
      }
    }

    #endregion

    #region OnDimmedChanged

    private void OnDimmedChanged()
    {
      var isAllBlack = Screens.Where(x => x != actualScreen).All(x => x.IsDimmed);

      Application.Current.Dispatcher.Invoke(() =>
      {
        if (isAllBlack)
        {
          IsTurnOffScreensButActive = true;
        }
        else
        {
          IsTurnOffScreensButActive = false;
        }
      });
    }

    #endregion

    #region UpdateActualScreen

    ScreenViewModel actualScreen;
    private void UpdateActualScreen()
    {
      var newAcutalScreen = GetCurrentScreen();

      if (newAcutalScreen != actualScreen)
      {
        if (actualScreen != null)
          actualScreen.IsActive = false;

        actualScreen = newAcutalScreen;

        if (actualScreen != null)
          actualScreen.IsActive = true;
      }
    }

    #endregion

    #region GetCurrentScreen

    private ScreenViewModel GetCurrentScreen()
    {
      var mousePostion = GetMousePosition();

      foreach (var screen in Screens)
      {
        double topLeftX = screen.Model.Bounds.X;
        double topLeftY = screen.Model.Bounds.Y;
        double bottomRightX = screen.Model.Bounds.X + screen.Model.Bounds.Width;
        double bottomRightY = screen.Model.Bounds.Y + screen.Model.Bounds.Height;

        if (topLeftX <= mousePostion.X && mousePostion.X <= bottomRightX && topLeftY <= mousePostion.Y && mousePostion.Y <= bottomRightY)
        {
          return screen;
        }

      }

      return null;
    }

    #endregion

    #region GetMousePosition

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetCursorPos(ref Win32Point pt);

    [StructLayout(LayoutKind.Sequential)]
    internal struct Win32Point
    {
      public Int32 X;
      public Int32 Y;
    };
    public static Point GetMousePosition()
    {
      var w32Mouse = new Win32Point();
      GetCursorPos(ref w32Mouse);

      return new Point(w32Mouse.X, w32Mouse.Y);
    }

    #endregion

   

    #endregion
  }


  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
  public struct PHYSICAL_MONITOR
  {
    public IntPtr hPhysicalMonitor;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string szPhysicalMonitorDescription;
  }
}
