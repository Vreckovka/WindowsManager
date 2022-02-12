using System.Collections.Generic;
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
          pValue = value;
          RaisePropertyChanged();
        }
      }
    }
    
    #endregion

  }

  public interface IRule
  {
    string Name { get; }
    IList<IRuleParameter> Parameters { get; }


    void Execute();
  }
}