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
using System.Windows.Threading;
using WindowsManager.Modularity;
using WindowsManager.ViewModels.ScreenManagement.Rules;
using WindowsManager.ViewModels.TurnOff;
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
    public double KwhPriceEur { get; set; }
  }

  public static class CommonSettings
  {
    public static double KwhPriceEur { get; set; }
  }


  public class ScreensManagementViewModel : RegionViewModel<ScreensManagementView>
  {
    private readonly RuleManagerViewModel ruleManagerViewModel;
    private readonly TurnOffViewModel turnOffViewModel;

    private string filePath;
    private DispatcherTimer screenTimer;
    private DispatcherTimer rulesTimer;
    private DispatcherTimer statisticsTimer;
    private bool disposed;
    //private string folderPath = "Data\\Monitors";
    private string folderPath = "D:\\Moje applikacie\\Builds\\WindowsManager\\Data\\Monitors";

    public ScreensManagementViewModel(
      IRegionProvider regionProvider, 
      RuleManagerViewModel ruleManagerViewModel,
      TurnOffViewModel turnOffViewModel) : base(regionProvider)
    {
      this.ruleManagerViewModel = ruleManagerViewModel ?? throw new ArgumentNullException(nameof(ruleManagerViewModel));
      this.turnOffViewModel = turnOffViewModel ?? throw new ArgumentNullException(nameof(turnOffViewModel));
      filePath = folderPath + "\\monitors_data.txt";
      

      ruleManagerViewModel.Rules.ItemUpdated
        .Where(x => x.EventArgs.PropertyName == nameof(RuleViewModel.IsRuleEnabled))
        .Where(x => x.Sender is RuleViewModel rule && !rule.IsRuleEnabled)
        .Subscribe(x =>
        {
          ((RuleViewModel)x.Sender)?.Model.Revert(Screens.ToArray());
          ruleManagerViewModel.SaveRules();
        }).DisposeWith(this);
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
      if (WasInitilized || disposed) return;
      base.Initialize();

      var screensArry = System.Windows.Forms.Screen.AllScreens.ToList();

      var scresnsVm = screensArry.Select(x => 
        new ScreenViewModel(new ScreenModel()
        {
          Screen = x
        }, folderPath + "\\Monitor_" + screensArry.IndexOf(x) + ".txt", turnOffViewModel)).ToList();

      ruleManagerViewModel.Screens = scresnsVm;
      Screens.AddRange(scresnsVm);

      if (Screens.Count > 0)
        Screens[0].IsSelected = true;

      Screens.ItemUpdated.Where(x => x.EventArgs.PropertyName == nameof(ScreenViewModel.IsDimmed)).Throttle(TimeSpan.FromMilliseconds(25)).ObserveOnDispatcher()
        .Subscribe(x => OnDimmedChanged((ScreenViewModel)x.Sender)).DisposeWith(this);

      Screens.ItemUpdated.Where(x => x.EventArgs.PropertyName == nameof(ScreenViewModel.IsActive)).Throttle(TimeSpan.FromMilliseconds(25)).ObserveOnDispatcher()
        .Subscribe(x => OnActiveChanged((ScreenViewModel)x.Sender)).DisposeWith(this);

      Screens.ItemUpdated.Where(x => x.EventArgs.PropertyName == nameof(ScreenViewModel.TotalDimmTime)).Subscribe((x) => OnDimmedTimeChagend()).DisposeWith(this);
      Screens.ItemUpdated.Where(x => x.EventArgs.PropertyName == nameof(ScreenViewModel.TotalSaved)).Subscribe((x) => OnSavedChagend()).DisposeWith(this);

      Screens.ItemAdded.Subscribe(x => { ruleManagerViewModel.Screens = Screens.ToList(); }).DisposeWith(this);
      Screens.ItemRemoved.Subscribe(x => { ruleManagerViewModel.Screens = Screens.ToList(); }).DisposeWith(this);

      foreach (var screen in Screens)
      {
        screen.Initialize();
      }

      UpdateActualScreen();

      screenTimer = new DispatcherTimer(DispatcherPriority.Background)
        { Interval = TimeSpan.FromMilliseconds(200) };
      screenTimer.Tick += OnScreenTimerTick;
      screenTimer.Start();
      rulesTimer = new DispatcherTimer(DispatcherPriority.Background)
        { Interval = TimeSpan.FromSeconds(1) };
      rulesTimer.Tick += OnRulesTimerTick;
      rulesTimer.Start();
      statisticsTimer = new DispatcherTimer(DispatcherPriority.Background)
        { Interval = TimeSpan.FromMinutes(1) };
      statisticsTimer.Tick += OnStatisticsTimerTick;
      statisticsTimer.Start();

      Application.Current.MainWindow.Closing += MainWindow_Closing;


      _ = LoadAsync();
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

      UpdateRules();

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
      UpdateRules();
    }

    #endregion

    private void UpdateRules()
    {
      if (disposed) return;
      ruleManagerViewModel.Rules
        .Where(x => x.Model.Types.Contains(IRuleAction.ScreenActivated))
        .Where(x => x.IsRuleEnabled)
        .ForEach(x => x.Model.Execute(Screens.ToArray()));
    }

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
        var screenModel = screen.Model.Screen;

        double topLeftX = screenModel.Bounds.X;
        double topLeftY = screenModel.Bounds.Y;
        double bottomRightX = screenModel.Bounds.X + screenModel.Bounds.Width;
        double bottomRightY = screenModel.Bounds.Y + screenModel.Bounds.Height;

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
    private async Task LoadAsync()
    {
      try
      {
        if (File.Exists(filePath))
        {
          var data = await File.ReadAllTextAsync(filePath);
          if (disposed) return;

          LoadedData = JsonSerializer.Deserialize<ScreensManagementData>(data);

          if (LoadedData != null)
          {
            CommonSettings.KwhPriceEur = LoadedData.KwhPriceEur;
            Screens.ForEach(x => x.RaiseTotalSaved());
            wasLoaded = true;
          }
        }
        else
        {
          wasLoaded = true;

          LoadedData = new ScreensManagementData()
          {
            StartDayOfUsingSoftware = DateTime.Now
          };
        }
      }
      catch (Exception ex) when (ex is JsonException || ex is IOException || ex is UnauthorizedAccessException)
      {
      }
    }

    #endregion

    private void OnScreenTimerTick(object sender, EventArgs e) => UpdateActualScreen();
    private void OnRulesTimerTick(object sender, EventArgs e) => UpdateRules();
    private void OnStatisticsTimerTick(object sender, EventArgs e)
    {
      RaisePropertyChanged(nameof(TotalDays));
      foreach (var screen in Screens) screen.RaiseDaysOfUsingSoftware();
    }

    public override void Dispose()
    {
      if (disposed) return;
      disposed = true;
      if (screenTimer != null) { screenTimer.Stop(); screenTimer.Tick -= OnScreenTimerTick; }
      if (rulesTimer != null) { rulesTimer.Stop(); rulesTimer.Tick -= OnRulesTimerTick; }
      if (statisticsTimer != null) { statisticsTimer.Stop(); statisticsTimer.Tick -= OnStatisticsTimerTick; }
      if (Application.Current?.MainWindow != null)
        Application.Current.MainWindow.Closing -= MainWindow_Closing;
      foreach (var screen in Screens) screen.Dispose();
      base.Dispose();
      Screens.Dispose();
    }
    #endregion

  }
}
