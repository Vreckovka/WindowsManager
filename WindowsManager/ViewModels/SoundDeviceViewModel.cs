using SoundManagement;
using VCore.Standard;

namespace WindowsManager.ViewModels
{
  public class BlankSoundDeviceViewModel : ViewModel<BlankSoundDevice>
  {
    public BlankSoundDeviceViewModel(BlankSoundDevice model) : base(model)
    {
    }

    #region Priority

    public int Priority
    {
      get { return Model.Priority; }
      set
      {
        if (value != Model.Priority)
        {
          Model.Priority = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

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
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region DisableAutomaticConnect

    public bool DisableAutomaticConnect
    {
      get { return Model.DisableAutomaticConnect; }
      set
      {
        if (value != Model.DisableAutomaticConnect)
        {
          Model.DisableAutomaticConnect = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

  }

  public class SoundDeviceViewModel : ViewModel<SoundDevice>
  {
    public SoundDeviceViewModel(SoundDevice model) : base(model)
    {
    }

  

  }
}