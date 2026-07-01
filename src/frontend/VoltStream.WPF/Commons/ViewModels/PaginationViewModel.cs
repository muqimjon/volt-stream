namespace VoltStream.WPF.Commons.ViewModels;

using ApiServices.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class PaginationViewModel : ObservableObject
{
    private readonly Func<Task> reload;

    public PaginationViewModel(Func<Task> reload) => this.reload = reload;

    public IReadOnlyList<int> PageSizeOptions { get; } = [15, 30, 50, 80];

    [ObservableProperty] private int page = 1;
    [ObservableProperty] private int pageSize = 30;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private int totalPages = 1;

    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public string Summary => TotalCount == 0
        ? "0"
        : $"{(Page - 1) * PageSize + 1}–{Math.Min(Page * PageSize, TotalCount)} / {TotalCount}";

    public void Apply(PagedListMetadata? meta)
    {
        if (meta is null) return;
        TotalCount = meta.TotalCount;
        TotalPages = meta.TotalPages < 1 ? 1 : meta.TotalPages;
        if (Page > TotalPages) Page = TotalPages;
    }

    public void SetTotal(int count)
    {
        TotalCount = count;
        TotalPages = count == 0 ? 1 : (int)Math.Ceiling((double)count / PageSize);
        if (Page > TotalPages) Page = TotalPages;
        if (Page < 1) Page = 1;
    }

    public IEnumerable<T> Slice<T>(IEnumerable<T> source)
        => source.Skip((Page - 1) * PageSize).Take(PageSize);

    public void Reset() => Page = 1;

    partial void OnPageChanged(int value) => NotifyState();
    partial void OnTotalPagesChanged(int value) => NotifyState();
    partial void OnTotalCountChanged(int value) => NotifyState();

    partial void OnPageSizeChanged(int value)
    {
        Page = 1;
        _ = reload();
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(HasPrevious));
        OnPropertyChanged(nameof(HasNext));
        OnPropertyChanged(nameof(Summary));
        FirstCommand.NotifyCanExecuteChanged();
        PreviousCommand.NotifyCanExecuteChanged();
        NextCommand.NotifyCanExecuteChanged();
        LastCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(HasPrevious))] private Task First() => GoTo(1);
    [RelayCommand(CanExecute = nameof(HasPrevious))] private Task Previous() => GoTo(Page - 1);
    [RelayCommand(CanExecute = nameof(HasNext))] private Task Next() => GoTo(Page + 1);
    [RelayCommand(CanExecute = nameof(HasNext))] private Task Last() => GoTo(TotalPages);

    private Task GoTo(int target)
    {
        if (target < 1 || target > TotalPages || target == Page) return Task.CompletedTask;
        Page = target;
        return reload();
    }
}
