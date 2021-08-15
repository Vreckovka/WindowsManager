using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using WindowsManager.Modularity;
using WindowsManager.Views;
using SoundManagement;
using VCore;
using VCore.Helpers;
using VCore.Modularity.RegionProviders;
using VCore.Standard;
using VCore.Standard.Helpers;
using VCore.ViewModels;

namespace WindowsManager.ViewModels
{
  public class SoundManagerViewModel : RegionViewModel<SoundSwitchView>
  {
    private const string knowDevicesPath = "KnownDevices.txt";

    public SoundManagerViewModel(IRegionProvider regionProvider) : base(regionProvider)
    {
      AudioDeviceManager.Instance.ObservePropertyChange(x => x.SelectedSoundDevice).Subscribe(OnSelectedSoundDevice).DisposeWith(this);
      AudioDeviceManager.Instance.SoundDevices.CollectionChanged += SoundDevices_CollectionChanged;

      KnownSoundDevices.CollectionChanged += KnownSoundDevices_CollectionChanged;
    }

    #region Properties

    public override string RegionName { get; protected set; } = RegionNames.MainContent;

    public override string Header => "Output devices";

    #region KnownSoundDevices

    private ObservableCollection<BlankSoundDeviceViewModel> knownSoundDevices = new ObservableCollection<BlankSoundDeviceViewModel>();

    public ObservableCollection<BlankSoundDeviceViewModel> KnownSoundDevices
    {
      get { return knownSoundDevices; }
      set
      {
        if (value != knownSoundDevices)
        {
          knownSoundDevices = value;
          RaisePropertyChanged();
        }
      }
    }


    #endregion

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

    #region RemoveItem

    private ActionCommand<BlankSoundDeviceViewModel> removeItem;

    public ICommand RemoveItem
    {
      get
      {
        return removeItem ??= new ActionCommand<BlankSoundDeviceViewModel>(RemoveKnownDevice);
      }
    }


    #endregion

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      var json = LoadKnownDevices(knowDevicesPath);

      if (!string.IsNullOrEmpty(json))
      {
        var soundDevices = JsonSerializer.Deserialize<IEnumerable<BlankSoundDevice>>(json);

        foreach (var soundDevice in soundDevices)
        {
          KnownSoundDevices.Add(new BlankSoundDeviceViewModel(soundDevice));
        }
      }
    }

    #endregion

    #region SoundDevices_CollectionChanged

    private void SoundDevices_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      if (e.NewItems != null)
      {
        foreach (var device in e.NewItems.OfType<SoundDevice>())
        {
          if (AudioDeviceManager.Instance.WasLoaded)
          {
            AudioDeviceManager.Instance.SetSelectedSoundDevice(device, false);
          }
        

          var newlyConnectedDevice = KnownSoundDevices.SingleOrDefault(x => x.Model.Id == device.ID);

          if (newlyConnectedDevice == null)
          {
            KnownSoundDevices.Add(new BlankSoundDeviceViewModel(new BlankSoundDevice()
            {
              Description = device.Description,
              Id = device.ID,
              Priority = KnownSoundDevices.Count
            }));
          }
        }
      }
      else if (e.OldItems != null)
      {
        foreach (var device in e.OldItems.OfType<SoundDevice>())
        {
          var newlyConnectedDevice = KnownSoundDevices.SingleOrDefault(x => x.Model.Id == device.ID);

          if (newlyConnectedDevice != null)
          {
            BlankSoundDeviceViewModel newDefaultDevice = null;

            newDefaultDevice = KnownSoundDevices.OrderBy(x => x.Priority).FirstOrDefault(x => AudioDeviceManager.Instance.SoundDevices.SingleOrDefault(y => y.ID == x.Model.Id) != null);

            if (newDefaultDevice != null && AudioDeviceManager.Instance.SelectedSoundDevice.ID == device.ID)
            {
              AudioDeviceManager.Instance.SetSelectedSoundDevice(AudioDeviceManager.Instance.SoundDevices.SingleOrDefault(y => y.ID == newDefaultDevice.Model.Id), false);
            }
          }
        }
      }


      SaveKnownDevices();
    }

    #endregion

    #region SaveKnownDevices

    private void SaveKnownDevices()
    {
      var json = JsonSerializer.Serialize(KnownSoundDevices.OrderBy(x => x.Priority).Select(x => x.Model));

      if (!File.Exists(knowDevicesPath))
      {
        var stream = File.Create(knowDevicesPath);
        stream.Flush();
        stream.Close();
      }

      File.WriteAllText(knowDevicesPath, json);
    }

    #endregion

    #region OnSelectedSoundDevice

    private void OnSelectedSoundDevice(SoundDevice soundDevice)
    {
      DefaultDevice = new SoundDeviceViewModel(soundDevice);

    }

    #endregion

    #region LoadDefaultDevice

    private string LoadKnownDevices(string path)
    {
      if (File.Exists(path))
      {
        return File.ReadAllText(path);
      }

      return null;
    }

    #endregion

    #region KnownSoundDevices_CollectionChanged

    private void KnownSoundDevices_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      for (int i = 0; i < KnownSoundDevices.Count; i++)
      {
        KnownSoundDevices[i].Priority = i;
      }

      SaveKnownDevices();
    }

    #endregion

    private void RemoveKnownDevice(BlankSoundDeviceViewModel blankSoundDeviceViewModel)
    {
      KnownSoundDevices.Remove(blankSoundDeviceViewModel);

      for (int i = 0; i < KnownSoundDevices.Count; i++)
      {
        KnownSoundDevices[i].Priority = i;
      }

      SaveKnownDevices();
    }

    #endregion
  }
}