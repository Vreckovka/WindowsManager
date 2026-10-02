using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Markup;
using System.Xml.Linq;
using Prism.Regions;
using VCore.Standard.Modularity.Interfaces;
using VCore.WPF;
using VCore.WPF.Modularity.RegionProviders;
using WindowsManager;
using WindowsManager.ViewModels.ScreenManagement;
using WindowsManager.ViewModels.ScreenManagement.Rules;
using WindowsManager.ViewModels.TurnOff;
using WindowsManager.ViewModels.ProcessManagement;
using WindowsManager.Views;

class Regions : IRegionProvider
{
  public void ActivateView(Guid id) { }
  public void RefreshView(Guid id) { }
  public void DectivateView(Guid id) { }
  public void GoBack(Guid id) { }
  public IRegionManager RegisterView<TView, TViewModel>(string name, TViewModel model,
    bool nested, out Guid id, IRegionManager manager = null)
    where TView : class, IView where TViewModel : class, INotifyPropertyChanged, IActivable
  { id = Guid.NewGuid(); return manager; }
}

class Program
{
  static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
  static string output;
  static readonly Dictionary<string, object> results = new Dictionary<string, object>();
  static readonly List<object> samples = new List<object>();
  static ScreensManagementViewModel screens;
  static ProcessesViewModel processes;
  static Window shell;
  static long notifications;
  static readonly HashSet<DispatcherOperation> outstanding = new HashSet<DispatcherOperation>();
  static int queuePeak;
  static bool countQueue;
  static int seconds;
  static bool fullMode;

  [STAThread]
  static int Main(string[] args)
  {
    output = Path.GetFullPath(args.Length > 0 ? args[0] : "results");
    seconds = args.Length > 1 ? int.Parse(args[1]) : 90;
    Directory.CreateDirectory(output);
    var app = new System.Windows.Application();
    var resourceSource = XDocument.Load(@"D:\Aplikacie\WindowsManager\WindowsManager\App.xaml");
    var dictionary = resourceSource.Descendants().First(e => e.Name.LocalName == "ResourceDictionary");
    foreach(var attribute in resourceSource.Root.Attributes().Where(a => a.IsNamespaceDeclaration))
      if(dictionary.Attribute(attribute.Name) == null) dictionary.Add(new XAttribute(attribute));
    app.Resources = (ResourceDictionary)XamlReader.Parse(dictionary.ToString());
    app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
    StartTests(app);
    Dispatcher.Run();
    return results.TryGetValue("success", out var success) && (bool)success ? 0 : 1;
  }

  public static void Attach(System.Windows.Application app, string outputPath, int duration)
  {
    fullMode = true;
    output = Path.GetFullPath(outputPath);
    seconds = duration;
    Directory.CreateDirectory(output);
    StartTests(app);
  }

  static void StartTests(System.Windows.Application app)
  {
    var context = new DispatcherSynchronizationContext(app.Dispatcher);
    SynchronizationContext.SetSynchronizationContext(context);
    VSynchronizationContext.UISynchronizationContext = context;
    VSynchronizationContext.UIDispatcher = app.Dispatcher;
    app.Dispatcher.Hooks.OperationPosted += (_, e) => {
      if (!countQueue) return;
      lock(outstanding) { outstanding.Add(e.Operation); queuePeak = Math.Max(queuePeak, outstanding.Count); }
    };
    app.Dispatcher.Hooks.OperationCompleted += (_, e) => { lock(outstanding) outstanding.Remove(e.Operation); };
    app.Dispatcher.Hooks.OperationAborted += (_, e) => { lock(outstanding) outstanding.Remove(e.Operation); };
    app.Dispatcher.BeginInvoke(new Action(async () => {
      try { await Run(); results["success"] = true; }
      catch(Exception e) { results["error"] = e.ToString(); results["success"] = false; }
      finally
      {
        try
        {
          foreach(var screen in screens?.Screens?.ToArray() ?? Array.Empty<ScreenViewModel>())
            if(screen.IsDimmed) screen.DimmOrUnDimm();
          processes?.Dispose(); screens?.Dispose();
        }
        catch(Exception e) { results["cleanupError"] = e.ToString(); }
        Persist();
        if(fullMode) app.Shutdown(); else app.Dispatcher.InvokeShutdown();
      }
    }));
  }

