using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace WindowsManager.ViewModels.ScreenManagement
{
  public class BrightnessController : IDisposable
  {
    [DllImport("user32.dll", EntryPoint = "MonitorFromWindow")]
    public static extern IntPtr MonitorFromWindow([In] IntPtr hwnd, uint dwFlags);

    [DllImport("dxva2.dll", EntryPoint = "DestroyPhysicalMonitors")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyPhysicalMonitors(uint dwPhysicalMonitorArraySize, [In] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

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

    private readonly object hardwareGate = new object();
    private readonly object requestGate = new object();
    private int? pendingBrightness;
    private bool workerRunning;
    private bool disposeRequested;
    private Task workerTask = Task.CompletedTask;
    public Task PendingWork { get { lock (requestGate) return workerTask; } }

    public int? Initilize(IntPtr ptr)
    {
      lock (hardwareGate)
      {
        lock (requestGate) if (disposeRequested) return null;
        if (ptr == IntPtr.Zero ||
            !GetNumberOfPhysicalMonitorsFromHMONITOR(ptr, ref _physicalMonitorsCount) ||
            _physicalMonitorsCount == 0)
          return null;

        _physicalMonitorArray = new PHYSICAL_MONITOR[_physicalMonitorsCount];
        if (!GetPhysicalMonitorsFromHMONITOR(ptr, _physicalMonitorsCount, _physicalMonitorArray))
        {
          _physicalMonitorsCount = 0;
          return null;
        }
        MonitorHandle = _physicalMonitorArray[0].hPhysicalMonitor;
        if (!GetMonitorBrightness(MonitorHandle, ref _minValue, ref _currentValue, ref _maxValue))
          return null;
        return _maxValue > _minValue
          ? (int)(100u * (_currentValue - _minValue) / (_maxValue - _minValue))
          : (int)_currentValue;
      }
    }

    // At most one worker per monitor. New requests replace a pending value;
    // a slow DDC/CI driver cannot block input or grow a queue of writes.
    public void RequestBrightness(int newValue)
    {
      lock (requestGate)
      {
        if (disposeRequested) return;
        pendingBrightness = Math.Max(0, Math.Min(100, newValue));
        StartWorker();
      }
    }

    private void StartWorker()
    {
      if (workerRunning) return;
      workerRunning = true;
      workerTask = Task.Run(ProcessRequests);
    }

    private void ProcessRequests()
    {
      while (true)
      {
        int? value;
        bool release;
        lock (requestGate)
        {
          value = pendingBrightness;
          pendingBrightness = null;
          release = disposeRequested;
          if (value == null && !release)
          {
            workerRunning = false;
            return;
          }
        }
        try
        {
          if (value != null) SetBrightness(value.Value);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is ExternalException)
        {
          System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
          if (release)
          {
            lock (hardwareGate)
            {
              if (_physicalMonitorsCount > 0 && _physicalMonitorArray != null)
                DestroyPhysicalMonitors(_physicalMonitorsCount, _physicalMonitorArray);
              _physicalMonitorsCount = 0;
              _physicalMonitorArray = null;
              MonitorHandle = IntPtr.Zero;
            }
          }
        }
        if (release) return;
      }
    }

    public virtual void SetBrightness(int newValue)
    {
      lock (hardwareGate)
      {
        if (MonitorHandle == IntPtr.Zero) return;
        newValue = Math.Max(0, Math.Min(100, newValue));
        _currentValue = (_maxValue - _minValue) * (uint)newValue / 100u + _minValue;
        SetMonitorBrightness(MonitorHandle, _currentValue);
      }
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!disposing) return;
      lock (requestGate)
      {
        if (disposeRequested) return;
        disposeRequested = true;
        StartWorker();
      }
    }
  }
}
