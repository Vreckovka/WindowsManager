using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using WindowsManager.ViewModels.Home;
using WindowsManager.ViewModels.Home.Scrapers;

namespace WindowsManager.TemplateSelectors
{
  public class TorrentTypeTemplateSelector : DataTemplateSelector
  {
    public DataTemplate Video { get; set; }
    public DataTemplate Other { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
      if (item is RargbtTorrentViewModel vm && vm.Model is VideoRargbtTorrent)
      {
        return Video;
      }

      return Other;
    }
  }
}
