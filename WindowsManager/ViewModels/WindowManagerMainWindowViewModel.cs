using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using WindowsManager.ViewModels.Home;
using WindowsManager.ViewModels.ScreenManagement;
using WindowsManager.ViewModels.ScreenManagement.Rules;
using WindowsManager.ViewModels.Torrents;
using WindowsManager.ViewModels.TurnOff;
using WindowsManager.Windows;
using TorrentAPI;
using VCore;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.Misc;
using VCore.WPF.Other;
using VCore.WPF.ViewModels;
using VCore.WPF.ViewModels.Navigation;

namespace WindowsManager.ViewModels
{
  //TODO: Monitor group - linkovat monitory (Predator ak bol aktivny tak nevyplne ten 19')
  //TODO: Fast panel (horiaci tachometer iconka) 
  public class WindowManagerMainWindowViewModel : BaseMainWindowViewModel
  {
    private readonly HomeViewModel homeViewModel;
    private readonly TorrentsViewModel torrentsViewModel;

    #region Constructors

    public WindowManagerMainWindowViewModel(
      ScreensManagementViewModel screensManagementViewModel,
      TurnOffViewModel turnOffViewModel,
      SoundManagerViewModel soundManagerViewModel,
      HomeViewModel homeViewModel,
      TorrentsViewModel torrentsViewModel,
      IViewModelsFactory viewModelsFactory) : base(viewModelsFactory)
    {
      this.homeViewModel = homeViewModel ?? throw new ArgumentNullException(nameof(homeViewModel));
      this.torrentsViewModel = torrentsViewModel ?? throw new ArgumentNullException(nameof(torrentsViewModel));
      ScreensManagementViewModel = screensManagementViewModel;
      TurnOffViewModel = turnOffViewModel;
      SoundManagerViewModel = soundManagerViewModel;
    }

    #endregion

    #region Properties

    public override string Title => "WindowManager";
    public ScreensManagementViewModel ScreensManagementViewModel { get; set; }
    public TurnOffViewModel TurnOffViewModel { get; set; }

    public SoundManagerViewModel SoundManagerViewModel { get; set; }

    public NavigationViewModel MainMenu { get; set; } = new NavigationViewModel();

    #endregion

    #region Commands

    public ICommand SwitchBehaviorCommand { get; set; }

    #endregion

    #region Methods

    #region OnClose

    protected override void OnClose(Window window)
    {
      window.WindowState = WindowState.Minimized;
    }

    #endregion

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      CreateMenu();
    }

    #endregion

    #region CreateMenu

