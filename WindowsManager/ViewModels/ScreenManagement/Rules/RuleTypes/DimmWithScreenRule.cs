using System.Collections.Generic;

namespace WindowsManager.ViewModels.ScreenManagement.Rules.RuleTypes
{
  public class DimmWithScreenRule : Rule
  {
    public DimmWithScreenRule()
    {
      Parameters = new List<IRuleParameter>()
      {
        new RuleParameterViewModel(RuleNames.MainScreen),
        new RuleParameterViewModel(RuleNames.SecondaryScreen)
      };
    }

    public override IEnumerable<IRuleAction> Types { get; } = new List<IRuleAction>() { IRuleAction.ScreenDimmed, IRuleAction.ScreenUnDimmed };
    public override string Name { get; } = "Deactive with";

    public override void Execute()
    {
      if (Parameters.Count == 2)
      {
        var mainScreen = (ScreenViewModel)Parameters[0].Value;
        var otherScreen = (ScreenViewModel)Parameters[1].Value;

        if (mainScreen == null || otherScreen == null)
          return;

        if (mainScreen.IsDimmed != otherScreen.IsDimmed)
        {
          otherScreen.DimmOrUnDimm();
        }
      }
    }
  }
}