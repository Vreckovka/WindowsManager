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

    public ScreenViewModel(Screen model, string fileName) : base(model)
    {
      brightnessController = new BrightnessController().DisposeWith(this);

      filePath = fileName;
    }

    #region Properties

    #region IsDimmed

    private bool isDimmed;

    public bool IsDimmed
    {
      get { return isDimmed; }
      private set
      {
        if (value != isDimmed)
        {
          isDimmed = value;
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
            SetDimmTimer();

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

          if (!isActive)
            SetDimmTimer();
          else
          {
            StopTurnOffTimer();
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

    #region TimeTillTurnOff

    private double? timeTillTurnOff;

    public double? TimeTillTurnOff
    {
      get { return timeTillTurnOff; }
      set
      {
        if (value != timeTillTurnOff)
        {
          timeTillTurnOff = value;
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

    #region SetDimmTimer

    private void SetDimmTimer()
    {
      if (TurnOffLimit != null)
      {
        TimeSinceActive = 0;
      }

      isActiveSerialDisposable.Disposable = Observable.Interval(TimeSpan.FromSeconds(0.5)).Subscribe(OnTimerDimm);
    }

    #endregion

    #region OnTimerDimm

    private Stopwatch stopWatch = new Stopwatch();

    private void OnTimerDimm(long index)
    {
      stopWatch.Stop();
    

      if (TurnOffLimit != null)
      {
        if (TimeSinceActive == null)
        {
          TimeSinceActive = 0;
        }

        TimeSinceActive += stopWatch.ElapsedMilliseconds / 1000.0 / 60.0;
        TimeTillTurnOff = TurnOffLimit - TimeSinceActive;

        if (TimeSinceActive > TurnOffLimit)
        {
          System.Windows.Application.Current.Dispatcher.Invoke(() =>
          {
            if (!IsDimmed)
            {
              Dimm();
            }
          });
        }
      }

      stopWatch.Reset();
      stopWatch.Start();
    }

    #endregion

    public void StopTurnOffTimer()
    {
      isActiveSerialDisposable.Disposable?.Dispose();
      TimeSinceActive = null;
      TimeTillTurnOff = null;
    }

    private void Save()
    {
      File.WriteAllText(filePath, TurnOffLimit.ToString());
    }

    private void Load()
    {
      if (File.Exists(filePath))
      {
        var data = File.ReadAllText(filePath);

        TurnOffLimit = double.Parse(data);
      }
    }

    #region Dispose

    public override void Dispose()
    {
      base.Dispose();

      isActiveSerialDisposable?.Dispose();
    }

    #endregion

    #endregion

  }
}