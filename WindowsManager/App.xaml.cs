using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WindowsManager.ViewModels;
using Ninject;
using Prism.Ioc;
using Prism.Ninject;
using Prism.Regions;
using VCore.Modularity.NinjectModules;
using VCore.Standard.Modularity.NinjectModules;

namespace WindowsManager
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : PrismApplication
  {
    private IKernel Kernel;
    private Mutex singleInstanceMutex;
    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
      Kernel = Container.GetContainer();

      Kernel.Load<CommonNinjectModule>();
      Kernel.Load<WPFNinjectModule>();
    }

    protected override Window CreateShell()
    {
      var shell = Container.Resolve<MainWindow>();

      RegionManager.SetRegionManager(shell, Kernel.Get<IRegionManager>());
      RegionManager.UpdateRegions();

      var dataContext = Container.Resolve<WindowManagerMainWindowViewModel>();
      shell.DataContext = dataContext;

      return shell;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);

#if !DEBUG
      bool aIsNewInstance = false;
      singleInstanceMutex = new Mutex(true, "WindowsManager", out aIsNewInstance);
      if (!aIsNewInstance)
      {
        MessageBox.Show("Already an instance is running...");
        App.Current.Shutdown();
      }
#endif

    }
  }
}
