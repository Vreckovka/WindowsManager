using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WindowsManager.Modularity;
using WindowsManager.ViewModels.ScreenManagement.Rules;
using WindowsManager.Views;
using VCore;
using VCore.ItemsCollections;
using VCore.Standard.Helpers;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels;

namespace WindowsManager.ViewModels.ScreenManagement
{
  public class ScreensManagementData
  {
    public DateTime StartDayOfUsingSoftware { get; set; }
  }

  public class ScreensManagementViewModel : RegionViewModel<ScreensManagementView>
  {

    private string filePath;
    private string folderPath = "Data\\Monitors";

    public ScreensManagementViewModel(IRegionProvider regionProvider) : base(regionProvider)
    {
      filePath = folderPath + "\\monitors_data.txt";
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

    public override string RegionName { get; protected set; } = RegionNames.MainContent;

    public override string Header => "Monitors";

    #region TotalDimmTime

    private TimeSpan totalDimmTime;

    public TimeSpan TotalDimmTime
    {
      get { return totalDimmTime; }
      set
      {
        if (value != totalDimmTime)
        {
          totalDimmTime = value;

          RaisePropertyChanged(nameof(TotalSaved));
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TotalSaved

    private double totalSaved;

    public double TotalSaved
    {
      get { return totalSaved; }
      set
      {
        if (value != totalSaved)
        {
          totalSaved = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region LoadedData

    private ScreensManagementData loadedData;

    public ScreensManagementData LoadedData
    {
      get { return loadedData; }
      set
      {
        if (value != loadedData)
        {
          loadedData = value;
          RaisePropertyChanged(nameof(TotalDays));
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public double TotalDays
    {
      get
      {
        if (LoadedData != null)
        {
          var totalDays = (DateTime.Now - LoadedData.StartDayOfUsingSoftware).TotalDays;

          return totalDays;
        }

        return 0;
      }
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

      var screensArry = System.Windows.Forms.Screen.AllScreens.ToList();

      var scresnsVm = screensArry.Select(x => new ScreenViewModel(x, folderPath + "\\Monitor_" + screensArry.IndexOf(x) + ".txt"));

      Screens.AddRange(scresnsVm);

      if (Screens.Count > 0)
        Screens[0].IsSelected = true;

      Screens.ItemUpdated.Where(x => x.EventArgs.PropertyName == nameof(ScreenViewModel.IsDimmed)).Throttle(TimeSpan.FromMilliseconds(0.25)).ObserveOnDispatcher()
        .Subscribe(x => OnDimmedChanged((ScreenViewModel)x.Sender)).DisposeWith(this);

      Screens.ItemUpdated.Where(x => x.EventArgs.PropertyName == nameof(ScreenViewModel.IsActive)).Throttle(TimeSpan.FromMilliseconds(0.25)).ObserveOnDispatcher()
        .Subscribe(x => OnActiveChanged((ScreenViewModel)x.Sender)).DisposeWith(this);

      Screens.ItemUpdated.Where(x => x.EventArgs.PropertyName == nameof(ScreenViewModel.TotalDimmTime)).Subscribe((x) => OnDimmedTimeChagend()).DisposeWith(this);
      Screens.ItemUpdated.Where(x => x.EventArgs.PropertyName == nameof(ScreenViewModel.TotalSaved)).Subscribe((x) => OnSavedChagend()).DisposeWith(this);

      foreach (var screen in Screens)
      {
        screen.Initialize();
      }

      UpdateActualScreen();

      Observable.Interval(TimeSpan.FromSeconds(0.2)).ObserveOnDispatcher().Subscribe(x => UpdateActualScreen()).DisposeWith(this);

      Application.Current.MainWindow.Closing += MainWindow_Closing;


      Task.Run(() => Load());
      Task.Run(() => CreateBackup());
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

    #region OnDimmedTimeChagend

    private void OnDimmedTimeChagend()
    {
      TotalDimmTime = new TimeSpan(Screens.Sum(r => r.TotalDimmTimeTicks));
    }

    #endregion

    #region OnSavedChagend

    private void OnSavedChagend()
    {
      TotalSaved = Screens.Sum(r => r.TotalSaved);
    }

    #endregion

    #region CreateBackup

    private void CreateBackup()
    {
      if (!Directory.Exists(folderPath))
        Directory.CreateDirectory(folderPath);

      var backupPath = "Data\\Monitors\\Backup";
      var newDir = backupPath + $"\\Data_{ DateTime.Today.ToString("dd.MM.yyyy")}";
      ;
      if (!Directory.Exists(newDir))
      {
        Directory.CreateDirectory(newDir);

        CopyFiles("Data\\Monitors", newDir);
      }
    }

    #endregion

    #region CopyFiles

    private void CopyFiles(string sourcePath, string targetPath, SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
      foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", searchOption))
      {
        File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
      }
    }

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

    private void OnDimmedChanged(ScreenViewModel screenViewModel)
    {
      var isAllBlack = Screens.Where(x => x != actualScreen).All(x => x.IsDimmed);

      IRuleAction rule = IRuleAction.ScreenDimmed;

      if(!screenViewModel.IsDimmed)
      {
        rule = IRuleAction.ScreenUnDimmed;
      }

      screenViewModel.Rules.Where(x => x.Types.Contains(rule)).ForEach(x => x.Execute());

      if (isAllBlack)
      {
        IsTurnOffScreensButActive = true;
      }
      else
      {
        IsTurnOffScreensButActive = false;
      }
    }

    #endregion

    #region OnActiveChanged

    private void OnActiveChanged(ScreenViewModel screenViewModel)
    {
      screenViewModel.Rules.Where(x => x.Types.Contains(IRuleAction.ScreenActivated)).ForEach(x => x.Execute());
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

      RaisePropertyChanged(nameof(TotalDays));
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

    #region Save

    private void Save()
    {
      var fileExists = File.Exists(filePath);

      if ((fileExists && wasLoaded) || !fileExists)
      {
        var json = JsonSerializer.Serialize(LoadedData);

        File.WriteAllText(filePath, json);
      }
    }

    #endregion

    #region Load

    private bool wasLoaded;
    private void Load()
    {
      try
      {
        if (File.Exists(filePath))
        {
          var data = File.ReadAllText(filePath);

          LoadedData = JsonSerializer.Deserialize<ScreensManagementData>(data);
        }
        else
        {
          wasLoaded = true;

          LoadedData = new ScreensManagementData()
          {
            StartDayOfUsingSoftware = DateTime.Now
          };

          Save();
        }
      }
      catch (JsonException ex)
      {
      }
    }

    #endregion

    #endregion

  }
}
