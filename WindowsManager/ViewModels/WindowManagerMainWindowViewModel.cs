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
using WindowsManager.ViewModels.ProcessManagement;
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
    private readonly ProcessesViewModel processesViewModel;

    #region Constructors

    public WindowManagerMainWindowViewModel(
      ScreensManagementViewModel screensManagementViewModel,
      TurnOffViewModel turnOffViewModel,
      SoundManagerViewModel soundManagerViewModel,
      HomeViewModel homeViewModel,
      TorrentsViewModel torrentsViewModel,
      ProcessesViewModel processesViewModel,
      IViewModelsFactory viewModelsFactory) : base(viewModelsFactory)
    {
      this.homeViewModel = homeViewModel ?? throw new ArgumentNullException(nameof(homeViewModel));
      this.torrentsViewModel = torrentsViewModel ?? throw new ArgumentNullException(nameof(torrentsViewModel));
      this.processesViewModel = processesViewModel ?? throw new ArgumentNullException(nameof(processesViewModel));
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

      var processes = new NavigationItem(processesViewModel);
      processes.IconPathData = "M314.6 216.6L285.6 199.9C287.1 192.1 288 184.2 288 176.1C288 167.8 287.1 159.8 285.6 151.1L314.6 135.2c4.293-2.48 6.445-7.695 4.883-12.4C312.2 100.1 300.6 81.11 285.7 64.28C283.7 62.03 280.9 60.87 278 60.87c-1.861 0-3.736 .4785-5.42 1.449L243 79.41C231 69.11 217.3 61.07 202.1 55.75V21.65c0-4.943-3.418-9.348-8.258-10.36C182.9 9.002 171.6 7.67 160 7.67c-11.61 0-22.88 1.342-33.81 3.632c-4.84 1.016-8.248 5.41-8.248 10.35v34.09C102.7 61.07 88.96 69.11 76.98 79.41L47.39 62.32C45.71 61.35 43.83 60.87 41.97 60.87c-2.893 0-5.648 1.169-7.652 3.427C19.39 81.12 7.758 100.1 .5078 122.8C-1.053 127.5 1.098 132.7 5.391 135.2l29.04 16.77C32.93 159.8 32 167.8 32 176.1c0 8.137 .9434 16.04 2.395 23.75L5.391 216.6C1.098 219.1-1.053 224.3 .5078 228.1c7.25 21.83 18.79 41.69 33.71 58.52C36.22 289.8 39.08 290.9 41.97 290.9c1.861 0 3.738-.4785 5.42-1.449L76.7 272.6c12.04 10.41 25.91 18.53 41.24 23.89v33.69c0 4.941 3.419 9.279 8.258 10.29C137.1 342.7 148.4 344.1 160 344.1c11.61 0 22.88-1.411 33.81-3.7c4.84-1.016 8.247-5.343 8.247-10.28V296.5c15.34-5.365 29.2-13.49 41.24-23.9L272.6 289.5c1.68 .9707 3.559 1.449 5.42 1.449c2.891 0 5.646-1.238 7.652-3.498c14.92-16.83 26.56-36.6 33.81-58.44C321.1 224.3 318.9 219.1 314.6 216.6zM160 224.1c-26.51 0-48-21.49-48-48s21.49-48 48-48s48 21.49 48 48S186.5 224.1 160 224.1zM628.5 318.2c-1.016-4.84-5.412-8.248-10.36-8.248h-34.09c-5.324-15.22-13.36-28.98-23.66-40.96l17.09-29.6c.9707-1.68 1.449-3.559 1.449-5.42c0-2.893-1.167-5.648-3.425-7.652c-16.83-14.92-36.67-26.56-58.51-33.81c-4.703-1.561-9.918 .5898-12.4 4.883l-16.77 29.04C479.1 224.9 471.1 224 463.7 224c-8.137 0-16.04 .9434-23.75 2.395L423.2 197.4c-2.48-4.293-7.699-6.443-12.4-4.883c-21.83 7.25-41.69 18.79-58.52 33.71c-2.26 2.004-3.419 4.857-3.419 7.748c0 1.861 .4795 3.738 1.45 5.42l16.92 29.31c-10.41 12.04-18.53 25.91-23.89 41.24H309.6c-4.941 0-9.496 3.393-10.51 8.232C296.8 329.1 295.7 340.4 295.7 352c0 11.61 1.184 22.9 3.473 33.82C300.1 390.7 304.7 394.1 309.6 394.1h33.69c5.365 15.34 13.49 29.2 23.9 41.24l-16.92 29.31c-.9707 1.68-1.45 3.559-1.45 5.42c0 2.891 1.044 5.742 3.304 7.748c16.83 14.92 36.8 26.46 58.63 33.71c4.703 1.562 9.922-.5898 12.4-4.883l16.74-29C447.6 479.1 455.5 480 463.7 480c8.268 0 16.3-.9336 24.13-2.432l16.77 29.04c2.48 4.293 7.695 6.445 12.4 4.883c21.84-7.25 41.69-18.9 58.52-33.82c2.258-2.006 3.414-4.751 3.414-7.642c0-1.861-.4785-3.736-1.449-5.42l-17.09-29.6c10.29-11.98 18.34-25.74 23.66-40.96h34.09c4.943 0 9.35-3.418 10.37-8.258C630.8 374.9 632.1 363.6 632.1 352C632.1 340.4 630.8 329.1 628.5 318.2zM463.7 400c-26.51 0-48-21.49-48-48s21.49-48 48-48s48 21.49 48 48S490.2 400 463.7 400z";

      var sndMngr = new NavigationItem(SoundManagerViewModel);
      sndMngr.IconPathData = "M456 160c22.09 0 40-17.91 40-40S478.1 80 456 80S416 97.91 416 120S433.9 160 456 160zM576 0h-240c-35.35 0-64 28.65-64 64v384c0 35.35 28.65 64 64 64H576c35.35 0 64-28.65 64-64V64C640 28.65 611.3 0 576 0zM592 448c0 8.822-7.178 16-16 16h-240c-8.822 0-16-7.178-16-16V64c0-8.822 7.178-16 16-16H576c8.822 0 16 7.178 16 16V448zM456 224C398.6 224 352 270.6 352 328s46.56 104 104 104s104-46.56 104-104S513.4 224 456 224zM456 384c-30.88 0-56-25.12-56-56c0-30.88 25.12-56 56-56S512 297.1 512 328C512 358.9 486.9 384 456 384zM184 80C161.9 80 144 97.91 144 120S161.9 160 184 160S224 142.1 224 120S206.1 80 184 80zM184 272C199.7 272 213.8 278.5 224 288.9V232.3C211.7 227.1 198.2 224 184 224C126.6 224 80 270.6 80 328s46.56 104 104 104c14.24 0 27.67-3.115 40-8.346v-56.58C213.8 377.5 199.7 384 184 384C153.1 384 128 358.9 128 328C128 297.1 153.1 272 184 272zM264.1 0H64C28.65 0 0 28.65 0 64v384c0 35.35 28.65 64 64 64h200.1c-11.94-13.24-20.25-29.67-23.35-48H64c-8.822 0-16-7.178-16-16V64c0-8.822 7.178-16 16-16h177.6C244.7 29.67 253 13.24 264.1 0z";

      var torrentsMenuVm = new NavigationItem(torrentsViewModel);
      torrentsMenuVm.ImagePath = "utorrent_logo.png";

      var ruleManagerViewModelMenuVm = new NavigationItem(ViewModelsFactory.Create<RuleManagerViewModel>());
      torrentsMenuVm.ImagePath = "utorrent_logo.png";

      MainMenu.Items.Add(homeMenuVm);
      MainMenu.Items.Add(turnOff);
      MainMenu.Items.Add(processes);

      MainMenu.Items.Add(monitorsMenuVm);
      MainMenu.Items.Add(sndMngr);
    
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