  static void Persist()
  {
    results["samples"] = samples.ToArray();
    var json = System.Text.Json.JsonSerializer.Serialize(results,
      new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(Path.Combine(output,"results.tmp"), json);
    File.Move(Path.Combine(output,"results.tmp"), Path.Combine(output,"results.json"), true);
  }

  static object GetField(object obj,string name) => obj.GetType().GetField(name,Private).GetValue(obj);
  static void SetField(object obj,string name,object value) => obj.GetType().GetField(name,Private).SetValue(obj,value);
  static DimmerWindowProxy Overlay(ScreenViewModel screen) => new DimmerWindowProxy((Window)GetField(screen,"dimmerWindow"));
  class DimmerWindowProxy { public Window Window; public DimmerWindowProxy(Window window) { Window = window; } }

  static async Task Run()
  {
    results["startedUtc"] = DateTime.UtcNow;
    results["assemblySha256"] = BitConverter.ToString(System.Security.Cryptography.SHA256.Create()
      .ComputeHash(File.ReadAllBytes(typeof(ScreenViewModel).Assembly.Location))).Replace("-", "");
    results["processorCount"] = Environment.ProcessorCount;
    results["durationSeconds"] = seconds;
    results["mode"] = fullMode ? "complete application" : "component fixture";
    if(fullMode)
    {
      shell=System.Windows.Application.Current.MainWindow;
      var main=(WindowsManager.ViewModels.WindowManagerMainWindowViewModel)shell.DataContext;
      screens=main.ScreensManagementViewModel;
      processes=(ProcessesViewModel)GetField(main,"processesViewModel");
      var ready=Stopwatch.StartNew();
      while(main.MainMenu.Items.Count==0 && ready.Elapsed.TotalSeconds<30) await Task.Delay(50);
      Navigate("Monitors");
      results["launchToMenuReadyMs"]=(DateTime.UtcNow-Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalMilliseconds;
      results["menuItems"]=main.MainMenu.Items.OfType<VCore.WPF.ViewModels.Navigation.NavigationItem>().Select(i=>i.Header).ToArray();
    }
    else
    {
      var regions=new Regions();
      screens=new ScreensManagementViewModel(regions,new RuleManagerViewModel(regions),new TurnOffViewModel(regions));
      processes=new ProcessesViewModel(regions);
      shell=new Window { Title="WindowsManager performance fixture",Width=1000,Height=750,
        Content=new ScreensManagementView { DataContext=screens },ShowInTaskbar=false };
      System.Windows.Application.Current.MainWindow=shell;
      shell.Show();
    }
    var data=Path.Combine(output,"Data","Monitors");
    Directory.CreateDirectory(data);
    Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory,"Data","Monitors"));
    var source=@"D:\Moje applikacie\Builds\WindowsManager\Data\Monitors";
    foreach(var file in Directory.GetFiles(source,"*.txt"))
      File.Copy(file,Path.Combine(data,Path.GetFileName(file)),true);
    SetField(screens,"folderPath",data);
    SetField(screens,"filePath",Path.Combine(data,"monitors_data.txt"));
    if(!fullMode)
    {
      var watch=Stopwatch.StartNew(); screens.Initialize();
      results["screenInitializeMs"]=watch.Elapsed.TotalMilliseconds;
    }
    results["screenCount"]=screens.Screens.Count;
    var rules=(RuleManagerViewModel)GetField(screens,"ruleManagerViewModel");
    results["enabledRules"]=rules.Rules.Count(r=>r.IsRuleEnabled);
    for(var i=0;i<screens.Screens.Count;i++)
    {
      var screen=screens.Screens[i];
      SetField(screen,"monitorDataFilePath",Path.Combine(data,"Monitor_"+i+".txt"));
      screen.TurnOffValue=120;
      screen.FastMode=FastMode.Off;
      screen.PropertyChanged+=(_,__)=>Interlocked.Increment(ref notifications);
    }
    screens.PropertyChanged+=(_,__)=>Interlocked.Increment(ref notifications);
    await Task.Delay(1500);
    Capture(shell,"screens.png");    await TimerAccuracy();
#if CANDIDATE
    await Regressions();
#endif
    await Measure("idle", Math.Min(30, seconds / 4));
    await CloseCycles(8);
    await QueueTest();
    if(fullMode) Navigate("Processes"); else processes.IsActive = true;
    await Task.Delay(3000);
    var processView = new WindowsManager.Views.ProcessManagement.ProcessesView { DataContext = processes };
    if(!fullMode) shell.Content = processView;
    await Measure("processesVisible", Math.Min(30, seconds / 4));
    Capture(fullMode ? (FrameworkElement)shell : processView,"processes.png");
    results["processRows"] = processes.MainProcesses.Count;
    processes.SearchString = "Windows";
    await Task.Delay(400);
    results["filteredRows"] = processes.MainProcessesFiltered.Count;
    processes.SearchString = "";
    await Task.Delay(400);
    if(fullMode) Navigate("Monitors"); else processes.IsActive = false;
    if(!fullMode) shell.Content = new ScreensManagementView { DataContext=screens };
    await Measure("dimmed", Math.Min(30, seconds / 4), true);
    var soak = Stopwatch.StartNew();
    var end = Math.Max(0, seconds - 110);
    var round = 0;
    while(soak.Elapsed.TotalSeconds < end)
    {
      await Measure("soakIdle_"+round, Math.Min(60, end - (int)soak.Elapsed.TotalSeconds));
      if(soak.Elapsed.TotalSeconds >= end) break;
      await Measure("soakDimmed_"+round, Math.Min(60, end - (int)soak.Elapsed.TotalSeconds), true);
      await CloseCycles(3);
      if(fullMode && round % 5 == 4 && soak.Elapsed.TotalSeconds < end)
      {
        Navigate("Processes");
        await Measure("soakProcesses_"+round, Math.Min(30, end-(int)soak.Elapsed.TotalSeconds));
        Navigate("Monitors");
      }
      round++;
      Persist();
    }
    await Task.Delay(750);
    using(var finalProcess=Process.GetCurrentProcess())
    {
      finalProcess.Refresh();
      results["endPrivateBytes"]=finalProcess.PrivateMemorySize64;
      results["endHandles"]=finalProcess.HandleCount;
      results["endThreads"]=finalProcess.Threads.Count;
    }
    results["completedUtc"] = DateTime.UtcNow;
  }

#if CANDIDATE
  class SlowBrightness : BrightnessController
  {
    public readonly ManualResetEventSlim Entered = new ManualResetEventSlim();
    public readonly ManualResetEventSlim Release = new ManualResetEventSlim();
    public int Writes;
    public int LastValue;
    public override void SetBrightness(int value)
    {
      Entered.Set();
      if(!Release.Wait(10000)) throw new Exception("Slow brightness test timed out");
      Interlocked.Increment(ref Writes);
      LastValue=value;
    }
  }

