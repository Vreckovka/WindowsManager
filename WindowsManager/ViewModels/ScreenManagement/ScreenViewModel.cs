using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using WindowsManager.ViewModels.ScreenManagement.Rules;
using WindowsManager.ViewModels.TurnOff;
using WindowsManager.Windows;
using VCore;
using VCore.Standard;
using VCore.Standard.Helpers;
using VCore.Standard.ViewModels;
using VCore.WPF;
using VCore.WPF.Misc;

namespace WindowsManager.ViewModels.ScreenManagement
{
  public class ScreenModel
  {
    public long TotalDimmTimeTicks { get; set; }

    public string Name
    {
      get { return Screen?.DeviceName; }
    }

    public bool ShouldByValue { get; set; }

    public double? TurnOffLimit { get; set; }

    public double PowerOutput { get; set; }

    public DateTime StartDayOfCounting { get; set; }

    [JsonIgnore]
    public Screen Screen { get; set; }
    public double DimmerOpacity { get; set; } = 1;

    public bool IsFastModeOn { get; set; }

  }

  public enum FastMode
  {
    Off,
    On,
    Pernament,
    Immediate
  }

  public class ScreenViewModel : ListViewItemViewModel<ScreenModel>
  {
    private BrightnessController brightnessController;
    private ReplaySubject<int> brightnessSubject = new ReplaySubject<int>(1);

    private string monitorDataFilePath;

    public ActionTimer automaticTurnOffTimer;
    private ActionTimer dimmerTimer;
    private bool disposed;
    private TimeSpan lastDimmElapsed;


    public ScreenViewModel(ScreenModel model, string monitorDataFileName, TurnOffViewModel turnOffViewModel) : base(model)
    {
      brightnessController = new BrightnessController().DisposeWith(this);

      automaticTurnOffTimer = new ActionTimer(TimeSpan.FromSeconds(0.1)).DisposeWith(this);
      dimmerTimer = new ActionTimer(TimeSpan.FromSeconds(1)).DisposeWith(this);

      monitorDataFilePath = monitorDataFileName;
      TurnOffViewModel = turnOffViewModel;

    }

    #region Properties

    public TurnOffViewModel TurnOffViewModel { get; set; }

    #region TotalDimmTime

    private TimeSpan totalDimmTime;
    private double lastSavedTime;

