using System.Collections.Generic;
using System.Linq;

namespace WindowsManager.ViewModels.ScreenManagement.Rules.RuleTypes
{
  public static class RuleNames
  {
    public const string MainScreen = "Main screen";
    public const string SecondaryScreen = "Secondary screen";
  }


  public class LinkMonitorsRule : Rule
  {
    public LinkMonitorsRule()
    {
      Parameters = new List<RuleParameterViewModel>()
      {
        new RuleParameterViewModel(RuleNames.MainScreen),
        new RuleParameterViewModel(RuleNames.SecondaryScreen)
      };
    }

    public override IEnumerable<IRuleAction> Types { get; set; } = new List<IRuleAction>() { IRuleAction.ScreenActivated };

    public override string Name { get; set; } = "Link Monitors";

    public override void Execute(ScreenViewModel[] screens)
    {
      if (Parameters.Count == 2)
      {
        var mainScreen = screens.FirstOrDefault(x => x.Model.DeviceName == (string)Parameters[0].Value);
        var otherScreen = screens.FirstOrDefault(x => x.Model.DeviceName == (string)Parameters[1].Value);

        if (mainScreen == null || otherScreen == null)
          return;

        if (mainScreen.IsActive)
        {
          if (!mainScreen.IsDimmed)
          {
            if (!otherScreen.IsDimmed)
            {
              otherScreen.StopTurnOffTimer();
            }
            else
            {
              otherScreen.automaticTurnOffTimer.StopTimer();
            }
          }
        }
        else if (otherScreen.IsActive)
        {
          mainScreen.StopTurnOffTimer();
        }
        else if (!otherScreen.IsActive)
        {
          if (mainScreen.TimeSinceActive == 0 || mainScreen.TimeSinceActive == null)
            mainScreen.StartTurnOffTimer();

          if (otherScreen.TimeSinceActive == 0 || otherScreen.TimeSinceActive == null)
            otherScreen.StartTurnOffTimer();
        }
      }
    }
  }
}
