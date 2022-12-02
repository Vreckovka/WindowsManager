using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using VCore.Standard;
using VCore.WPF.Misc;

namespace WindowsManager.ViewModels.ProcessManagement
{
  public class ProcessViewModel : ViewModel
  {
    public string Name { get; set; }

    public Process Process { get; set; }



    #region IsFavorite

    private bool isFavorite;

    public bool IsFavorite
    {
      get { return isFavorite; }
      set
      {
        if (value != isFavorite)
        {
          isFavorite = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion




    public double TotalMemorySize
    {
      get
      {
        return ChildProcesses.Sum(x => x.Process.WorkingSet64) / 1024 / 1024;
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
}