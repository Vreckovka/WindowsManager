using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using WindowsManager.Windows;
using VCore;
using VCore.Standard;
using VCore.Standard.Helpers;
using VCore.WPF.Misc;

namespace WindowsManager.ViewModels.ScreenManagement
{
  public class ScreenViewModel : ViewModel<Screen>
  {
    private BrightnessController brightnessController;
    private ReplaySubject<int> brightnessSubject = new ReplaySubject<int>(1);

    private string filePath;

    private ActionTimer automaticTurnOffTimer;
    private ActionTimer dimmerTimer;

    public ScreenViewModel() : base(null)
    {

    }

    public ScreenViewModel(Screen model, string fileName) : base(model)
    {
      brightnessController = new BrightnessController().DisposeWith(this);

      automaticTurnOffTimer = new ActionTimer(TimeSpan.FromSeconds(0.1));
      dimmerTimer = new ActionTimer(TimeSpan.FromSeconds(0.1));

      filePath = fileName;
    }

    #region Properties

    #region TotalDimmTime

    private TimeSpan totalDimmTime;

    [JsonIgnore]
    public TimeSpan TotalDimmTime
    {
      get { return totalDimmTime; }
      set
      {
        if (value != totalDimmTime)
        {
          totalDimmTime = value;
          TotalDimmTimeTicks = totalDimmTime.Ticks;

          if (((int)totalDimmTime.TotalSeconds) % 5 == 0)
          {
            Save();
          }

          RaisePropertyChanged(nameof(TotalSaved));

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public long TotalDimmTimeTicks { get; set; }

    #region IsDimmed

    private bool isDimmed;

    private SerialDisposable isDimmedDisposable = new SerialDisposable();

    [JsonIgnore]
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
      get { return Model?.DeviceName; }

    }

    #endregion

    public bool ShouldByValue { get; set; }

    #region TurnOffLimit

    private double? turnOffLimit = null;

    public double? TurnOffLimit
    {
      get { return turnOffLimit; }
      set
      {
        if (value != turnOffLimit)
        {
          turnOffLimit = value;

          if (turnOffLimit == 0)
          {
            StopTurnOffTimer();
            ShouldByValue = false;
            File.Delete(filePath);
          }
          else
          {
            if (!IsDimmed && !IsActive)
            {
              ShouldByValue = true;
              StartTurnOffTimer();
            }

            Save();
          }

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsActive

    private bool isActive;
    private SerialDisposable isActiveSerialDisposable = new SerialDisposable();

    [JsonIgnore]
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
          else if (!IsDimmed)
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

    [JsonIgnore]
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

    [JsonIgnore]
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

    [JsonIgnore]
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

    [JsonIgnore]
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

    #region PowerOutput

    private double powerOutput;

    public double PowerOutput
    {
      get { return powerOutput; }
      set
      {
        if (value != powerOutput)
        {
          powerOutput = value;
          Save();
          RaisePropertyChanged(nameof(TotalSaved));
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TotalSaved

    private double kwhCost = 0.09;

    [JsonIgnore]
    public double TotalSaved
    {
      get
      {
        return TotalDimmTime.TotalHours * (PowerOutput / 1000.0) * kwhCost;
      }

    }

    #endregion

    #region StartDayOfCounting

    private DateTime startDayOfCounting;

    public DateTime StartDayOfCounting
    {
      get { return startDayOfCounting; }
      set
      {
        if (value != startDayOfCounting)
        {
          startDayOfCounting = value;

          RaisePropertyChanged();
          RaisePropertyChanged(nameof(DaysOfUsingSoftware));
        }
      }
    }

    #endregion

    #region DaysOfUsingSoftware

    [JsonIgnore]
    public int DaysOfUsingSoftware
    {
      get { return (int)(DateTime.Now - StartDayOfCounting).TotalDays; }

    }

    #endregion


    [JsonIgnore]
    public override Screen Model { get => base.Model; set => base.Model = value; }

    #endregion

    #region TurnOffCommand

    private ActionCommand turnOffCommand;

    [JsonIgnore]
    public ICommand TurnOffCommand
    {
      get
      {
        return turnOffCommand ??= new ActionCommand(DimmOrUnDimm, CanTurnOffCommnad);
      }
    }

    private bool CanTurnOffCommnad()
    {
      return true;
    }

    #endregion

    #region Methods

    #region Initialize

    public override async void Initialize()
    {
      base.Initialize();

      var handle = MonitorHelper.GetHWMonitor(Model);

      Brightness = brightnessController.Initilize(handle);

      if (Brightness != null)
      {
        Brightness = 80;

        brightnessSubject.Throttle(TimeSpan.FromSeconds(0.5)).Subscribe(x => { brightnessController.SetBrightness(x); }).DisposeWith(this);
      }

      Load();
    }

    #endregion

    #region DimmOrUnDimm

    private DimmerWindow dimmer;
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
      dimmer = new DimmerWindow()
      {
        Width = 100,
        Height = 100,
        ShowInTaskbar = false,
        Topmost = true
      };

      dimmer.Left = Model.Bounds.X + (Model.Bounds.Width / 2) - (dimmer.Width / 2);
      dimmer.Top = Model.Bounds.Y + (Model.Bounds.Height / 2) - (dimmer.Height / 2);

      if (Brightness != null)
      {
        brightnessController.SetBrightness(0);
      }

      dimmer.Loaded += Dimmer_Loaded;
      dimmer.Closed += Dimmer_Closed;

      dimmer.Show();
    }

    #endregion

    #region UnDimm

    private void UnDimm()
    {
      dimmer.Loaded -= Dimmer_Loaded;
      dimmer.Closed -= Dimmer_Closed;

      dimmer.Close();

      if (Brightness != null)
      {
        brightnessController.SetBrightness(Brightness.Value);
      }

      IsDimmed = false;
    }

    #endregion

    #region Dimmer_Loaded

    private void Dimmer_Loaded(object sender, RoutedEventArgs e)
    {
      ((Window)sender).WindowState = WindowState.Maximized;

      IsDimmed = true;
    }

    #endregion

    #region Dimmer_Closed

    private void Dimmer_Closed(object sender, System.EventArgs e)
    {
      UnDimm();
    }

    #endregion

    #region Turn Off Timer

    #region StartTurnOffTimer

    public void StartTurnOffTimer()
    {
      if (IsDimmed)
      {
        return;
      }

      if (TurnOffLimit == null && ShouldByValue)
      {
        TurnOffLimit = 0;
      }

      if (TurnOffLimit != null && Model != null)
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

      TimeSinceActive = null;
      ActualTimerTime = null;

      Debug.WriteLine($"{Name} - StopTurnOffTimer");
    }

    #endregion

    #region OnTurnOffTimerTick

    private void OnTurnOffTimerTick(long index)
    {
      System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
      {
        TimeSinceActive = automaticTurnOffTimer.ActualTime;


        var milsLimit = (TurnOffLimit * 60 * 1000);
        var milis = milsLimit - TimeSinceActive;

        if (milis != null)
          ActualTimerTime = TimeSpan.FromMilliseconds(milis.Value);


        if (TimeSinceActive > milsLimit)
        {
          if (!IsDimmed && !IsActive)
          {
            Dimm();
          }
        }
      });
    }

    #endregion

    #endregion

    #region Dimmed Timer

    #region StartIsDimmedTimer

    private void StartIsDimmedTimer()
    {
      ActualTimerTime = new TimeSpan(0);

      isDimmedDisposable.Disposable = dimmerTimer.OnTimerTick.Subscribe(OnIsDimmedTimerTick);

      dimmerTimer.StartTimer();

    }

    #endregion

    #region StopIsDimmedTimer

    public void StopIsDimmedTimer()
    {
      dimmerTimer.StopTimer();

      ActualTimerTime = null;
    }

    #endregion

    #region OnTurnOffTimerTick

    private void OnIsDimmedTimerTick(long index)
    {
      System.Windows.Application.Current.Dispatcher.Invoke(() =>
      {
        if (dimmerTimer.ActualTime != null)
        {
          var oldValue = ActualTimerTime;

          ActualTimerTime = TimeSpan.FromMilliseconds(dimmerTimer.ActualTime.Value);

          var diff = ActualTimerTime - oldValue;

          if (diff != null)
          {
            TotalDimmTime += diff.Value;
          }
        }
      });
    }

    #endregion

    #endregion

    #region Save

    private void Save()
    {
      if (string.IsNullOrEmpty(filePath))
      {
        return;
      }

      var fileExists = File.Exists(filePath);

      if ((fileExists && wasLoaded ) || !fileExists)
      {
        var json = JsonSerializer.Serialize(this);

        if (!json.Contains('\0') && !string.IsNullOrEmpty(json))
        {
          filePath.EnsureDirectoryExists();

          File.WriteAllText(filePath, json);
        }
      }
    }

    #endregion

    #region Load

    private bool wasLoaded;
    private object batton = new object();
    private void Load()
    {
      lock (batton)
      {
        try
        {
          if (File.Exists(filePath))
          {
            var data = File.ReadAllText(filePath);

            var serialized = JsonSerializer.Deserialize<ScreenViewModel>(data);

            if (serialized != null)
            {
              TotalDimmTime = TimeSpan.FromTicks(serialized.TotalDimmTimeTicks);
              TurnOffLimit = serialized.TurnOffLimit;
              PowerOutput = serialized.PowerOutput;
              StartDayOfCounting = serialized.StartDayOfCounting;
              ShouldByValue = serialized.ShouldByValue;

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
        } 
      }
    }

    #endregion

    #region Dispose

    public override void Dispose()
    {
      base.Dispose();

      isActiveSerialDisposable?.Dispose();
      isDimmedDisposable?.Dispose();
    }

    #endregion

    #endregion

  }
}