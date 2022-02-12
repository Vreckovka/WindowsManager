using System.Collections.Generic;

namespace WindowsManager.ViewModels.ScreenManagement.Rules
{
  public abstract class Rule : IRule
  {
    public IList<IRuleParameter> Parameters { get; protected set; } = new List<IRuleParameter>();
    public abstract IEnumerable<IRuleAction> Types { get; }
    public abstract string Name { get; }



    public abstract void Execute();
  }
}