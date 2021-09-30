using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WindowsManager.ViewModels;
using WindowsManager.ViewModels.ScreenManagement;
using Logger;
using Ninject;
using Prism.Ioc;
using Prism.Ninject;
using Prism.Regions;
using SoundManagement;
using VCore.Modularity.NinjectModules;
using VCore.Standard.Modularity.NinjectModules;
using VPlayer.AudioStorage.Modularity.NinjectModules;
using VPlayer.Core.Managers.Status;

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
      Kernel.Bind<ILogger>().To<Logger.Logger>().InSingletonScope(); 
      Kernel.Bind<ILoggerContainer>().To<ConsoleLogger>().InSingletonScope();;
      Kernel.Bind<ScreensManagementViewModel>().ToSelf().InSingletonScope();

      Kernel.Bind<IStatusManager>().To<BaseStatusManager>();
      Kernel.Load<AudioStorageNinjectModule>();
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

    protected override void OnExit(ExitEventArgs e)
    {
      AudioDeviceManager.Instance.Dispose();

      base.OnExit(e);
    }
  }
}
