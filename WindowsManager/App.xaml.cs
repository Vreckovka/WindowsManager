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
using VCore.Standard.Modularity.NinjectModules;
using VCore.WPF;
using VCore.WPF.Views.SplashScreen;
using VPlayer.AudioStorage.Modularity.NinjectModules;
using VPlayer.Core.Managers.Status;

namespace WindowsManager
{

  public class WindowsManagerApp : VApplication<MainWindow, WindowManagerMainWindowViewModel, SplashScreenView>
  {
    private Mutex singleInstanceMutex;

    protected override void LoadModules()
    {
      base.LoadModules();

      Kernel.Bind<ScreensManagementViewModel>().ToSelf().InSingletonScope();
      Kernel.Bind<SoundManagerViewModel>().ToSelf().InSingletonScope();

      Kernel.Bind<IStatusManager>().To<BaseStatusManager>();
      Kernel.Load<AudioStorageNinjectModule>();
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


  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : WindowsManagerApp
  {

  }
}
