using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WindowsManager.Modularity;
using WindowsManager.Views;
using WindowsManager.Views.ProcessManagement;
using VCore;
using VCore.ItemsCollections;
using VCore.Standard;
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
    Size
  }

  public class ProcessViewModel : ViewModel
  {
    public string Name { get; set; }

    public Process Process { get; set; }


    public double TotalMemorySize
    {
      get
      {
        return ChildProcesses.Sum(x => x.Process.WorkingSet64) / (1024 * 1024);
      }
    }

    #region IsLoading

    private bool isLoading;

    public bool IsLoading
    {
      get { return isLoading; }
      set
      {
        if (value != isLoading)
        {
          isLoading = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ChildProcesses

    private IEnumerable<ProcessViewModel> childProcesses;

    public IEnumerable<ProcessViewModel> ChildProcesses
    {
      get { return childProcesses; }
      set
      {
        if (value != childProcesses)
        {
          childProcesses = value;
          RaisePropertyChanged();
          RaisePropertyChanged(nameof(TotalMemorySize));
        }
      }
    }

    #endregion

    #region CloseCommand

    private ActionCommand closeCommand;

    public ICommand CloseCommand
    {
      get
      {
        return closeCommand ??= new ActionCommand(OnClose);
      }
    }


    private async void OnClose()
    {
      try
      {
        IsLoading = true;

        await Task.Run(() =>
        {
          Process process = new Process();
          process.StartInfo.FileName = "cmd.exe";
          process.StartInfo.CreateNoWindow = true;
          process.StartInfo.RedirectStandardInput = true;
          process.StartInfo.RedirectStandardOutput = true;
          process.StartInfo.UseShellExecute = false;
          process.Start();

          string command = $"taskkill /IM \"{Name}.exe\" -f";

          process.StandardInput.WriteLine(command);
          process.StandardInput.Flush();
          process.StandardInput.Close();
          process.WaitForExit();
        });
      }
      finally
      {
        IsLoading = false;
      }

    }

    #endregion
  }


  public class ProcessesViewModel : RegionViewModel<ProcessesView>
  {
    private Subject<string> subject = new Subject<string>();
    public ProcessesViewModel(IRegionProvider regionProvider) : base(regionProvider)
    {
      SetSort(SortBy.SubCount);

      Observable.Interval(TimeSpan.FromSeconds(1)).ObserveOnDispatcher().Subscribe((x) => UpdateProcesses()).DisposeWith(this);

      MainProcessesFiltered = MainProcesses;

      subject.ObserveOnDispatcher().Throttle(TimeSpan.FromSeconds(0.2)).Subscribe(x =>
      {
        if (string.IsNullOrEmpty(x))
        {
          MainProcessesFiltered = MainProcesses;
        }
        else
        {
          MainProcessesFiltered = new RxObservableCollection<ProcessViewModel>(MainProcesses.Where(p => IsInSearch(p.Name,x)));
        }
      });
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

    private void SetSort(SortBy sortBy)
    {
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
        default:
          throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null);
      }
    }

    #endregion

    #region UpdateProcesses

    private void UpdateProcesses()
    {
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

      existing.ForEach(x =>
    {
      var newVersion = allProcesessesList.Single(p => p.Name == x.Name);
      x.ChildProcesses = newVersion.ChildProcesses;
    });
    }

    #endregion

    private bool IsInSearch(string name, string predicate)
    {
      return name.Similarity(predicate) > 0.8 || name.Contains(predicate) || predicate.Contains(name);
    }
  }
}
