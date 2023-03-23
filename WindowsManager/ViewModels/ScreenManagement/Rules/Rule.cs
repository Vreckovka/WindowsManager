using System.Collections.Generic;

namespace WindowsManager.ViewModels.ScreenManagement.Rules
{
  public class Rule : IRule
  {
    public IList<RuleParameterViewModel> Parameters { get; set; } = new List<RuleParameterViewModel>();
    public virtual IEnumerable<IRuleAction> Types { get; set; }

    /// <summary>
    /// IsEnabled is overused term
    /// </summary>
    public bool IsRuleEnabled { get; set; } = true;

    public virtual string Name { get; set; }
    public virtual void Execute(ScreenViewModel[] screens) {}
    public virtual void Revert(ScreenViewModel[] screens) { }
  }
}