using System.Collections.Generic;
using System.Windows.Forms;
using VCore.Standard;

namespace WindowsManager.ViewModels.ScreenManagement.Rules
{
  public interface IRuleParameter
  {
    public string Name { get; }
    public object Value { get; set; }
  }

  public class RuleParameterViewModel : ViewModel, IRuleParameter
  {
    public RuleParameterViewModel(string name)
    {
      Name = name;
    }

    public string Name { get; }

    #region Value

    private object pValue;

    public object Value
    {
      get { return pValue; }
      set
      {
        if (value != pValue)
        {
          if (value is ScreenViewModel screen)
          {
            pValue = screen.Model.DeviceName;
          }
          else
          {
            pValue = value;
          }
         
          RaisePropertyChanged();
        }
      }
    }
    
    #endregion
  }

  public enum IRuleAction
  {
    ScreenDimmed,
    ScreenUnDimmed,
    ScreenActivated,
    ScreenUnActivated,
  }

  public interface IRule
  {
    string Name { get; }
    IList<RuleParameterViewModel> Parameters { get; }
    IEnumerable<IRuleAction> Types { get; }

    void Execute(ScreenViewModel[] screens);
  }
}