  static async Task Regressions()
  {
    var count=screens.Screens.Count;
    screens.Initialize();
    foreach(var screen in screens.Screens) screen.Initialize();
    if(screens.Screens.Count != count) throw new Exception("Initialization duplicated monitors");
    results["reinitializeCount"]=screens.Screens.Count;
    var target=screens.Screens[0];
    var original=(BrightnessController)GetField(target,"brightnessController");
    var fake=new SlowBrightness();
    SetField(target,"brightnessController",fake);
    var originalBrightness=target.Brightness;
    target.Brightness=70;
    try
    {
      fake.RequestBrightness(1);
      if(!await Task.Run(()=>fake.Entered.Wait(2000))) throw new Exception("Brightness worker did not start");
      for(var i=0;i<1000;i++) fake.RequestBrightness(i%100);
      ShowOverlay(target);
      await Task.Delay(150);
      target.TurnOffCommand.Execute(null);
      if(System.Windows.Application.Current.Windows.OfType<WindowsManager.Windows.DimmerWindow>().Count()!=1)
        throw new Exception("Duplicate overlay was created");
      var watch=Stopwatch.StartNew();
      target.DimmOrUnDimm();
      results["closeWithBlockedDriverMs"]=watch.Elapsed.TotalMilliseconds;
      if(watch.ElapsedMilliseconds>100) throw new Exception("Slow brightness driver blocked overlay close");
      fake.Release.Set();
      await Task.Delay(750);
      results["brightnessWritesFor1000Requests"]=fake.Writes;
      if(fake.Writes>3 || fake.LastValue!=70) throw new Exception("Brightness requests were not bounded or restored");
      if(GetField(target,"dimmerWindow")!=null) throw new Exception("Closed overlay reference retained");
    }
    finally
    {
      fake.Release.Set(); fake.Dispose();
      SetField(target,"brightnessController",original);
      target.Brightness=originalBrightness;
    }
    ShowOverlay(target); await Task.Delay(100);
    var before=target.TotalDimmTime;
    var elapsedBefore=((ActionTimer)GetField(target,"dimmerTimer")).Elapsed;
    await Task.Delay(1234);
    var elapsed=((ActionTimer)GetField(target,"dimmerTimer")).Elapsed-elapsedBefore;
    target.DimmOrUnDimm();
    var counted=target.TotalDimmTime-before;
    // 'before' is the last displayed value. Include the unaccounted initial
    // fraction to compare the total against the timer's complete duration.
    results["dimmedTailCountedMs"]=counted.TotalMilliseconds;
    if(counted.TotalMilliseconds < elapsed.TotalMilliseconds-25)
      throw new Exception("Closing lost the final dimmed time fraction");
    var timer=new ActionTimer(TimeSpan.FromMilliseconds(30));
    int ticks=0;
    var sub=timer.OnTimerTick.Subscribe(new TickObserver(_=>ticks++));
    for(var i=0;i<100;i++) { timer.StartTimer(); timer.StopTimer(); }
    await Task.Delay(200);
    if(ticks!=0) throw new Exception("Stopped/restarted timer left callbacks");
    timer.Dispose(); sub.Dispose();
    results["restartStopRegression"]=true;
    await ProcessRegressions();
    await RuleRegressions();
  }