    private void CreateMenu()
    {
      var homeMenuVm = new NavigationItem(homeViewModel);
      homeMenuVm.IconPathData = IconPaths.Home;

      var monitorsMenuVm = new NavigationItem(ScreensManagementViewModel);
      monitorsMenuVm.IconPathData = "M512 0H64C28.65 0 0 28.65 0 64v288c0 35.35 28.65 64 64 64h148.3l-9.6 48H152C138.8 464 128 474.8 128 488S138.8 512 152 512h272c13.25 0 24-10.75 24-24s-10.75-24-24-24h-50.73L363.7 416H512c35.35 0 64-28.65 64-64V64C576 28.65 547.3 0 512 0zM324.3 464H251.7L261.3 416h53.46L324.3 464zM528 352c0 8.822-7.178 16-16 16H64c-8.822 0-16-7.178-16-16V64c0-8.822 7.178-16 16-16h448c8.822 0 16 7.178 16 16V352z";

      var turnOff = new NavigationItem(TurnOffViewModel);
      turnOff.IconPathData = "M32 256C14.33 256 0 270.3 0 288c0 17.67 14.33 32 32 32s32-14.33 32-32C64 270.3 49.67 256 32 256zM84.35 446.4c-12.5 12.5-12.5 32.76 0 45.26c12.5 12.5 32.76 12.5 45.26 0c12.5-12.5 12.5-32.76 0-45.26C117.1 433.9 96.85 433.9 84.35 446.4zM129.6 129.6c12.5-12.5 12.5-32.76 0-45.25c-12.5-12.5-32.76-12.5-45.26 0c-12.5 12.5-12.5 32.76 0 45.25C96.85 142.1 117.1 142.1 129.6 129.6zM288 64c17.67 0 32-14.33 32-32c0-17.67-14.33-32-32-32C270.3 0 256 14.33 256 32C256 49.67 270.3 64 288 64zM446.4 446.4c-12.5 12.5-12.5 32.76 0 45.26c12.5 12.5 32.76 12.5 45.26 0c12.5-12.5 12.5-32.76 0-45.26S458.9 433.9 446.4 446.4zM544 256c-17.67 0-32 14.33-32 32c0 17.67 14.33 32 32 32s32-14.33 32-32C576 270.3 561.7 256 544 256zM446.4 84.35c-12.5 12.5-12.5 32.76 0 45.25c12.5 12.5 32.76 12.5 45.26 0c12.5-12.5 12.5-32.76 0-45.25C479.2 71.86 458.9 71.86 446.4 84.35zM287.1 127.1C199.6 128 127.1 199.6 127.1 288s71.63 160 159.1 159.1c88.37 .0014 160-71.63 160-159.1S376.4 128 287.1 127.1zM399.1 288c0 53.5-37.77 98.23-88 109.3V288c0-13.25-10.75-24-24-24c-13.25 0-24 10.75-24 24v109.3c-50.24-11.03-87.1-55.76-88-109.3c0-61.76 50.24-112 112-112C349.8 175.1 400 226.2 399.1 288z";

      var sndMngr = new NavigationItem(SoundManagerViewModel);
      sndMngr.IconPathData = "M456 160c22.09 0 40-17.91 40-40S478.1 80 456 80S416 97.91 416 120S433.9 160 456 160zM576 0h-240c-35.35 0-64 28.65-64 64v384c0 35.35 28.65 64 64 64H576c35.35 0 64-28.65 64-64V64C640 28.65 611.3 0 576 0zM592 448c0 8.822-7.178 16-16 16h-240c-8.822 0-16-7.178-16-16V64c0-8.822 7.178-16 16-16H576c8.822 0 16 7.178 16 16V448zM456 224C398.6 224 352 270.6 352 328s46.56 104 104 104s104-46.56 104-104S513.4 224 456 224zM456 384c-30.88 0-56-25.12-56-56c0-30.88 25.12-56 56-56S512 297.1 512 328C512 358.9 486.9 384 456 384zM184 80C161.9 80 144 97.91 144 120S161.9 160 184 160S224 142.1 224 120S206.1 80 184 80zM184 272C199.7 272 213.8 278.5 224 288.9V232.3C211.7 227.1 198.2 224 184 224C126.6 224 80 270.6 80 328s46.56 104 104 104c14.24 0 27.67-3.115 40-8.346v-56.58C213.8 377.5 199.7 384 184 384C153.1 384 128 358.9 128 328C128 297.1 153.1 272 184 272zM264.1 0H64C28.65 0 0 28.65 0 64v384c0 35.35 28.65 64 64 64h200.1c-11.94-13.24-20.25-29.67-23.35-48H64c-8.822 0-16-7.178-16-16V64c0-8.822 7.178-16 16-16h177.6C244.7 29.67 253 13.24 264.1 0z";

      var torrentsMenuVm = new NavigationItem(torrentsViewModel);
      torrentsMenuVm.ImagePath = "utorrent_logo.png";

      var ruleManagerViewModelMenuVm = new NavigationItem(ViewModelsFactory.Create<RuleManagerViewModel>());
      torrentsMenuVm.ImagePath = "utorrent_logo.png";

      MainMenu.Items.Add(homeMenuVm);
      MainMenu.Items.Add(monitorsMenuVm);
      MainMenu.Items.Add(sndMngr);
      MainMenu.Items.Add(turnOff);
      MainMenu.Items.Add(torrentsMenuVm);
      MainMenu.Items.Add(ruleManagerViewModelMenuVm);

      homeMenuVm.IsActive = true;

    } 

    #endregion

    #region SwitchScreenCommand

    private ActionCommand switchScreenCommand;

    public ICommand SwitchScreenCommand
    {
      get
      {
        return switchScreenCommand ??= new ActionCommand(SwitchScreen);
      }
    }


    private void SwitchScreen()
    {
      SwitchBehaviorCommand?.Execute(null);


    }

    #endregion

    #region OnWindowStateChanged

    private WindowState lastState;
    protected override void OnWindowStateChanged(WindowState windowState)
    {
      if (lastState == WindowState.Minimized && windowState == WindowState.Normal)
      {
        SwitchScreen();
      }

      lastState = windowState;
    }

    #endregion

    #endregion
  }
}
