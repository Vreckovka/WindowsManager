using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using WindowsManager.ViewModels.ScreenManagement.Rules.RuleTypes;
using WindowsManager.ViewModels.Torrents;

namespace WindowsManager.TemplateSelectors
{
  public class RuleTypeTemplateSelector : DataTemplateSelector
  {
    public DataTemplate ActivateWith { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
      if (item is LinkMonitorsRule vm)
      {
        return ActivateWith;
      }

      return new DataTemplate();
    }
  }
}
