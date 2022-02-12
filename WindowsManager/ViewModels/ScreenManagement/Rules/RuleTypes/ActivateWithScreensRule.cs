using System.Collections.Generic;

namespace WindowsManager.ViewModels.ScreenManagement.Rules.RuleTypes
{
  public static class RuleNames
  {
    public const string MainScreen = "Main screen";
    public const string SecondaryScreen = "Secondary screen";
  }


  public class ActivateWithScreensRule : Rule
  {
    public ActivateWithScreensRule()
    {
      Parameters = new List<IRuleParameter>()
      {
        new RuleParameterViewModel(RuleNames.MainScreen),
        new RuleParameterViewModel(RuleNames.SecondaryScreen)
      };
    }

    public override IEnumerable<IRuleAction> Types { get; } = new List<IRuleAction>() { IRuleAction.ScreenActivated };

    public override string Name { get; } = "Activate with";

    public override void Execute()
    {
      if (Parameters.Count == 2)
      {
        var mainScreen = (ScreenViewModel)Parameters[0].Value;
        var otherScreen = (ScreenViewModel)Parameters[1].Value;

        if (mainScreen == null || otherScreen == null)
          return;

        if (mainScreen.IsActive)
        {
          if (!mainScreen.IsDimmed)
          {
            if (otherScreen.IsDimmed)
            {
              otherScreen.DimmOrUnDimm();
            }

            otherScreen.StopTurnOffTimer();
          }
        }
        else if (!otherScreen.IsActive)
        {
          mainScreen.StartTurnOffTimer();
        }
      }
    }
  }
}
