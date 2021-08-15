using SoundManagement;
using VCore.Standard;

namespace WindowsManager.ViewModels
{
  public class SoundDeviceViewModel : ViewModel<SoundDevice>
  {
    public SoundDeviceViewModel(SoundDevice model) : base(model)
    {
    }

    public int Order { get; set; }
  }
}