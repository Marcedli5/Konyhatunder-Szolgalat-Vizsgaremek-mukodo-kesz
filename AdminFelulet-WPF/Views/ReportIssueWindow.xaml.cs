using System.Windows;
using WPF_AdminFelulet.ViewModels;

namespace WPF_AdminFelulet.Views;

public partial class ReportIssueWindow : Window
{
    public ReportIssueWindow(IssueReportDialogViewModel viewModel)
    {
        DataContext = viewModel;
        viewModel.RequestClose += (_, _) =>
        {
            DialogResult = viewModel.DialogResult;
            Close();
        };

        InitializeComponent();
    }
}
