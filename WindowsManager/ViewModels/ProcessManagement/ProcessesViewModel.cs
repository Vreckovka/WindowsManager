using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using WindowsManager.Modularity;
using WindowsManager.Views.ProcessManagement;
using VCore;
using VCore.Standard.Helpers;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels;

namespace WindowsManager.ViewModels.ProcessManagement
{
  public enum SortBy { Name, SubCount, Size, IsFavorite, None }

  // ObservableCollection owns no per-item subscriptions. Departed processes
  // therefore cannot stay rooted by the collection's notification machinery.
  public class ProcessCollection : ObservableCollection<ProcessViewModel>
  {
    public ObservableCollection<ProcessViewModel> View => this;
    public void Synchronize(IEnumerable<ProcessViewModel> items)
    {
      var desired = items.ToArray();
      var retained = new HashSet<ProcessViewModel>(desired);
      for (var i = Count - 1; i >= 0; i--)
        if (!retained.Contains(this[i])) RemoveAt(i);
      for (var i = 0; i < desired.Length; i++)
      {
        if (i < Count && ReferenceEquals(this[i], desired[i])) continue;
        var oldIndex = IndexOf(desired[i]);
        if (oldIndex >= 0) Move(oldIndex, i);
        else Insert(i, desired[i]);
      }
    }
  }

  public class ProcessesViewModel : RegionViewModel<ProcessesView>
  {
    private readonly HashSet<string> favoriteProcesses;
    private readonly Subject<string> searchSubject = new Subject<string>();
    private readonly Subject<bool> favoritesSubject = new Subject<bool>();
    private readonly string favoritesPath = Path.Combine(AppContext.BaseDirectory, "Data", "favorites.txt");
    private readonly DispatcherTimer refreshTimer;
    private bool updating;
    private bool disposed;
    private bool applyingSnapshot;
    private SortBy actualSortBy = SortBy.IsFavorite;

    public ProcessesViewModel(IRegionProvider regionProvider) : base(regionProvider)
    {
      var loadPath = favoritesPath;
      var legacyPath = Path.Combine(Path.GetPathRoot(AppContext.BaseDirectory), "Data", "favorites.txt");
      if (!File.Exists(loadPath) && File.Exists(legacyPath)) loadPath = legacyPath;
      try
      {
        favoriteProcesses = new HashSet<string>(File.Exists(loadPath)
          ? JsonSerializer.Deserialize<List<string>>(File.ReadAllText(loadPath)) ?? new List<string>()
          : new List<string>(), StringComparer.Ordinal);
      }
      catch (Exception ex) when (ex is IOException || ex is JsonException || ex is UnauthorizedAccessException)
      {
        Debug.WriteLine(ex);
        favoriteProcesses = new HashSet<string>(StringComparer.Ordinal);
      }

      refreshTimer = new DispatcherTimer(DispatcherPriority.Background)
        { Interval = TimeSpan.FromSeconds(5) };
      refreshTimer.Tick += OnRefreshTick;
      searchSubject.Throttle(TimeSpan.FromMilliseconds(200)).ObserveOnDispatcher()
        .Subscribe(_ => ApplyFilter()).DisposeWith(this);
      favoritesSubject.Throttle(TimeSpan.FromMilliseconds(150)).ObserveOnDispatcher()
        .Subscribe(_ => SaveFavorites()).DisposeWith(this);
    }

    public override string RegionName { get; protected set; } = RegionNames.MainContent;
    public override string Header => "Processes";
    public ProcessCollection MainProcesses { get; } = new ProcessCollection();
    public ProcessCollection MainProcessesFiltered { get; } = new ProcessCollection();

    private string searchString;
    public string SearchString
    {
      get => searchString;
      set
      {
        if (value == searchString) return;
        searchString = value;
        RaisePropertyChanged();
        searchSubject.OnNext(value);
      }
    }

    private ActionCommand<SortBy> sortCommand;
    public ICommand SortCommand => sortCommand ??= new ActionCommand<SortBy>(OnSortCommand);
    private void OnSortCommand(SortBy sortBy)
    {
      actualSortBy = sortBy;
      ApplyFilter();
    }

    public override void OnActivation(bool firstActivation)
    {
      base.OnActivation(firstActivation);
      if (disposed) return;
      refreshTimer.Start();
      UpdateProcesses();
    }