  static async Task ProcessRegressions()
  {
    var model=new ProcessesViewModel(new Regions());
    // Keep favorite persistence inside the test's output directory.
    SetField(model,"favoritesPath",Path.Combine(output,"favorites-regression.json"));
    model.OnActivation(true);
    for(var i=0;i<100 && (model.MainProcesses.Count==0 || (bool)GetField(model,"updating"));i++)
      await Task.Delay(50);
    if(model.MainProcesses.Count==0) throw new Exception("Process snapshot was empty");
    var originalRows=model.MainProcesses.ToArray();
    model.SortCommand.Execute(SortBy.Name);
    if(!model.MainProcesses.Select(p=>p.Name).SequenceEqual(originalRows.Select(p=>p.Name).OrderBy(n=>n,StringComparer.Ordinal)))
      throw new Exception("Name sorting failed");
    model.SortCommand.Execute(SortBy.Size);
    var sizes=model.MainProcesses.Select(p=>p.TotalMemorySize).ToArray();
    if(!sizes.SequenceEqual(sizes.OrderByDescending(n=>n))) throw new Exception("Memory sorting failed");
    if(model.MainProcesses.Any(p=>p.ChildProcesses.Any(c=>c.Process!=null)))
      throw new Exception("OS process handles were retained by snapshots");
    var row=originalRows.First();
    var wasFavorite=row.IsFavorite;
    row.IsFavorite=!wasFavorite;
    await Task.Delay(300);
    if(!File.Exists(Path.Combine(output,"favorites-regression.json"))) throw new Exception("Favorite change was not persisted");
    var saved=System.Text.Json.JsonSerializer.Deserialize<string[]>(File.ReadAllText(Path.Combine(output,"favorites-regression.json")));
    if(saved.Contains(row.Name)!=row.IsFavorite) throw new Exception("Favorite persistence did not match UI");
    row.IsFavorite=wasFavorite;
    model.SearchString="Windows";
    await Task.Delay(300);
    if(model.MainProcessesFiltered.Any(p=>!originalRows.Contains(p))) throw new Exception("Filtering replaced process identities");
    model.SearchString="";
    await Task.Delay(300);
    if(model.MainProcessesFiltered.Count!=model.MainProcesses.Count) throw new Exception("Clearing search lost rows");
    model.OnDeactived();
    if(((DispatcherTimer)GetField(model,"refreshTimer")).IsEnabled) throw new Exception("Hidden process view still polls");
    model.Dispose();
    results["processSortingFavoritesFilteringRegression"]=true;
  }

