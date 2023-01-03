using System.Collections.Generic;

namespace WindowsManager.ViewModels.ScreenManagement.Rules
{
  public class Rule : IRule
  {
    public IList<RuleParameterViewModel> Parameters { get; set; } = new List<RuleParameterViewModel>();
    public virtual IEnumerable<IRuleAction> Types { get; set; }
    public virtual string Name { get; set; }
    public virtual void Execute(ScreenViewModel[] screens) {}
  }
}