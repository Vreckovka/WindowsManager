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

   
  }

  public class SoundDeviceViewModel : ViewModel<SoundDevice>
  {
    public SoundDeviceViewModel(SoundDevice model) : base(model)
    {
    }

  

  }
}