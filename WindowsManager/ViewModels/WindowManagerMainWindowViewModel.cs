using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using VCore.Helpers;
using VCore.Standard;
using VCore.Standard.Helpers;
using VCore.ViewModels;

namespace WindowsManager.ViewModels
{
  public class SoundDeviceViewModel : ViewModel<SoundDevice>
  {
    public SoundDeviceViewModel(SoundDevice model) : base(model)
    {
    }


    #region IsDefault

    private bool isDefault;

    public bool IsDefault
    {
      get { return isDefault; }
      set
      {
        if (value != isDefault)
        {
          isDefault = value;

          if (value)
          {
            WindowManagerMainWindowViewModel.SaveDefaultDevice(Model.ID);
          }
          else
          {
            WindowManagerMainWindowViewModel.SaveDefaultDevice(null);
          }

          RaisePropertyChanged();
        }
      }
    }

    #endregion

  }

  public class WindowManagerMainWindowViewModel : BaseMainWindowViewModel
  {
    private const string defaultDevicePath = "DefaultDevice.txt";
    private static string defaultDeviceKey = null;

    public WindowManagerMainWindowViewModel(
      ScreensManagementViewModel screensManagementViewModel,
      TurnOffViewModel turnOffViewModel)
    {
      ScreensManagementViewModel = screensManagementViewModel;
      TurnOffViewModel = turnOffViewModel;

      AudioDeviceManager.Instance.ObservePropertyChange(x => x.SelectedSoundDevice).Subscribe(OnSelectedSoundDevice).DisposeWith(this);
      AudioDeviceManager.Instance.SoundDevices.CollectionChanged += SoundDevices_CollectionChanged;

      defaultDeviceKey = LoadDefaultDevice(defaultDevicePath);
    }

    #region Properties

    public override string Title => "Window manager";
    public ScreensManagementViewModel ScreensManagementViewModel { get; set; }
    public TurnOffViewModel TurnOffViewModel { get; set; }


    #region DefaultDevice

    private SoundDeviceViewModel defaultDevice;

    public SoundDeviceViewModel DefaultDevice
    {
      get { return defaultDevice; }
      set
      {
        if (value != defaultDevice)
        {
          defaultDevice = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion


    #endregion

    #region Commands

    public ICommand SwitchBehaviorCommand { get; set; }

    #endregion

    #region Methods

    #region SoundDevices_CollectionChanged

    private void SoundDevices_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      if (e.NewItems != null)
      {
        var device = e.NewItems.OfType<SoundDevice>().SingleOrDefault(x => x.ID == defaultDeviceKey);

        if (device != null)
        {
          AudioDeviceManager.Instance.SetSelectedSoundDevice(device, false);
        }
         
      }
    }

    #endregion

    #region OnSelectedSoundDevice

    private void OnSelectedSoundDevice(SoundDevice soundDevice)
    {
      DefaultDevice = new SoundDeviceViewModel(soundDevice);

      if (soundDevice.ID == defaultDeviceKey)
      {
        DefaultDevice.IsDefault = true;
      }
    }

    #endregion

    #region OnClose

    protected override void OnClose(Window window)
    {
      window.WindowState = WindowState.Minimized;
    }

    #endregion

    #region SaveDefaultDevice

    internal static void SaveDefaultDevice(string defaultDeviceID)
    {
      if (defaultDeviceKey != defaultDeviceID)
      {
        File.WriteAllText(defaultDevicePath, defaultDeviceID);

        defaultDeviceKey = defaultDeviceID;
      }
    }

    #endregion

    #region LoadDefaultDevice

    private string LoadDefaultDevice(string path)
    {
      if (File.Exists(path))
      {
        return File.ReadAllText(path);
      }

      return null;
    }

    #endregion

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

    #endregion
  }
}
