using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace WindowsManager.Views.Behaviors
{
  public class ContainerSliderValueBehavior : Behavior<FrameworkElement>
  {
    #region Slider

    public static readonly DependencyProperty SliderProperty =
      DependencyProperty.Register(
        nameof(Slider),
        typeof(Slider),
        typeof(ContainerSliderValueBehavior),
        new PropertyMetadata(null));

    public Slider Slider
    {
      get { return (Slider)GetValue(SliderProperty); }
      set { SetValue(SliderProperty, value); }
    }

    #endregion

    public int ValueChangeSize { get; set; } = 1;

    protected override void OnAttached()
    {
      base.OnAttached();

      AssociatedObject.MouseWheel += AssociatedObject_MouseWheel;
    }

    private void AssociatedObject_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
      if (Slider != null)
      {
        if (e.Delta > 0)
        {
          Slider.Value += ValueChangeSize;
        }
        else
        {
          Slider.Value -= ValueChangeSize;
        }

        e.Handled = true;
      }
    }
  }
}
