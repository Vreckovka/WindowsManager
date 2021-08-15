using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using WindowsManager.Modularity;
using WindowsManager.Views;
using SoundManagement;
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

    private ObservableCollection<BlankSoundDevice> knownSoundDevices = new ObservableCollection<BlankSoundDevice>();

    public ObservableCollection<BlankSoundDevice> KnownSoundDevices
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

    #region Methods

    public override void Initialize()
    {
      base.Initialize();

      var json = LoadKnownDevices(knowDevicesPath);

      if (!string.IsNullOrEmpty(json))
      {
        var soundDevices = JsonSerializer.Deserialize<IEnumerable<BlankSoundDevice>>(json);

        foreach (var soundDevice in soundDevices)
        {
          KnownSoundDevices.Add(soundDevice);
        }
      }
    }

    #region SoundDevices_CollectionChanged

    private void SoundDevices_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      if (e.NewItems != null)
      {
        foreach (var device in e.NewItems.OfType<SoundDevice>())
        {
          var newlyConnectedDevice = KnownSoundDevices.SingleOrDefault(x => x.Id == device.ID);

          if (newlyConnectedDevice != null)
          {
            if (AudioDeviceManager.Instance.SelectedSoundDevice != null && AudioDeviceManager.Instance.SelectedSoundDevice.ID != newlyConnectedDevice.Id)
            {
              var actualDevice = KnownSoundDevices.Single(x => x.Id == AudioDeviceManager.Instance.SelectedSoundDevice.ID);

              if (newlyConnectedDevice.Priority < actualDevice.Priority)
              {
                AudioDeviceManager.Instance.SetSelectedSoundDevice(device, false);
              }
            }
          }
          else
          {
            KnownSoundDevices.Add(new BlankSoundDevice()
            {
              Description = device.Description,
              Id = device.ID,
              Priority = KnownSoundDevices.Count
            });

            SaveKnownDevices();
          }
        }
      }
    }

    #endregion

    private void SaveKnownDevices()
    {
      var json = JsonSerializer.Serialize(KnownSoundDevices.OrderBy(x => x.Priority));

      if (!File.Exists(knowDevicesPath))
      {
        var stream = File.Create(knowDevicesPath);
        stream.Flush();
        stream.Close();
      }

      File.WriteAllText(knowDevicesPath, json);
    }

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

    private void KnownSoundDevices_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      for (int i = 0; i < KnownSoundDevices.Count; i++)
      {
        KnownSoundDevices[i].Priority = i;
      }

      SaveKnownDevices();
    }

    #endregion
  }
}