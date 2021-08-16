using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using WindowsManager.Windows;
using VCore;
using VCore.Standard;
using VCore.Standard.Helpers;

namespace WindowsManager.ViewModels
{
  public class ScreenViewModel : ViewModel<Screen>
  {
    private BrightnessController brightnessController;
    private ReplaySubject<int> brightnessSubject = new ReplaySubject<int>(1);

    private string filePath;

    private ActionTimer automaticTurnOffTimer;
    private ActionTimer dimmerTimer;

    public ScreenViewModel(Screen model, string fileName) : base(model)
    {
      brightnessController = new BrightnessController().DisposeWith(this);

      automaticTurnOffTimer = new ActionTimer(TimeSpan.FromSeconds(0.1));
      dimmerTimer = new ActionTimer(TimeSpan.FromSeconds(0.1));

      filePath = fileName;
    }

    #region Properties

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
      get { return Model.DeviceName; }

    }

    #endregion

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

            File.Delete(filePath);
          }
          else
          {
            StartTurnOffTimer();

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

    public bool IsActive
    {
      get { return isActive; }
      set
      {
        if (value != isActive)
        {
          isActive = value;

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

    #endregion

    #region TurnOffCommand

    private ActionCommand turnOffCommand;

    public ICommand TurnOffCommand
    {
      get
      {
        return turnOffCommand ??= new ActionCommand(DimmOrUnDimm);
      }
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

    #region TurnOff

    #region StartTurnOffTimer

    private void StartTurnOffTimer()
    {
      if (TurnOffLimit != null)
      {
        TimeSinceActive = 0;

        isActiveSerialDisposable.Disposable = automaticTurnOffTimer.OnTimerTick.Subscribe(OnTurnOffTimerTick);

        automaticTurnOffTimer.StartTimer();
      }
    }

    #endregion

    #region StopTurnOffTimer

    public void StopTurnOffTimer()
    {
      automaticTurnOffTimer.StopTimer();

      TimeSinceActive = null;
      ActualTimerTime = null;
    }

    #endregion

    #region OnTurnOffTimerTick

    private void OnTurnOffTimerTick(long index)
    {
      System.Windows.Application.Current.Dispatcher.Invoke(() =>
      {
        TimeSinceActive = automaticTurnOffTimer.ActualTime / 1000.0 / 60;

        var milis = TurnOffLimit - TimeSinceActive;

        if (milis != null)
          ActualTimerTime = TimeSpan.FromMilliseconds(milis.Value);


        if (TimeSinceActive > TurnOffLimit)
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

    #region IsDimmedTimer

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
          ActualTimerTime = TimeSpan.FromMilliseconds(dimmerTimer.ActualTime.Value);
      });
    }

    #endregion

    #endregion

    #region Save

    private void Save()
    {
      File.WriteAllText(filePath, TurnOffLimit.ToString());
    }

    #endregion

    #region Load

    private void Load()
    {
      if (File.Exists(filePath))
      {
        var data = File.ReadAllText(filePath);

        TurnOffLimit = double.Parse(data);
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