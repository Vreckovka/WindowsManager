﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Input;
using WindowsManager.Modularity;
using WindowsManager.ViewModels.ScreenManagement.Rules.RuleTypes;
using WindowsManager.Views;
using WindowsManager.Views.Rules;
using VCore;
using VCore.ItemsCollections;
using VCore.Standard;
using VCore.Standard.Helpers;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels;

namespace WindowsManager.ViewModels.ScreenManagement.Rules
{
  public class RuleViewModel : ViewModel<IRule>
  {
    public RuleViewModel(IRule model) : base(model)
    {
    }



    #region IsRuleEnabled

    public bool IsRuleEnabled
    {
      get { return Model.IsRuleEnabled; }
      set
      {
        if (value != Model.IsRuleEnabled)
        {
          Model.IsRuleEnabled = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion



  }

  public class RuleManagerViewModel : RegionViewModel<RulesManagerView>
  {
    private string filePath = "Data\\rules.txt";
    public RuleManagerViewModel(IRegionProvider regionProvider) : base(regionProvider)
    {
     
    }

    #region Properties
    public override string RegionName { get; protected set; } = RegionNames.MainContent;

    public override string Header => "Rules";

    #region Rules

    private RxObservableCollection<RuleViewModel> rules = new RxObservableCollection<RuleViewModel>();

    public RxObservableCollection<RuleViewModel> Rules
    {
      get { return rules; }
      private set
      {
        if (value != rules)
        {
          rules = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Screens

    private IEnumerable<ScreenViewModel> screens;

    public IEnumerable<ScreenViewModel> Screens
    {
      get { return screens; }
      set
      {
        if (value != screens)
        {
          screens = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public IEnumerable<IRule> RuleTypes
    {
      get
      {
        return new IRule[]
        {
          new LinkMonitorsRule()
        };
      }
    }

    #endregion

    #region Commands

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
      Rules.Add(new RuleViewModel(rule));

      SaveRules();
    }

    #endregion

    #endregion

    #region Methods

    public override void Initialize()
    {
      base.Initialize();

      LoadRules();
    }

    public void SaveRules()
    {
      filePath.EnsureDirectoryExists();

      File.WriteAllText(filePath, JsonSerializer.Serialize(Rules.Select(x => (Rule)x.Model)));

    }

    private void LoadRules()
    {
      if (File.Exists(filePath))
      {
        var json = File.ReadAllText(filePath);
        var loadedRules = JsonSerializer.Deserialize<Rule[]>(json);

        if (loadedRules != null)
        {
          var linkMonitorsRules = loadedRules.Where(x => x.Name == new LinkMonitorsRule().Name).Select(x =>
          {
            var newOne = new LinkMonitorsRule();

            newOne.Name = x.Name;
            newOne.Parameters = x.Parameters;
            newOne.Types = x.Types;
            newOne.IsRuleEnabled = x.IsRuleEnabled;

            newOne.Parameters.ForEach(par => par.Value = par.Value.ToString());

            return newOne;
          }).Select(x => new RuleViewModel(x));

          Rules = new RxObservableCollection<RuleViewModel>(linkMonitorsRules);
        }
      }
    }

    #endregion
  }
}