  static async Task RuleRegressions()
  {
    if(screens.Screens.Count<2) return;
    var a=new ScreenViewModel(new ScreenModel { Screen=screens.Screens[0].Model.Screen,TurnOffLimit=30 },
      Path.Combine(output,"rule-monitor-a.json"),screens.Screens[0].TurnOffViewModel);
    var b=new ScreenViewModel(new ScreenModel { Screen=screens.Screens[1].Model.Screen,TurnOffLimit=30 },
      Path.Combine(output,"rule-monitor-b.json"),screens.Screens[1].TurnOffViewModel);
    try
    {
      a.TurnOffValue=30; b.TurnOffValue=30;
      var rule=new WindowsManager.ViewModels.ScreenManagement.Rules.RuleTypes.LinkMonitorsRule();
      rule.Parameters[0].Value=a.Name; rule.Parameters[1].Value=b.Name;
      a.IsActive=true;
      rule.Execute(new[]{a,b});
      if(b.automaticTurnOffTimer.IsRunning || b.TimeSinceActive!=null)
        throw new Exception("Linked monitor did not cancel countdown");
      a.IsActive=false; b.IsActive=false;
      rule.Execute(new[]{a,b});
      await Task.Delay(250);
      if(!(a.TimeSinceActive>0) || !(b.TimeSinceActive>0)) throw new Exception("Linked monitor countdown did not resume");
      a.StopTurnOffTimer(); b.StopTurnOffTimer();
      rule.Revert(new[]{a,b});
      if(!a.automaticTurnOffTimer.IsRunning || !b.automaticTurnOffTimer.IsRunning)
        throw new Exception("Disabling link rule did not resume countdowns");
      b.TurnOffValue=.001;
      await Task.Delay(300);
      if(!b.IsDimmed) throw new Exception("Automatic timeout did not open overlay");
      b.Dispose();
      if(b.IsDimmed || Overlay(b).Window!=null) throw new Exception("Disposal retained automatic overlay");
      results["monitorLinkAndAutomaticCountdownRegression"]=true;
    }
    finally { a.Dispose(); b.Dispose(); }
  }
#endif
  static void Navigate(string header)
  {
    var main=(WindowsManager.ViewModels.WindowManagerMainWindowViewModel)shell.DataContext;
    foreach(var item in main.MainMenu.Items.OfType<VCore.WPF.ViewModels.Navigation.NavigationItem>().Where(i=>i.Header!=header)) item.IsActive=false;
    main.MainMenu.Items.OfType<VCore.WPF.ViewModels.Navigation.NavigationItem>().Single(i=>i.Header==header).IsActive=true;
    Console.WriteLine(DateTime.UtcNow.ToString("O")+" navigation "+header);
  }
  static async Task TimerAccuracy()
  {
    var timer = new ActionTimer(TimeSpan.FromMilliseconds(100));
    var changes = 0;
    var tickErrors = new List<double>();
    var wallWatch = new Stopwatch();
    using(var sub = timer.OnTimerTick.Subscribe(new TickObserver(_ => { changes++; lock(tickErrors) tickErrors.Add(wallWatch.Elapsed.TotalMilliseconds - (timer.ActualTime ?? 0)); })))
    {
      var watch = Stopwatch.StartNew(); wallWatch.Restart(); timer.StartTimer();
      await Task.Delay(1200);
      results["timerActualMs"] = timer.ActualTime;
      results["timerWallMs"] = watch.Elapsed.TotalMilliseconds;
      results["timerErrorMs"] = watch.Elapsed.TotalMilliseconds - timer.ActualTime;
      lock(tickErrors) { results["timerDriftAtTickMs"] = tickErrors.Last(); results["timerMaxDriftAtTickMs"] = tickErrors.Max(); }
      timer.StopTimer();
      var stopped = changes;
      await Task.Delay(350);
      results["ticksAfterStop"] = changes - stopped;
      timer.Dispose();
    }
  }
  class TickObserver : IObserver<long>
  {
    Action<long> action; public TickObserver(Action<long> action) { this.action = action; }
    public void OnNext(long tick) => action(tick);
    public void OnCompleted() { } public void OnError(Exception e) { throw e; }
  }

  static async Task Measure(string name, int duration, bool dim = false)
  {
    if(duration <= 0) return;
    Console.WriteLine(DateTime.UtcNow.ToString("O")+" measuring "+name);
    if(dim) foreach(var screen in screens.Screens) if(!screen.IsDimmed) ShowOverlay(screen);
    await Task.Delay(400);
    using var process = Process.GetCurrentProcess();
    process.Refresh(); var cpu = process.TotalProcessorTime;
    var memory = process.PrivateMemorySize64; var handles = process.HandleCount;
    Interlocked.Exchange(ref notifications,0);
    var latency = new List<double>();
    var watch = Stopwatch.StartNew();
    for(var i=0;i<duration*5;i++)
    {
      await Task.Run(async () => {
        await Task.Delay(200).ConfigureAwait(false);
        var probe=Stopwatch.StartNew();
        await shell.Dispatcher.InvokeAsync(()=>latency.Add(probe.Elapsed.TotalMilliseconds),DispatcherPriority.Input);
      });
    }
    process.Refresh();
    var used = (process.TotalProcessorTime-cpu).TotalMilliseconds;
    samples.Add(new {
      name, wallSeconds=watch.Elapsed.TotalSeconds, cpuMs=used,
      cpuPercent=used/watch.Elapsed.TotalMilliseconds/Environment.ProcessorCount*100,
      oneCoreCpuPercent=used/watch.Elapsed.TotalMilliseconds*100,
      privateBytes=process.PrivateMemorySize64, privateDeltaBytes=process.PrivateMemorySize64-memory,
      handles=process.HandleCount, handleDelta=process.HandleCount-handles,
      notifications=notifications, dispatcherLatenciesMs=latency.ToArray(), dispatcherP95Ms=Percentile(latency,.95), dispatcherMaxMs=latency.Max()
    });
    if(dim) foreach(var screen in screens.Screens) if(screen.IsDimmed) screen.DimmOrUnDimm();
    Persist();
  }

