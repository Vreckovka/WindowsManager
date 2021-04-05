using System;
using System.Runtime.InteropServices;

namespace WindowsManager.ViewModels
{
  public class BrightnessController : IDisposable
  {
    [DllImport("user32.dll", EntryPoint = "MonitorFromWindow")]
    public static extern IntPtr MonitorFromWindow([In] IntPtr hwnd, uint dwFlags);

    [DllImport("dxva2.dll", EntryPoint = "DestroyPhysicalMonitors")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyPhysicalMonitors(uint dwPhysicalMonitorArraySize, ref PHYSICAL_MONITOR[] pPhysicalMonitorArray);

    [DllImport("dxva2.dll", EntryPoint = "GetNumberOfPhysicalMonitorsFromHMONITOR")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, ref uint pdwNumberOfPhysicalMonitors);

    [DllImport("dxva2.dll", EntryPoint = "GetPhysicalMonitorsFromHMONITOR")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint dwPhysicalMonitorArraySize, [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

    [DllImport("dxva2.dll", EntryPoint = "GetMonitorBrightness")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetMonitorBrightness(IntPtr handle, ref uint minimumBrightness, ref uint currentBrightness, ref uint maxBrightness);

    [DllImport("dxva2.dll", EntryPoint = "SetMonitorBrightness")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetMonitorBrightness(IntPtr handle, uint newBrightness);

    private uint _physicalMonitorsCount = 0;
    private PHYSICAL_MONITOR[] _physicalMonitorArray;

    public IntPtr MonitorHandle;

    private uint _minValue = 0;
    private uint _maxValue = 0;
    private uint _currentValue = 0;

    public int? Initilize(IntPtr ptr)
    {
      uint dwFlags = 0u;

      if (!GetNumberOfPhysicalMonitorsFromHMONITOR(ptr, ref _physicalMonitorsCount))
      {
        return null;
      }
      _physicalMonitorArray = new PHYSICAL_MONITOR[_physicalMonitorsCount];

      if (!GetPhysicalMonitorsFromHMONITOR(ptr, _physicalMonitorsCount, _physicalMonitorArray))
      {
        return null;
      }

      MonitorHandle = _physicalMonitorArray[0].hPhysicalMonitor;

      if (!GetMonitorBrightness(MonitorHandle, ref _minValue, ref _currentValue, ref _maxValue))
      {
        return null;
      }

      return (int)_currentValue;
    }

    public void SetBrightness(int newValue) // 0 ~ 100
    {
      newValue = Math.Min(newValue, Math.Max(0, newValue));
      _currentValue = (_maxValue - _minValue) * (uint)newValue / 100u + _minValue;
      SetMonitorBrightness(MonitorHandle, _currentValue);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (disposing)
      {
        if (_physicalMonitorsCount > 0)
        {
          DestroyPhysicalMonitors(_physicalMonitorsCount, ref _physicalMonitorArray);
        }
      }
    }
  }
}