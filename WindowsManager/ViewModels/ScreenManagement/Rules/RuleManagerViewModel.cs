using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using WindowsManager.Modularity;
using WindowsManager.ViewModels.ScreenManagement.Rules.RuleTypes;
using WindowsManager.Views;
using WindowsManager.Views.Rules;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels;

namespace WindowsManager.ViewModels.ScreenManagement.Rules
{
  public class NewRule
  {
    public string RuleType { get; set; }
  }

  public class RuleManagerViewModel : RegionViewModel<RulesManagerView>
  {
    private readonly ScreensManagementViewModel screensManagementViewModel;

    public RuleManagerViewModel(IRegionProvider regionProvider, ScreensManagementViewModel screensManagementViewModel) : base(regionProvider)
    {
      this.screensManagementViewModel = screensManagementViewModel ?? throw new ArgumentNullException(nameof(screensManagementViewModel));
    }

    public override string RegionName { get; protected set; } = RegionNames.MainContent;

    public override string Header => "Rules";

    public IEnumerable<ScreenViewModel> Screens
    {
      get
      {
        return screensManagementViewModel.Screens;
      }
    }

    #region AddCommand

    private ActionCommand<IRule> addCommand;
    public ICommand AddCommand
    {
      get
      {
        return addCommand ??= new ActionCommand<IRule>(OnAdd);
      }
    }

    private void OnAdd(IRule rule)
    {
      if(rule is ActivateWithScreensRule activateWith && activateWith.Parameters[0].Value is ScreenViewModel mainScreen)
      {
        mainScreen.Rules.Add(activateWith);
      }
    }


    #endregion

    public IEnumerable<IRule> RuleTypes
    {
      get
      {
        return new IRule[]
        {
          new ActivateWithScreensRule()
        };
      }
    }
  }
}
