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
using VCore.ItemsCollections;
using VCore.Standard;
using VCore.Standard.Helpers;
using VCore.WPF.Helpers;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels;

namespace WindowsManager.ViewModels
{
  public class SoundManagerViewModel : RegionViewModel<SoundSwitchView>
  {
    private const string knowDevicesPath = "KnownDevices.txt";

    public SoundManagerViewModel(IRegionProvider regionProvider) : base(regionProvider)
    {
      AudioDeviceManager.Instance.ObservePropertyChange(x => x.SelectedSoundDevice).Subscribe(OnSelectedSoundDevice).DisposeWith(this);
      AudioDeviceManager.Instance.SoundDevices.CollectionChanged += SoundDevices_CollectionChanged;
    }

    #region Properties

    public override string RegionName { get; protected set; } = RegionNames.MainContent;

    public override string Header => "Output devices";

    #region KnownSoundDevices

    private RxObservableCollection<BlankSoundDeviceViewModel> knownSoundDevices = new RxObservableCollection<BlankSoundDeviceViewModel>();

    public RxObservableCollection<BlankSoundDeviceViewModel> KnownSoundDevices
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
          if (defaultDevice != null)
          {
            var lastSelected = KnownSoundDevices.SingleOrDefault(x => x.Model.Description == defaultDevice.Model.Description);

            if (lastSelected != null)
              lastSelected.IsDefault = false;
          }

          defaultDevice = value;

          if (defaultDevice != null)
          {
            var selected = KnownSoundDevices.SingleOrDefault(x => x.Model.Description == defaultDevice.Model.Description);

            if (selected != null)
              selected.IsDefault = true;
          }

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion

    #region RemoveSoundItem

    private ActionCommand<BlankSoundDeviceViewModel> removeSoundItem;

    public ICommand RemoveSoundItem
    {
      get
      {
        return removeSoundItem ??= new ActionCommand<BlankSoundDeviceViewModel>(RemoveKnownDevice);
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
        var soundDevices = JsonSerializer.Deserialize<IEnumerable<BlankSoundDevice>>(json)?.ToList();

        if (soundDevices != null)
          foreach (var soundDevice in soundDevices)
          {
            KnownSoundDevices.Add(new BlankSoundDeviceViewModel(soundDevice));
          }
      }

      KnownSoundDevices.ItemUpdated.Subscribe((x) =>
      {
        SaveKnownDevices();
      });


      KnownSoundDevices.CollectionChanged += KnownSoundDevices_CollectionChanged;
    }

    #endregion

    #region SoundDevices_CollectionChanged

    private void SoundDevices_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      AudioDeviceManager.Instance.UpdateIndexes();

      if (e.NewItems != null)
      {
        foreach (var device in e.NewItems.OfType<SoundDevice>())
        {
          var blankDevice = KnownSoundDevices.SingleOrDefault(x => x.Model.Description == device.Description);

          if (AudioDeviceManager.Instance.WasLoaded && blankDevice != null && !blankDevice.DisableAutomaticConnect)
          {
            AudioDeviceManager.Instance.SetSelectedSoundDevice(AudioDeviceManager.Instance.SoundDevices.SingleOrDefault(y => y.Description == blankDevice.Model.Description), false);
          }

          var newlyConnectedDevice = KnownSoundDevices.SingleOrDefault(x => x.Model.Description == device.Description);

          if (newlyConnectedDevice == null)
          {
            KnownSoundDevices.Add(new BlankSoundDeviceViewModel(new BlankSoundDevice()
            {
              Description = device.Description,
              Id = device.ID,
              Priority = KnownSoundDevices.Count
            }));
          }
          else
          {
            newlyConnectedDevice.Model.Id = device.ID;
          }
        }
      }
      else if (e.OldItems != null)
      {
        foreach (var device in e.OldItems.OfType<SoundDevice>())
        {
          var newlyConnectedDevice = KnownSoundDevices.SingleOrDefault(x => x.Model.Description == device.Description);

          if (newlyConnectedDevice != null)
          {
            BlankSoundDeviceViewModel newDefaultDevice = null;

            newDefaultDevice = KnownSoundDevices.OrderBy(x => x.Priority).FirstOrDefault(x => AudioDeviceManager.Instance.SoundDevices.SingleOrDefault(y => y.Description == x.Model.Description) != null);

            if (newDefaultDevice != null && AudioDeviceManager.Instance.SelectedSoundDevice.Description == device.Description)
            {
              AudioDeviceManager.Instance.SetSelectedSoundDevice(AudioDeviceManager.Instance.SoundDevices.SingleOrDefault(y => y.Description == newDefaultDevice.Model.Description), false);
            }
          }
        }
      }

      if (AudioDeviceManager.Instance.WasLoaded)
        SaveKnownDevices();
    }

    #endregion

    #region SaveKnownDevices

    private object batton = new object();
    private void SaveKnownDevices()
    {
      lock (batton)
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

    #region RemoveKnownDevice

    public void RemoveKnownDevice(BlankSoundDeviceViewModel blankSoundDeviceViewModel)
    {
      KnownSoundDevices.Remove(blankSoundDeviceViewModel);

      for (int i = 0; i < KnownSoundDevices.Count; i++)
      {
        KnownSoundDevices[i].Priority = i;
      }

      SaveKnownDevices();
    }

    #endregion

    #endregion
  }
}