    public TimeSpan TotalDimmTime
    {
      get { return totalDimmTime; }
      set
      {
        if (value != totalDimmTime)
        {
          totalDimmTime = value;
          TotalDimmTimeTicks = totalDimmTime.Ticks;

          if (lastSavedTime == 0)
          {
            lastSavedTime = totalDimmTime.TotalSeconds;
          }

          if (totalDimmTime.TotalSeconds - lastSavedTime >= 30)
          {
            Save();
            lastSavedTime = totalDimmTime.TotalSeconds;
          }

          RaisePropertyChanged(nameof(TotalSaved));
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TotalDimmTimeTicks

    public long TotalDimmTimeTicks
    {
      get { return Model.TotalDimmTimeTicks; }
      set
      {
        if (value != Model.TotalDimmTimeTicks)
        {
          Model.TotalDimmTimeTicks = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsDimmed

    private bool isDimmed;

    private SerialDisposable isDimmedDisposable = new SerialDisposable();

    public bool IsDimmed
    {
      get { return isDimmed; }
      private set
      {
        if (value != isDimmed)
        {
          isDimmed = value;

          StopTurnOffTimer();

          if (!isDimmed && !IsActive)
          {
            StartTurnOffTimer();
          }


          if (IsDimmed)
          {
            StartIsDimmedTimer();
          }
          else if (!IsDimmed)
          {
            StopIsDimmedTimer();
          }

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Name

    public string Name
    {
      get { return Model?.Screen.DeviceName; }

    }

    #endregion

    #region ShouldByValue

    public bool ShouldByValue
    {
      get { return Model.ShouldByValue; }
      set
      {
        if (value != Model.ShouldByValue)
        {
          Model.ShouldByValue = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public bool Force { get; set; }

    public bool IsFastMode
    {
      get
      {
        switch (fastMode)
        {
          case FastMode.Off:
            return false;
          case FastMode.On:
            return true;
          case FastMode.Pernament:
            return true;
          case FastMode.Immediate:
            return true;
        }

        return false;
      }
    }

    #region FastMode

    private FastMode fastMode;

    public FastMode FastMode
    {
      get { return fastMode; }
      set
      {
        if (value != fastMode)
        {
          fastMode = value;

          if (IsFastMode)
          {
            StartDelayed(force: true);
          }
          else
          {
            TurnOffValue = TurnOffLimit;
            StopTurnOffTimer();
          }

          RaisePropertyChanged();
          RaisePropertyChanged(nameof(IsFastMode));

          if (fastMode == FastMode.Pernament)
          {
            Model.IsFastModeOn = true;
            Save();
          }
          else if (Model.IsFastModeOn)
          {
            Model.IsFastModeOn = false;
            Save();
          }
        }
      }
    }



    #endregion

    #region TurnOffValue

    private double? turnOffValue;

    public double? TurnOffValue
    {
      get { return turnOffValue; }
      set
      {
        if (value != turnOffValue)
        {
          turnOffValue = value;

          if (turnOffValue == 0)
          {
            StopTurnOffTimer();
            ShouldByValue = false;
          }
          else
          {
            if (!IsDimmed && !IsActive)
            {
              ShouldByValue = true;
              StartTurnOffTimer();
            }
          }

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TurnOffLimit

    public double? TurnOffLimit
    {
      get { return Model.TurnOffLimit; }
      set
      {
        if (value != Model.TurnOffLimit)
        {
          Model.TurnOffLimit = value;

          if (Model.TurnOffLimit == 0)
          {
            File.Delete(monitorDataFilePath);
          }
          else
          {
            Save();
          }

          RaisePropertyChanged();

          TurnOffValue = Model.TurnOffLimit;
        }
      }
    }

    #endregion

    #region PowerOutput

    public double PowerOutput
    {
      get { return Model.PowerOutput; }
      set
      {
        if (value != Model.PowerOutput)
        {
          Model.PowerOutput = value;
          Save();
          RaisePropertyChanged(nameof(TotalSaved));
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region StartDayOfCounting

    public DateTime StartDayOfCounting
    {
      get { return Model.StartDayOfCounting; }
      set
      {
        if (value != Model.StartDayOfCounting)
        {
          Model.StartDayOfCounting = value;

          RaisePropertyChanged();
          RaisePropertyChanged(nameof(DaysOfUsingSoftware));
        }
      }
    }

    #endregion

    #region IsActive

    private bool isActive;
    private SerialDisposable isActiveSerialDisposable = new SerialDisposable();

    public bool IsActive
    {
      get { return isActive; }
      set
      {
        if (value != isActive)
        {
          isActive = value;

          Debug.WriteLine($"{Name} - isACtive  = {value}");

          if (!isActive && !IsDimmed)
          {
            StartTurnOffTimer();

          }
          else if (!IsDimmed && FastMode != FastMode.Immediate)
          {
            StopTurnOffTimer();
          }

          if (isActive)
          {
            TimeSinceActive = 0;
          }

          RaisePropertyChanged();
          turnOffCommand?.RaiseCanExecuteChanged();
        }
      }
    }

    #endregion

    #region Brightness

    private int? brightness;
    public int? Brightness
    {
      get { return brightness; }
      set
      {
        if (value != brightness)
        {
          brightness = value;

          if (brightness != null)
            brightnessSubject.OnNext(brightness.Value);

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsSelected

    private bool isSelected;
    public bool IsSelected
    {
      get { return isSelected; }
      set
      {
        if (value != isSelected)
        {
          isSelected = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TimeSinceActive

    private double? timeSinceActive;

    public double? TimeSinceActive
    {
      get { return timeSinceActive; }
      set
      {
        if (value != timeSinceActive)
        {
          timeSinceActive = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ActualTimerTime

    private TimeSpan? actualTimerTime;

    public TimeSpan? ActualTimerTime
    {
      get { return actualTimerTime; }
      set
      {
        if (value != actualTimerTime)
        {
          actualTimerTime = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TotalSaved

    public double TotalSaved
    {
      get
      {
        return ((TotalDimmTime.TotalHours * PowerOutput) / 1000.0) * CommonSettings.KwhPriceEur;
      }

    }

    #endregion

    #region DaysOfUsingSoftware

    public int DaysOfUsingSoftware
    {
      get { return (int)(DateTime.Now - StartDayOfCounting).TotalDays; }

    }

    #endregion

    #region DimmerOpacity

    public double DimmerOpacity
    {
      get { return Model.DimmerOpacity; }
      set
      {
        if (value != Model.DimmerOpacity)
        {
          Model.DimmerOpacity = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion

    #region TurnOffCommand

    private ActionCommand turnOffCommand;
    public ICommand TurnOffCommand
    {
      get
      {
        return turnOffCommand ??= new ActionCommand(OnTurnOffCommand, CanTurnOffCommnad);
      }
    }

    private bool CanTurnOffCommnad()
    {
      return true;
    }

    private void OnTurnOffCommand()
    {
      Dimm();
    }

    #endregion

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      if (WasInitilized || disposed) return;
      base.Initialize();

      _ = InitializeBrightnessAsync();

      Load(monitorDataFilePath, true);
    }

    #endregion

    private async Task InitializeBrightnessAsync()
    {
      var handle = MonitorHelper.GetHWMonitor(Model.Screen);
      try
      {
        var value = await Task.Run(() => brightnessController.Initilize(handle));
        if (disposed) return;
        Brightness = value;
        if (value != null)
          brightnessSubject.Throttle(TimeSpan.FromSeconds(0.5)).ObserveOnDispatcher()
            .Subscribe(x => brightnessController.RequestBrightness(IsDimmed ? 0 : x)).DisposeWith(this);
      }
      catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is ExternalException)
      {
        Debug.WriteLine(ex);
      }
    }
    #region DimmOrUnDimm

    private DimmerWindow dimmerWindow;
    public void DimmOrUnDimm()
    {
      if (!IsDimmed)
      {
        Dimm();
      }
      else
      {
        UnDimm();
      }
    }

    #endregion

    #region Dimm

    private void Dimm()
    {
      if (disposed || dimmerWindow != null) return;
      var screen = Model.Screen;

      dimmerWindow = new DimmerWindow()
      {
        Width = screen.Bounds.Width,
        Height = screen.Bounds.Height,
        ShowInTaskbar = false,
        ShowActivated = false,
        DataContext = this,
        Topmost = true
      };

      dimmerWindow.Left = screen.Bounds.X + (screen.Bounds.Width / 2) - (dimmerWindow.Width / 2);
      dimmerWindow.Top = screen.Bounds.Y + (screen.Bounds.Height / 2) - (dimmerWindow.Height / 2);

      if (Brightness != null)
      {
        brightnessController.RequestBrightness(0);
      }

      dimmerWindow.Loaded += DimmerWindowLoaded;
      dimmerWindow.Closed += DimmerWindowClosed;

      if (!IsFastMode)
        DimmerOpacity = 1;

      dimmerWindow.Show();
    }

    #endregion

    #region UnDimm

    private void UnDimm()
    {
      CompleteUnDimm(true);
    }

    private void CompleteUnDimm(bool closeWindow)
    {
      var window = dimmerWindow;
      if (window == null) return;
      dimmerWindow = null;
      window.Loaded -= DimmerWindowLoaded;
      window.Closed -= DimmerWindowClosed;

      // Stop accounting before resetting the dimmed state, including the final
      // fraction of a second between the last tick and the close command.
      StopIsDimmedTimer();
      if (closeWindow) window.Close();
      window.DataContext = null;

      if (Brightness != null)
        brightnessController.RequestBrightness(Brightness.Value);

      if (!IsFastMode)
      {
        TurnOffValue = TurnOffLimit;
        Force = false;
      }

      StopTurnOffTimer();
      IsDimmed = false;
      Save();

      if (!disposed && FastMode == FastMode.Immediate)
        StartDelayed(force: true);
    }
    #endregion

    #region Dimmer_Loaded

    private void DimmerWindowLoaded(object sender, RoutedEventArgs e)
    {
      IsDimmed = true;
    }

    #endregion

    #region Dimmer_Closed

    private void DimmerWindowClosed(object sender, System.EventArgs e)
    {
      if (ReferenceEquals(sender, dimmerWindow)) CompleteUnDimm(false);
    }

    #endregion

    #region Turn Off Timer

    #region StartTurnOffTimer

    public void StartTurnOffTimer()
    {
      if (disposed || IsDimmed)
      {
        return;
      }

      if (TurnOffValue == null && ShouldByValue)
      {
        TurnOffValue = 0;
      }

      if (TurnOffValue != null && Model != null)
      {
        Debug.WriteLine($"{Name} - StartTurnOffTimer");

        TimeSinceActive = 0;

        isActiveSerialDisposable.Disposable = automaticTurnOffTimer.OnTimerTick.Subscribe(OnTurnOffTimerTick);

        automaticTurnOffTimer.StartTimer();
      }
      else
      {
        Debug.WriteLine($"{Name} - Nevydareny nepresiel if StartTurnOffTimer");
      }
    }

    #endregion

    #region StopTurnOffTimer

    public void StopTurnOffTimer()
    {
      automaticTurnOffTimer.StopTimer();
      isActiveSerialDisposable.Disposable = null;

      TimeSinceActive = null;
      ActualTimerTime = null;

      Debug.WriteLine($"{Name} - StopTurnOffTimer");
    }

    #endregion

    #region OnTurnOffTimerTick

    private void OnTurnOffTimerTick(long index)
    {
      if (disposed || !automaticTurnOffTimer.IsRunning) return;
      TimeSinceActive = automaticTurnOffTimer.ActualTime;
      var milsLimit = TurnOffValue * 60 * 1000;
      var remaining = milsLimit - TimeSinceActive;
      if (remaining != null)
        ActualTimerTime = TimeSpan.FromSeconds(Math.Max(0, Math.Floor(remaining.Value / 1000)));
      if (TimeSinceActive > milsLimit && !IsDimmed && (!IsActive || IsFastMode || Force))
        Dimm();
    }
    #endregion

    private void StartDelayed(int seconds = 15, bool force = false)
    {
      TurnOffValue = TimeSpan.FromSeconds(seconds).TotalMinutes;

      Force = force;

      if ((!IsDimmed && !IsActive) || force)
      {
        ShouldByValue = true;
        StartTurnOffTimer();
      }
    }

    #endregion

    #region Dimmed Timer

    #region StartIsDimmedTimer

    private void StartIsDimmedTimer()
    {
      lastDimmElapsed = TimeSpan.Zero;
      ActualTimerTime = TimeSpan.Zero;

      isDimmedDisposable.Disposable = dimmerTimer.OnTimerTick.Subscribe(OnIsDimmedTimerTick);

      dimmerTimer.StartTimer();

    }

    #endregion

    #region StopIsDimmedTimer

    public void StopIsDimmedTimer()
    {
      UpdateDimmedTime();
      dimmerTimer.StopTimer();
      isDimmedDisposable.Disposable = null;

      ActualTimerTime = null;
    }

    #endregion

    #region OnTurnOffTimerTick

    private void OnIsDimmedTimerTick(long index)
    {
      if (!disposed) UpdateDimmedTime();
    }

    private void UpdateDimmedTime()
    {
      if (!dimmerTimer.IsRunning) return;
      var elapsed = dimmerTimer.Elapsed;
      var difference = elapsed - lastDimmElapsed;
      lastDimmElapsed = elapsed;
      ActualTimerTime = elapsed;
      if (difference > TimeSpan.Zero) TotalDimmTime += difference;
    }
    #endregion

    #endregion

    #region Save

    private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
    private void Save()
    {
      Task.Run(async () =>
      {
        try
        {
          await semaphoreSlim.WaitAsync();

          if (string.IsNullOrEmpty(monitorDataFilePath))
          {
            return;
          }

          var fileExists = File.Exists(monitorDataFilePath);

          if ((fileExists && wasLoaded) || !fileExists)
          {
            var json = JsonSerializer.Serialize(Model);

            if (!json.Contains('\0') && !string.IsNullOrEmpty(json))
            {
              monitorDataFilePath.EnsureDirectoryExists();

              var serialized = JsonSerializer.Deserialize<ScreenModel>(json);

              if (serialized != null)
              {
                File.WriteAllText(monitorDataFilePath, json);
              }
            }
          }
        }
        finally
        {
          semaphoreSlim.Release();
        }
      });

    }

    #endregion

    #region Load

    private bool wasLoaded;
    private object batton = new object();
    private void Load(string filePath, bool loadBackup)
    {
      lock (batton)
      {
        try
        {
          if (File.Exists(filePath))
          {
            var data = File.ReadAllText(filePath);

            var serialized = JsonSerializer.Deserialize<ScreenModel>(data);

            if (serialized != null)
            {
              serialized.Screen = Model.Screen;

              Model = serialized;
              TotalDimmTime = TimeSpan.FromTicks(serialized.TotalDimmTimeTicks);
              TurnOffValue = TurnOffLimit;

              FastMode = Model.IsFastModeOn ? FastMode.Pernament : FastMode.Off;

              RaisePropertyChanged(nameof(TurnOffLimit));
              RaisePropertyChanged(nameof(PowerOutput));
              RaisePropertyChanged(nameof(StartDayOfCounting));
              RaisePropertyChanged(nameof(ShouldByValue));
              RaisePropertyChanged(nameof(Model));

              wasLoaded = true;

              if (StartDayOfCounting == DateTime.MinValue)
              {
                StartDayOfCounting = DateTime.Now;

                Save();
              }
            }
          }
          else
          {
            wasLoaded = true;

            StartDayOfCounting = DateTime.Now;

            Save();
          }
        }
        catch (JsonException ex)
        {
          if (loadBackup)
            LoadBackup();
        }
      }
    }

    #endregion

    private void LoadBackup()
    {
      var fileName = Path.GetFileName(monitorDataFilePath);
      var backups = new DirectoryInfo(Path.GetDirectoryName(monitorDataFilePath) + "//Backup")
        .GetDirectories().OrderByDescending(x => x.CreationTime);

      foreach (var backup in backups)
      {
        Load(backup.FullName + "//" + fileName, false);

        if (wasLoaded)
          break;
      }
    }

    public Task PendingBrightnessWork => brightnessController.PendingWork;

    public void RaiseDaysOfUsingSoftware() => RaisePropertyChanged(nameof(DaysOfUsingSoftware));

    public void RaiseTotalSaved()
    {
      RaisePropertyChanged(nameof(TotalSaved));
    }

    #region Dispose

    public override void Dispose()
    {
      if (disposed) return;
      disposed = true;
      UnDimm();
      StopTurnOffTimer();
      StopIsDimmedTimer();
      base.Dispose();
      brightnessSubject.Dispose();

      isActiveSerialDisposable?.Dispose();
      isDimmedDisposable?.Dispose();
    }

    #endregion

    #endregion

  }
}
