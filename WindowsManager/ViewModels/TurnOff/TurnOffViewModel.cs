using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using WindowsManager.Modularity;
using WindowsManager.Views;
using VCore;
using VCore.Standard;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels;

namespace WindowsManager.ViewModels.TurnOff
{
  public class TurnOffViewModel : RegionViewModel<TurnOffView>
  {
    #region Fields

    private Timer timer = new Timer();
    private string fileName = "turnOff.txt";

    #endregion

    #region Constructors

    public TurnOffViewModel(IRegionProvider regionProvider) : base(regionProvider)
    {
      InitilizeTimeCollections();
    }

    #endregion

    #region Properties

    public override string RegionName { get; protected set; } = RegionNames.MainContent;

    public override string Header => "Shut down";

    #region TimeLeft

    private TimeSpan? timeLeft;

    public TimeSpan? TimeLeft
    {
      get { return timeLeft; }
      set
      {
        if (value != timeLeft)
        {
          timeLeft = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region SelectedHours

    private int selectedHours;

    public int SelectedHours
    {
      get { return selectedHours; }
      set
      {
        if (value != selectedHours)
        {
          selectedHours = value;
          startCommand?.RaiseCanExecuteChanged();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region SelectedMinutes

    private int selectedMinutes;

    public int SelectedMinutes
    {
      get { return selectedMinutes; }
      set
      {
        if (value != selectedMinutes)
        {
          selectedMinutes = value;
          startCommand?.RaiseCanExecuteChanged();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsPaused

    private bool? isPaused;

    public bool? IsPaused
    {
      get { return isPaused; }
      set
      {
        if (value != isPaused)
        {
          isPaused = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region CanChangedTime

    private bool canChangedTime = true;

    public bool CanChangedTime
    {
      get { return canChangedTime; }
      set
      {
        if (value != canChangedTime)
        {
          canChangedTime = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public IEnumerable<int> HoursCollection { get; private set; }
    public IEnumerable<int> MinutesCollection { get; private set; }

    #endregion

    #region Commands

    #region StartCommand

    private ActionCommand startCommand;

    public ICommand StartCommand
    {
      get
      {
        return startCommand ??= new ActionCommand(OnStartCommand, CanStartCommand);
      }
    }

    private void OnStartCommand()
    {
      try
      {
        if (SelectedHours > 0 || SelectedMinutes > 0)
        {
          TimeLeft = new TimeSpan(SelectedHours, SelectedMinutes, 0);
        }

        SaveTime();
        timer.Enabled = true;
        IsPaused = false;
        CanChangedTime = false;

        startCommand.RaiseCanExecuteChanged();
        pauseCommand.RaiseCanExecuteChanged();
        stopCommand.RaiseCanExecuteChanged();
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message);
      }
    }

    private bool CanStartCommand()
    {
      return (SelectedHours > 0 || SelectedMinutes > 0) && isPaused == null;
    }

    #endregion

    #region StopCommand

    private ActionCommand stopCommand;

    public ICommand StopCommand
    {
      get
      {
        return stopCommand ??= new ActionCommand(OnStopCommand, CanStop);
      }
    }

    private void OnStopCommand()
    {
      TimeLeft = null;
      timer.Enabled = false;
      IsPaused = null;
      CanChangedTime = true;

      startCommand.RaiseCanExecuteChanged();
      pauseCommand.RaiseCanExecuteChanged();
      stopCommand.RaiseCanExecuteChanged();
    }

    private bool CanStop()
    {
      return timer.Enabled || IsPaused != null;
    }

    #endregion

    #region PauseCommand

    private ActionCommand pauseCommand;

    public ICommand PauseCommand
    {
      get
      {
        return pauseCommand ??= new ActionCommand(OnPauseCommand, CanPause);
      }
    }

    private void OnPauseCommand()
    {
      IsPaused = !IsPaused;

      timer.Enabled = !IsPaused.Value;
    }

    private bool CanPause()
    {
      return isPaused != null;
    }

    #endregion

    #endregion

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      timer.Elapsed += OnTimedEvent;
      timer.Interval = 1000;
      LoadTime();
    }

    #endregion

    private void LoadTime()
    {
      if (File.Exists(fileName))
      {
        var time = File.ReadAllText(fileName)?.Split(":");

        if (time != null && time.Length > 1)
        {
          SelectedHours = int.Parse(time[0]);
          SelectedMinutes = int.Parse(time[1]);
        }
      }
    }

    private void SaveTime()
    {
      File.WriteAllText(fileName, $"{SelectedHours}:{SelectedMinutes}");
    }

    #region InitilizeTimeCollections

    private void InitilizeTimeCollections()
    {
      var hours = new List<int>();
      var minutes = new List<int>();

      for (int i = 0; i < 24; i++)
      {
        hours.Add(i);
      }

      for (int i = 0; i < 60; i += 5)
      {
        minutes.Add(i);
      }

      HoursCollection = hours;
      MinutesCollection = minutes;
    }

    #endregion

    #region OnTimedEvent

    private void OnTimedEvent(object source, ElapsedEventArgs e)
    {
      if (TimeLeft != null)
      {
        TimeLeft = new TimeSpan(TimeLeft.Value.Hours, TimeLeft.Value.Minutes, TimeLeft.Value.Seconds - 1);

        if (TimeLeft.Value.TotalSeconds <= 0)
        {
          timer.Enabled = false;
          ShutDown();
        }
      }
    }

    #endregion

    #region ShutDown

    private void ShutDown()
    {
      //To cancel autoShutDown
      //shutdown –a
      Process process = new Process();
      process.StartInfo.FileName = "cmd.exe";
      process.StartInfo.CreateNoWindow = true;
      process.StartInfo.RedirectStandardInput = true;
      process.StartInfo.RedirectStandardOutput = true;
      process.StartInfo.UseShellExecute = false;
      process.Start();

      string shutDown = "shutdown /s /t 0";
      //string stopShutDown = "shutdown -a";

      process.StandardInput.WriteLine(shutDown);
      process.StandardInput.Flush();
      process.StandardInput.Close();
      process.WaitForExit();

      Console.WriteLine(process.StandardOutput.ReadToEnd());
      Console.Read();
    }

    #endregion

    #region Dispose

    public override void Dispose()
    {
      base.Dispose();

      timer.Elapsed -= OnTimedEvent;

    }

    #endregion

    #endregion


  }
}
