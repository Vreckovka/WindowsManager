using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WindowsManager.ViewModels;
using WindowsManager.ViewModels.ScreenManagement;
using WindowsManager.ViewModels.ScreenManagement.Rules;
using WindowsManager.ViewModels.Torrents;
using WindowsManager.ViewModels.TurnOff;
using Logger;
using Ninject;
using Prism.Ioc;
using Prism.Ninject;
using Prism.Regions;
using SoundManagement;
using TorrentAPI;
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
      Kernel.Bind<RuleManagerViewModel>().ToSelf().InSingletonScope();
      Kernel.Bind<TorrentsViewModel>().ToSelf().InSingletonScope();
      Kernel.Bind<TurnOffViewModel>().ToSelf().InSingletonScope();

      Kernel.Bind<IRarbgApiClient>().To<RarbgApiClient>().InSingletonScope()
        .WithConstructorArgument("baseUrl", "https://torrentapi.org/pubapi_v2.php")
        .WithConstructorArgument("appID", Assembly.GetExecutingAssembly().GetName().Name);


      Kernel.Bind<ITorrentProvider>().To<X1337TorrentProvider>().InSingletonScope();
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
#if PERFORMANCE_TESTS
      if (e.Args.Length == 3 && e.Args[0] == "--performance-test")
        global::Program.Attach(this, e.Args[1], int.Parse(e.Args[2]));
#endif
    }

    protected override void OnExit(ExitEventArgs e)
    {
#if !PERFORMANCE_BASELINE
      // Windows have already closed when OnExit runs. Give the asynchronous
      // monitor restore and handle release a bounded chance to finish before
      // the process terminates its background threads.
      var screenManager = Kernel?.TryGet<ScreensManagementViewModel>();
      screenManager?.Dispose();
      if (screenManager != null)
      {
        try { Task.WaitAll(screenManager.Screens.Select(s => s.PendingBrightnessWork).ToArray(), 2000); }
        catch (AggregateException ex) { System.Diagnostics.Debug.WriteLine(ex); }
      }
#endif
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
