using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WindowsManager.Windows
{
  /// <summary>
  /// Interaction logic for DimmerWindow.xaml
  /// </summary>
  public partial class DimmerWindow : Window
  {
    public DimmerWindow()
    {
      InitializeComponent();
    }

    private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ClickCount == 2)
      {
        Close();
      }
    }
  }
}
