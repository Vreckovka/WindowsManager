using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
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

    public ScreenViewModel(Screen model) : base(model)
    {
      brightnessController = new BrightnessController().DisposeWith(this);
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

    #region IsActive

    private bool isActive;

    public bool IsActive
    {
      get { return isActive; }
      set
      {
        if (value != isActive)
        {
          isActive = value;
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

    public override void Initialize()
    {
      base.Initialize();

      var handle = MonitorHelper.GetHWMonitor(Model);

      Brightness = brightnessController.Initilize(handle);

      if (Brightness != null)
      {
        brightnessController.SetBrightness(80);

        brightnessSubject.Throttle(TimeSpan.FromSeconds(0.5)).Subscribe(x => { brightnessController.SetBrightness(x); }).DisposeWith(this);
      }
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

    #endregion

  }
}