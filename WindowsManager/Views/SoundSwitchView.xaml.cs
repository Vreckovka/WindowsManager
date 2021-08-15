using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WindowsManager.ViewModels;
using SoundManagement;
using VCore.Standard.Modularity.Interfaces;

namespace WindowsManager.Views
{
  /// <summary>
  /// Interaction logic for SoundSwitchView.xaml
  /// </summary>
  public partial class SoundSwitchView : UserControl, IView
  {
    public SoundSwitchView()
    {
      InitializeComponent();
    }


    void s_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (sender is Grid grid)
      {
        DragDrop.DoDragDrop(grid, grid.DataContext, DragDropEffects.Move);

        //draggedItem.IsSelected = true;

        e.Handled = false;
      }
    }

    void listbox1_Drop(object sender, DragEventArgs e)
    {
      var itemSource = (ObservableCollection<BlankSoundDeviceViewModel>)KnowDevicesListView.ItemsSource;

      BlankSoundDeviceViewModel droppedData = e.Data.GetData(typeof(BlankSoundDeviceViewModel)) as BlankSoundDeviceViewModel;
      BlankSoundDeviceViewModel target = ((Border)(sender)).DataContext as BlankSoundDeviceViewModel;

      int removedIdx = KnowDevicesListView.Items.IndexOf(droppedData);
      int targetIdx = KnowDevicesListView.Items.IndexOf(target);

      if (removedIdx < targetIdx)
      {
        itemSource.Insert(targetIdx + 1, droppedData);
        itemSource.RemoveAt(removedIdx);
      }
      else
      {
        int remIdx = removedIdx + 1;
        if (itemSource.Count + 1 > remIdx)
        {
          itemSource.Insert(targetIdx, droppedData);
          itemSource.RemoveAt(remIdx);
        }
      }
    }
  }
}
