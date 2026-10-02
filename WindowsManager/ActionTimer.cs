using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Threading;
using VCore.Standard;

namespace WindowsManager
{
  // UI timers do not enqueue one dispatcher callback for every background tick
  // while a window or display driver is blocking the UI thread.
  public class ActionTimer : ViewModel
  {
    private readonly DispatcherTimer timer;
    private readonly Subject<long> subject = new Subject<long>();
    private readonly Stopwatch stopWatch = new Stopwatch();
    private long tick;
    private bool disposed;

    public ActionTimer() : this(TimeSpan.FromMilliseconds(100)) { }

    public ActionTimer(TimeSpan interval)
    {
      if (interval <= TimeSpan.Zero)
        throw new ArgumentOutOfRangeException(nameof(interval));

      timer = new DispatcherTimer(DispatcherPriority.Background,
        Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher);
      timer.Interval = interval;
      timer.Tick += OnInternalTick;
    }

    public IObservable<long> OnTimerTick => subject.AsObservable();
    public bool IsRunning => timer.IsEnabled;
    public TimeSpan Elapsed => stopWatch.Elapsed;

    private double? actualTime;
    public double? ActualTime
    {
      get => actualTime;
      private set
      {
        if (value != actualTime)
        {
          actualTime = value;
          RaisePropertyChanged();
        }
      }
    }

    public void StartTimer()
    {
      if (disposed) return;
      timer.Stop();
      stopWatch.Restart();
      tick = 0;
      ActualTime = 0;
      timer.Start();
    }

    public void StopTimer()
    {
      timer.Stop();
      stopWatch.Reset();
      ActualTime = null;
    }

    private void OnInternalTick(object sender, EventArgs e)
    {
      if (disposed || !timer.IsEnabled) return;
      ActualTime = stopWatch.Elapsed.TotalMilliseconds;
      subject.OnNext(tick++);
    }

    public override void Dispose()
    {
      if (disposed) return;
      disposed = true;
      StopTimer();
      timer.Tick -= OnInternalTick;
      subject.OnCompleted();
      subject.Dispose();
      base.Dispose();
    }
  }
}
