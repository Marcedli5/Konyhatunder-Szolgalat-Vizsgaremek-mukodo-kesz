using System;
using System.Windows.Input;
using WPF_AdminFelulet.Commands;

namespace WPF_AdminFelulet.ViewModels;

public sealed class IssueReportDialogViewModel : ViewModelBase
{
    private readonly RelayCommand _saveCommand;
    private string _description = string.Empty;

    public IssueReportDialogViewModel()
    {
        _saveCommand = new RelayCommand(_ => Save(), _ => IsFormValid);
        SaveCommand = _saveCommand;
        CancelCommand = new RelayCommand(_ => Cancel());
    }

    public event EventHandler? RequestClose;

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    public bool? DialogResult { get; private set; }

    public string Description
    {
        get => _description;
        set
        {
            if (SetProperty(ref _description, value))
            {
                RefreshState();
            }
        }
    }

    public bool IsFormValid => !string.IsNullOrWhiteSpace(Description);

    private void Save()
    {
        if (!IsFormValid)
        {
            return;
        }

        DialogResult = true;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private void Cancel()
    {
        DialogResult = false;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshState()
    {
        OnPropertyChanged(nameof(IsFormValid));
        _saveCommand.RaiseCanExecuteChanged();
    }
}
