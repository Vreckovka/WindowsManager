using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using VCore.Standard;

namespace WindowsManager
{
  public class ActionTimer : ViewModel
  {
    private readonly TimeSpan interval;
    private SerialDisposable serialDisposable = new SerialDisposable();
    private Subject<long> subject = new Subject<long>();
    private Stopwatch stopWatch = new Stopwatch();

    public ActionTimer() : this(new TimeSpan())
    {
    }

    public ActionTimer(TimeSpan interval)
    {
      this.interval = interval;
    }

    public IObservable<long> OnTimerTick
    {
      get
      {
        return subject.AsObservable();
      }
    }

    #region ActualTime

    private double? actualTime;

    public double? ActualTime
    {
      get { return actualTime; }
      set
      {
        if (value != actualTime)
        {
          actualTime = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public void StartTimer()
    {
      stopWatch.Stop();
      stopWatch.Reset();

      ActualTime = 0;
      serialDisposable.Disposable = Observable.Interval(interval).Subscribe(OnInternallTick);
    }

    public void StopTimer()
    {
      serialDisposable.Disposable?.Dispose();
      ActualTime = null;
      stopWatch.Stop();
      stopWatch.Reset();
    }

    private void OnInternallTick(long tick)
    {
      stopWatch.Stop();

      ActualTime += stopWatch.ElapsedMilliseconds; 

      subject.OnNext(tick);

      stopWatch.Reset();
      stopWatch.Start();
    }

    public override void Dispose()
    {
      serialDisposable?.Dispose();
    }
  }
}