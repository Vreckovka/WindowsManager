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
using WindowsManager.ViewModels.TurnOff;
using WindowsManager.Windows;
using VCore;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using VCore.WPF.ViewModels.Navigation;

namespace WindowsManager.ViewModels
{
  //TODO: Monitor group - linkovat monitory (Predator ak bol aktivny tak nevyplne ten 19')

  public class WindowManagerMainWindowViewModel : BaseMainWindowViewModel
  {
    #region Constructors

    public WindowManagerMainWindowViewModel(
      ScreensManagementViewModel screensManagementViewModel,
      TurnOffViewModel turnOffViewModel,
      SoundManagerViewModel soundManagerViewModel)
    {
      ScreensManagementViewModel = screensManagementViewModel;
      TurnOffViewModel = turnOffViewModel;
      SoundManagerViewModel = soundManagerViewModel;
    }

    #endregion

    #region Properties

    public override string Title => "Window manager";
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

    public override void Initialize()
    {
      base.Initialize();

      CreateMenu();
    }

    private void CreateMenu()
    {
      var monitorsMenuVm = new NavigationItem(new MenuViewModel("Monitors"));
      monitorsMenuVm.IconPathData = "M512 0H64C28.65 0 0 28.65 0 64v288c0 35.35 28.65 64 64 64h148.3l-9.6 48H152C138.8 464 128 474.8 128 488S138.8 512 152 512h272c13.25 0 24-10.75 24-24s-10.75-24-24-24h-50.73L363.7 416H512c35.35 0 64-28.65 64-64V64C576 28.65 547.3 0 512 0zM324.3 464H251.7L261.3 416h53.46L324.3 464zM528 352c0 8.822-7.178 16-16 16H64c-8.822 0-16-7.178-16-16V64c0-8.822 7.178-16 16-16h448c8.822 0 16 7.178 16 16V352z";


      var screenMng = new NavigationItem(ScreensManagementViewModel);
      screenMng.IconPathData = "M160 144c-17.67 0-32 14.33-32 32c0 17.67 14.33 32 32 32s32-14.33 32-32C192 158.3 177.7 144 160 144zM307.1 198.8L290.5 189.2c.5-4.469 .7344-8.781 .7344-12.97c0-4.375-.25-8.781-.75-13.31l16.72-9.656C317.6 147.1 322.5 134.5 318.7 123.2C311.5 101.5 300.3 81.94 285.1 64.88C277.4 56.16 263.4 54.06 253.4 59.94L236.1 69.91c-7-5.031-14.42-9.312-22.23-12.84V37.19c0-12-8.422-22.53-20.12-25.03c-22.5-4.75-44.86-4.75-67.52 0c-11.62 2.5-20.06 13.03-20.06 25.03v19.88C98.34 60.59 90.91 64.88 83.92 69.91L66.7 59.97c-10.19-5.938-24.17-3.75-31.89 4.969c-15.06 17-26.36 36.59-33.53 58.22C-2.5 134.5 2.359 147.1 12.86 153.2l16.69 9.656c-.5 4.531-.75 8.938-.75 13.31c0 4.188 .2344 8.5 .7344 12.97L12.91 198.8C2.359 204.8-2.531 217.4 1.266 228.8c7.203 21.66 18.48 41.25 33.47 58.13c7.688 8.844 21.83 10.94 31.91 5.156l16.98-9.812c7.047 5.094 14.58 9.438 22.52 13.03v19.5c0 12 8.438 22.53 20.14 25.03C137.5 342.2 148.9 343.4 160 343.4s22.47-1.188 33.8-3.562c11.62-2.5 20.05-13.03 20.05-25.03v-19.5c7.938-3.594 15.47-7.938 22.52-13.03l16.94 9.75C263.5 297.1 277.5 295.8 285.2 287.1c15.08-17 26.36-36.59 33.55-58.22C322.5 217.5 317.7 204.8 307.1 198.8zM248 176c0 11.08-2.273 21.57-6.01 31.34l34.54 19.94c-3.719 8.5-8.344 16.53-13.84 24L227.1 231.2c-13.36 16.41-32.3 27.89-54.14 31.36v39.99c-9.281 1.125-18.41 1.125-27.69 0V262.6c-21.84-3.473-40.78-14.95-54.14-31.36L57.31 251.3c-5.469-7.469-10.09-15.5-13.84-24l34.54-19.94C74.27 197.6 72 187.1 72 176s2.273-21.57 6.01-31.34l-34.54-19.94c3.719-8.5 8.344-16.53 13.84-24l34.71 20.04c13.36-16.41 32.3-27.89 54.14-31.36V49.41c9.312-1.125 18.44-1.125 27.69 0v39.99c21.84 3.475 40.79 14.95 54.15 31.37l34.73-20.04c5.469 7.5 10.09 15.53 13.81 24l-34.54 19.94C245.7 154.4 248 164.9 248 176zM463.9 320c-17.67 0-32 14.33-32 32s14.33 32 32 32c17.67 0 32-14.33 32-32S481.6 320 463.9 320zM627.8 318.3c-2.5-11.62-13.03-20.11-25.03-20.11H582.9c-3.531-7.797-7.812-15.25-12.84-22.23l9.969-17.27c5.938-10.19 3.711-24.12-5.008-31.84c-17-15.06-36.59-26.36-58.22-33.53c-11.34-3.781-23.98 1.07-30.07 11.57l-9.641 16.69c-4.531-.5-8.945-.7393-13.32-.7393c-4.188 0-8.492 .2314-12.96 .7314L441.2 204.9c-6.031-10.55-18.66-15.38-30.04-11.59c-21.66 7.203-41.34 18.53-58.21 33.52c-8.844 7.688-10.84 21.78-5.062 31.86l9.812 16.98c-5.094 7.047-9.445 14.59-13.04 22.52h-19.5c-12 0-22.52 8.43-25.02 20.13c-2.375 11.25-3.57 22.59-3.57 33.72s1.188 22.41 3.562 33.74c2.5 11.62 13.03 20.1 25.03 20.1h19.5c3.594 7.938 7.945 15.48 13.04 22.52l-9.797 16.98c-5.938 10.2-3.688 24.17 5 31.81c17 15.08 36.6 26.37 58.23 33.55c11.38 3.797 24.02-1.039 30.05-11.55l9.641-16.7c4.469 .5 8.773 .7363 12.96 .7363c4.375 0 8.789-.2598 13.32-.7598l9.656 16.73c6.094 10.47 18.71 15.31 30.02 11.55c21.66-7.188 41.19-18.44 58.25-33.55c8.719-7.766 10.88-21.81 5.008-31.83l-9.969-17.27c5.031-7 9.312-14.43 12.84-22.24h19.87c12 0 22.52-8.421 25.02-20.12C632.6 363.2 632.6 340.9 627.8 318.3zM590.6 365.8h-39.99c-3.475 21.84-14.95 40.79-31.37 54.14l20.04 34.73c-7.5 5.469-15.53 10.09-24 13.81l-19.94-34.54C485.6 437.7 475.1 440 464 440s-21.57-2.273-31.34-6.01l-19.94 34.54c-8.5-3.719-16.53-8.344-24-13.84l20.04-34.71c-16.41-13.36-27.89-32.3-31.36-54.14h-39.99c-1.125-9.281-1.125-18.41 0-27.69h39.99c3.473-21.84 14.95-40.78 31.36-54.14l-20.04-34.71c7.469-5.469 15.5-10.09 24-13.84l19.94 34.54C442.4 266.3 452.9 264 464 264s21.57 2.273 31.34 6.01l19.94-34.54c8.5 3.719 16.53 8.344 24 13.84l-20.04 34.71c16.41 13.36 27.89 32.3 31.36 54.14h39.99C591.7 347.5 591.7 356.6 590.6 365.8z";
     
      var turnOff = new NavigationItem(TurnOffViewModel);
      turnOff.IconPathData = "M32 256C14.33 256 0 270.3 0 288c0 17.67 14.33 32 32 32s32-14.33 32-32C64 270.3 49.67 256 32 256zM84.35 446.4c-12.5 12.5-12.5 32.76 0 45.26c12.5 12.5 32.76 12.5 45.26 0c12.5-12.5 12.5-32.76 0-45.26C117.1 433.9 96.85 433.9 84.35 446.4zM129.6 129.6c12.5-12.5 12.5-32.76 0-45.25c-12.5-12.5-32.76-12.5-45.26 0c-12.5 12.5-12.5 32.76 0 45.25C96.85 142.1 117.1 142.1 129.6 129.6zM288 64c17.67 0 32-14.33 32-32c0-17.67-14.33-32-32-32C270.3 0 256 14.33 256 32C256 49.67 270.3 64 288 64zM446.4 446.4c-12.5 12.5-12.5 32.76 0 45.26c12.5 12.5 32.76 12.5 45.26 0c12.5-12.5 12.5-32.76 0-45.26S458.9 433.9 446.4 446.4zM544 256c-17.67 0-32 14.33-32 32c0 17.67 14.33 32 32 32s32-14.33 32-32C576 270.3 561.7 256 544 256zM446.4 84.35c-12.5 12.5-12.5 32.76 0 45.25c12.5 12.5 32.76 12.5 45.26 0c12.5-12.5 12.5-32.76 0-45.25C479.2 71.86 458.9 71.86 446.4 84.35zM287.1 127.1C199.6 128 127.1 199.6 127.1 288s71.63 160 159.1 159.1c88.37 .0014 160-71.63 160-159.1S376.4 128 287.1 127.1zM399.1 288c0 53.5-37.77 98.23-88 109.3V288c0-13.25-10.75-24-24-24c-13.25 0-24 10.75-24 24v109.3c-50.24-11.03-87.1-55.76-88-109.3c0-61.76 50.24-112 112-112C349.8 175.1 400 226.2 399.1 288z";

      monitorsMenuVm.SubItems.Add(screenMng);
      monitorsMenuVm.SubItems.Add(turnOff);


      var soundMenuItem = new NavigationItem(new MenuViewModel("Sound"));
      soundMenuItem.IconPathData = "M333.2 34.84c-4.201-1.896-8.729-2.841-13.16-2.841c-7.697 0-15.29 2.784-21.27 8.1L163.8 160H80c-26.51 0-48 21.49-48 47.1v95.1c0 26.51 21.49 47.1 48 47.1h83.84l134.9 119.9C304.7 477.2 312.3 480 320 480c4.438 0 8.959-.9312 13.16-2.837C344.7 472 352 460.6 352 448V64C352 51.41 344.7 39.1 333.2 34.84zM304 412.4L182.1 304H80v-95.1h102.1L304 99.64V412.4zM444.6 181.9c-4.471-3.629-9.857-5.401-15.2-5.401c-6.949 0-13.83 2.994-18.55 8.807c-8.406 10.25-6.906 25.37 3.375 33.78C425.5 228.4 432 241.8 432 256s-6.5 27.62-17.81 36.87c-10.28 8.406-11.78 23.53-3.375 33.78c4.719 5.812 11.62 8.812 18.56 8.812c5.344 0 10.75-1.781 15.19-5.406C467.1 311.6 480 284.7 480 256C480 227.3 467.1 200.4 444.6 181.9zM505.1 108c-4.455-3.637-9.842-5.417-15.2-5.417c-6.934 0-13.82 2.979-18.58 8.761c-8.406 10.25-6.906 25.37 3.344 33.78C508.6 172.9 528 213.3 528 256c0 42.69-19.44 83.09-53.31 110.9c-10.25 8.406-11.75 23.53-3.344 33.78c4.75 5.781 11.62 8.781 18.56 8.781c5.375 0 10.75-1.781 15.22-5.437C550.2 367.1 576 313.1 576 256C576 198.9 550.2 144.9 505.1 108z";


      var sndMngr = new NavigationItem(SoundManagerViewModel);
      sndMngr.IconPathData = "M456 160c22.09 0 40-17.91 40-40S478.1 80 456 80S416 97.91 416 120S433.9 160 456 160zM576 0h-240c-35.35 0-64 28.65-64 64v384c0 35.35 28.65 64 64 64H576c35.35 0 64-28.65 64-64V64C640 28.65 611.3 0 576 0zM592 448c0 8.822-7.178 16-16 16h-240c-8.822 0-16-7.178-16-16V64c0-8.822 7.178-16 16-16H576c8.822 0 16 7.178 16 16V448zM456 224C398.6 224 352 270.6 352 328s46.56 104 104 104s104-46.56 104-104S513.4 224 456 224zM456 384c-30.88 0-56-25.12-56-56c0-30.88 25.12-56 56-56S512 297.1 512 328C512 358.9 486.9 384 456 384zM184 80C161.9 80 144 97.91 144 120S161.9 160 184 160S224 142.1 224 120S206.1 80 184 80zM184 272C199.7 272 213.8 278.5 224 288.9V232.3C211.7 227.1 198.2 224 184 224C126.6 224 80 270.6 80 328s46.56 104 104 104c14.24 0 27.67-3.115 40-8.346v-56.58C213.8 377.5 199.7 384 184 384C153.1 384 128 358.9 128 328C128 297.1 153.1 272 184 272zM264.1 0H64C28.65 0 0 28.65 0 64v384c0 35.35 28.65 64 64 64h200.1c-11.94-13.24-20.25-29.67-23.35-48H64c-8.822 0-16-7.178-16-16V64c0-8.822 7.178-16 16-16h177.6C244.7 29.67 253 13.24 264.1 0z";

      soundMenuItem.SubItems.Add(sndMngr);


      MainMenu.Items.Add(monitorsMenuVm);
      MainMenu.Items.Add(soundMenuItem);

      monitorsMenuVm.SubItems[0].IsActive = true;

    }

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
