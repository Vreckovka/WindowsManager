using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using System.Windows.Input;
using WindowsManager.Modularity;
using WindowsManager.Views;
using WindowsManager.Views.ProcessManagement;
using VCore;
using VCore.ItemsCollections;
using VCore.Standard.Helpers;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels;

namespace WindowsManager.ViewModels.ProcessManagement
{
  public enum SortBy
  {
    Name,
    SubCount,
    Size,
    IsFavorite,
    None
  }


  public class ProcessesViewModel : RegionViewModel<ProcessesView>
  {
    private List<string> favoriteProcesess = new List<string>();
    private Subject<string> subject = new Subject<string>();
    private string favoritesPath = "\\Data\\favorites.txt";
    private SerialDisposable serialDisposable;

    public ProcessesViewModel(IRegionProvider regionProvider) : base(regionProvider)
    {
      serialDisposable = new SerialDisposable().DisposeWith(this);

      SetSort(SortBy.IsFavorite);

      Observable.Interval(TimeSpan.FromSeconds(5)).ObserveOnDispatcher().Subscribe((x) => UpdateProcesses()).DisposeWith(this);

      MainProcessesFiltered = MainProcesses;

      subject.ObserveOnDispatcher().Throttle(TimeSpan.FromSeconds(0.2)).Subscribe(x =>
      {
        if (string.IsNullOrEmpty(x))
        {
          MainProcessesFiltered = MainProcesses;
        }
        else
        {
          MainProcessesFiltered = new RxObservableCollection<ProcessViewModel>(MainProcesses.Where(p => IsInSearch(p.Name, x)));
        }
      });

      favoriteProcesess = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(favoritesPath));

      SubscribeToFavorites();
    }

    public override string RegionName { get; protected set; } = RegionNames.MainContent;
    public override string Header => "Processes";

    #region SearchString

    private string searchString;

    public string SearchString
    {
      get { return searchString; }
      set
      {
        if (value != searchString)
        {
          searchString = value;
          subject.OnNext(value);

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region MainProcessesFiltered

    private RxObservableCollection<ProcessViewModel> mainProcessesFiltered = new RxObservableCollection<ProcessViewModel>();

    public RxObservableCollection<ProcessViewModel> MainProcessesFiltered
    {
      get { return mainProcessesFiltered; }
      set
      {
        if (value != mainProcessesFiltered)
        {
          mainProcessesFiltered = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region MainProcesses
    public RxObservableCollection<ProcessViewModel> MainProcesses { get; } = new RxObservableCollection<ProcessViewModel>();

    #endregion

    #region SortCommand

    private ActionCommand<SortBy> sortCommand;

    public ICommand SortCommand
    {
      get
      {
        return sortCommand ??= new ActionCommand<SortBy>(OnSortCommand);
      }
    }


    private void OnSortCommand(SortBy sortBy)
    {
      SetSort(sortBy);
    }

    #endregion

    #region SetSort

    private SortBy acutalSortBy;
    private void SetSort(SortBy sortBy)
    {
      MainProcesses.SortType = new Comparison<ProcessViewModel>((x, y) => 0);
      acutalSortBy = sortBy;

      switch (sortBy)
      {
        case SortBy.Name:
          MainProcesses.SortType = new Comparison<ProcessViewModel>((x, y) => String.Compare(x.Name, y.Name, StringComparison.Ordinal));
          break;
        case SortBy.SubCount:
          MainProcesses.SortType = new Comparison<ProcessViewModel>((x, y) => y.ChildProcesses.Count().CompareTo(x.ChildProcesses.Count()));
          break;
        case SortBy.Size:
          MainProcesses.SortType = new Comparison<ProcessViewModel>((x, y) => y.TotalMemorySize.CompareTo(x.TotalMemorySize));
          break;
        case SortBy.IsFavorite:
          MainProcesses.SortType = new Comparison<ProcessViewModel>((x, y) =>
          {
            var result = y.IsFavorite.CompareTo(x.IsFavorite);
            return result != 0 ? result : y.TotalMemorySize.CompareTo(x.TotalMemorySize);
          });
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null);
      }
    }

    #endregion

    #region UpdateProcesses

    private void UpdateProcesses()
    {
      MainProcesses.DisableNotification();

      var allProcessesList = Process.GetProcesses();
      var allProcesses = allProcessesList.GroupBy(x => x.ProcessName).ToList();

      var allProcesessesList = allProcesses.Select(x => new ProcessViewModel()
      {
        Name = x.Key,
        ChildProcesses = x.Select(p => new ProcessViewModel()
        {
          Name = p.ProcessName,
          Process = p
        }).ToList()
      }).ToList();


      var removed = MainProcesses.Where(p => !allProcesessesList.Any(l => p.Name == l.Name)).ToList();
      var newItems = allProcesessesList.Where(p => !MainProcesses.Any(l => p.Name == l.Name)).ToList();
      var existing = MainProcesses.Where(p => allProcesessesList.Any(l => p.Name == l.Name)).ToList();

      MainProcesses.AddRange(newItems);
      MainProcesses.RemoveRange(removed);

      if (!string.IsNullOrEmpty(searchString))
      {
        MainProcessesFiltered.AddRange(newItems.Where(x => IsInSearch(x.Name, searchString)));
        MainProcessesFiltered.RemoveRange(removed);
      }

      serialDisposable.Disposable?.Dispose();

      MainProcesses.Where(x => favoriteProcesess.Contains(x.Name)).ForEach(x => x.IsFavorite = true);

      SubscribeToFavorites();
      SetSort(acutalSortBy);

      existing.ForEach(x => {
        var newVersion = allProcesessesList.Single(p => p.Name == x.Name);

        x.ChildProcesses = newVersion.ChildProcesses;
      });

      MainProcesses.SortView();
      MainProcesses.EnableNotification();
    }

    #endregion

    private bool IsInSearch(string name, string predicate)
    {
      return name.Similarity(predicate) > 0.8 || name.Contains(predicate) || predicate.Contains(name);
    }

    private void SubscribeToFavorites()
    {
      serialDisposable.Disposable = MainProcesses.ItemUpdated
        .Where(x => x.EventArgs.PropertyName == nameof(ProcessViewModel.IsFavorite))
        .Throttle(TimeSpan.FromSeconds(0.15))
        .Subscribe(x => { SaveFavorites(); });
    }

    private void SaveFavorites()
    {
      var newFavoriteProcesess = favoriteProcesess.ToList();

      var existingFavoriteProcesses = MainProcesses.Where(x => favoriteProcesess.Contains(x.Name));

      var removed = existingFavoriteProcesses.Where(x => !x.IsFavorite);
      var added = MainProcesses.Where(x => x.IsFavorite).Where(x => !favoriteProcesess.Contains(x.Name));

      newFavoriteProcesess.RemoveAll(x => removed.Select(x => x.Name).Contains(x));
      newFavoriteProcesess.AddRange(added.Select(x => x.Name));
      favoriteProcesess = newFavoriteProcesess;

      Directory.CreateDirectory("Data");
      File.WriteAllText(favoritesPath, JsonSerializer.Serialize(newFavoriteProcesess));
    }
  }
}
