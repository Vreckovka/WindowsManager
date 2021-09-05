using System;
using System.Runtime.InteropServices;

namespace WindowsManager.ViewModels.ScreenManagement
{
  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
  public struct PHYSICAL_MONITOR
  {
    public IntPtr hPhysicalMonitor;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string szPhysicalMonitorDescription;
  }
}