    public override void OnDeactived()
    {
      refreshTimer.Stop();
      base.OnDeactived();
    }

    private void OnRefreshTick(object sender, EventArgs e) => UpdateProcesses();

    private async void UpdateProcesses()
    {
      if (disposed || updating) return;
      updating = true;
      try
      {
        var snapshot = await Task.Run(ReadProcesses);
        if (disposed) return;
        var existing = MainProcesses.ToDictionary(p => p.Name, StringComparer.Ordinal);
        var rows = new List<ProcessViewModel>();
        applyingSnapshot = true;
        try
        {
          foreach (var group in snapshot.GroupBy(p => p.Name))
          {
            if (!existing.TryGetValue(group.Key, out var row))
            {
              row = new ProcessViewModel { Name = group.Key, IsFavorite = favoriteProcesses.Contains(group.Key) };
              row.PropertyChanged += OnProcessPropertyChanged;
            }
            row.ChildProcesses = group.ToArray();
            rows.Add(row);
          }
          var names = new HashSet<string>(rows.Select(p => p.Name), StringComparer.Ordinal);
          foreach (var removed in MainProcesses.Where(p => !names.Contains(p.Name)))
            removed.PropertyChanged -= OnProcessPropertyChanged;
          MainProcesses.Synchronize(rows);
          ApplyFilter();
        }
        finally { applyingSnapshot = false; }
      }
      catch (Exception ex) when (ex is InvalidOperationException || ex is Win32Exception)
      {
        Debug.WriteLine(ex);
      }
      finally { updating = false; }
    }

    private static List<ProcessViewModel> ReadProcesses()
    {
      var result = new List<ProcessViewModel>();
      var processes = Process.GetProcesses();
      try
      {
        foreach (var process in processes)
        {
          try
          {
            result.Add(new ProcessViewModel { Name = process.ProcessName, MemorySizeBytes = process.WorkingSet64 });
          }
          catch (Exception ex) when (ex is InvalidOperationException || ex is Win32Exception)
          {
            // Processes may exit or deny access during a snapshot.
          }
        }
      }
      finally
      {
        foreach (var process in processes) process.Dispose();
      }
      return result;
    }

    private void ApplyFilter()
    {
      IEnumerable<ProcessViewModel> rows = MainProcesses;
      switch (actualSortBy)
      {
        case SortBy.Name: rows = rows.OrderBy(p => p.Name, StringComparer.Ordinal); break;
        case SortBy.SubCount: rows = rows.OrderByDescending(p => p.ChildProcesses.Count()); break;
        case SortBy.Size: rows = rows.OrderByDescending(p => p.TotalMemorySize); break;
        case SortBy.IsFavorite: rows = rows.OrderByDescending(p => p.IsFavorite).ThenByDescending(p => p.TotalMemorySize); break;
      }
      var ordered = rows.ToArray();
      MainProcesses.Synchronize(ordered);
      MainProcessesFiltered.Synchronize(string.IsNullOrEmpty(searchString)
        ? ordered : ordered.Where(p => IsInSearch(p.Name, searchString)));
    }

    private bool IsInSearch(string name, string predicate) =>
      name.Contains(predicate) || predicate.Contains(name) || name.Similarity(predicate) > 0.8;

    private void OnProcessPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      if (applyingSnapshot || e.PropertyName != nameof(ProcessViewModel.IsFavorite)) return;
      var row = (ProcessViewModel)sender;
      if (row.IsFavorite) favoriteProcesses.Add(row.Name);
      else favoriteProcesses.Remove(row.Name);
      ApplyFilter();
      favoritesSubject.OnNext(true);
    }

    private void SaveFavorites()
    {
      var json = JsonSerializer.Serialize(favoriteProcesses.OrderBy(p => p).ToArray());
      // One small write per user change; process refreshes never trigger saves.
      try
      {
        Directory.CreateDirectory(Path.GetDirectoryName(favoritesPath));
        File.WriteAllText(favoritesPath, json);
      }
      catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { Debug.WriteLine(ex); }
    }

    public override void Dispose()
    {
      if (disposed) return;
      disposed = true;
      refreshTimer.Stop();
      refreshTimer.Tick -= OnRefreshTick;
      foreach (var row in MainProcesses) row.PropertyChanged -= OnProcessPropertyChanged;
      base.Dispose();
      searchSubject.Dispose();
      favoritesSubject.Dispose();
    }
  }
}