  static void ShowOverlay(ScreenViewModel screen)
  {
    screen.DimmOrUnDimm();
    var window=Overlay(screen).Window;
    // Real DimmerWindow content and event handlers, small enough to leave the
    // user's desktop usable during a long unattended fixture run.
    if(!fullMode && window != null) { window.Topmost=false; window.Width=400; window.Height=260; window.Left=20; window.Top=20; }
  }

  static async Task CloseCycles(int count)
  {
    Console.WriteLine(DateTime.UtcNow.ToString("O")+" closing overlays "+count);
    var values=new List<double>();
    var completed=new List<double>();
    for(var i=0;i<count;i++)
    {
      var screen=screens.Screens[i%screens.Screens.Count];
      ShowOverlay(screen); await Task.Delay(150);
      var window=Overlay(screen).Window;
      var watch=Stopwatch.StartNew();
      // Alternate the command and the Closed event path used by double click.
      if(i%2==0) window.Close(); else screen.DimmOrUnDimm();
      values.Add(watch.Elapsed.TotalMilliseconds);
      var pending=screen.GetType().GetProperty("PendingBrightnessWork")?.GetValue(screen) as Task;
      if(pending!=null) await pending;
      completed.Add(watch.Elapsed.TotalMilliseconds);
      await Task.Delay(150);
      if(screen.IsDimmed) throw new Exception("Overlay still dimmed after close");
    }
    if(!results.ContainsKey("closeMs")) results["closeMs"]=new List<double>();
    ((List<double>)results["closeMs"]).AddRange(values);
    var refs=screens.Screens.Count(screen=>GetField(screen,"dimmerWindow")!=null);
    results["retainedOverlayReferences"]=refs;
    if(!results.ContainsKey("closeThroughBrightnessMs")) results["closeThroughBrightnessMs"]=new List<double>();
    ((List<double>)results["closeThroughBrightnessMs"]).AddRange(completed);
    results["closeP95Ms"]=Percentile((List<double>)results["closeMs"],.95);
    results["closeMaxMs"]=((List<double>)results["closeMs"]).Max();
  }

  static async Task QueueTest()
  {
    foreach(var screen in screens.Screens) if(!screen.IsDimmed) ShowOverlay(screen);
    await Task.Delay(250);
    outstanding.Clear(); queuePeak=0; countQueue=true;
    // Reproduce a busy driver/UI thread deterministically and count dispatcher
    // operations posted during the same three-second stall in both versions.
    Thread.Sleep(3000);
    var atRelease=outstanding.Count;
    await Task.Delay(500); countQueue=false;
    results["operationsQueuedDuring3sStall"]=atRelease;
    results["queuePeak"]=queuePeak;
    foreach(var screen in screens.Screens) if(screen.IsDimmed) screen.DimmOrUnDimm();
  }

  static double Percentile(List<double> values,double fraction)
  { var sorted=values.OrderBy(v=>v).ToArray(); return sorted[Math.Min(sorted.Length-1,(int)Math.Ceiling(sorted.Length*fraction)-1)]; }

  static void Capture(FrameworkElement view,string name)
  {
    view.UpdateLayout();
    var bitmap=new RenderTargetBitmap(Math.Max(1,(int)view.ActualWidth),Math.Max(1,(int)view.ActualHeight),96,96,PixelFormats.Pbgra32);
    bitmap.Render(view);
    var encoder=new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
    using(var file=File.Create(Path.Combine(output,name))) encoder.Save(file);
  }
}
