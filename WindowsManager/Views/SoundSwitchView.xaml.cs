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
      if (sender is ListViewItem)
      {
        ListViewItem draggedItem = sender as ListViewItem;
        DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
        draggedItem.IsSelected = true;
      }
    }

    void listbox1_Drop(object sender, DragEventArgs e)
    {
      var itemSource = (ObservableCollection<BlankSoundDevice>)KnowDevicesListView.ItemsSource;

      BlankSoundDevice droppedData = e.Data.GetData(typeof(BlankSoundDevice)) as BlankSoundDevice;
      BlankSoundDevice target = ((ListViewItem)(sender)).DataContext as BlankSoundDevice;

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
