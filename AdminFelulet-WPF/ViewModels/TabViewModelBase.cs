namespace WPF_AdminFelulet.ViewModels;

public abstract class TabViewModelBase : ViewModelBase
{
    protected TabViewModelBase(string title, string description)
    {
        Title = title;
        Description = description;
    }

    public string Title { get; }

    public string Description { get; }
}
