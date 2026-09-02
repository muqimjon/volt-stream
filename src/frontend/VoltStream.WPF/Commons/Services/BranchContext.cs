namespace VoltStream.WPF.Commons.Services;

using ApiServices.Extensions;
using ApiServices.Interfaces;
using ApiServices.Models;
using ApiServices.Models.Responses;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using VoltStream.WPF.Commons.Localization;
using VoltStream.WPF.Configurations;

public partial class BranchContext(IBranchesApi branchesApi, ISessionService session) : ObservableObject
{
    private static readonly string ConfigPath = AppStorage.PathFor("branch.json");

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BranchId))]
    [NotifyPropertyChangedFor(nameof(BranchName))]
    private ObservableCollection<BranchResponse> branches = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BranchId))]
    [NotifyPropertyChangedFor(nameof(BranchName))]
    private BranchResponse? selected;

    public bool CanSelect => session.IsAdmin;

    public long? BranchId => session.IsAdmin
        ? (Selected is { Id: > 0 } ? Selected.Id : null)
        : session.BranchId;

    public string? BranchName => session.IsAdmin ? Selected?.Name : session.BranchName;

    partial void OnSelectedChanged(BranchResponse? value)
    {
        if (value is not null) Save(value.Id);
    }

    public async Task LoadAsync()
    {
        if (!CanSelect) return;

        var response = await branchesApi.GetAllAsync().Handle();
        if (!response.IsSuccess) return;

        ObservableCollection<BranchResponse> list =
        [
            new() { Id = 0, Name = TranslationSource.T("Header.AllBranches"), IsActive = true },
            .. response.Data
        ];
        Branches = list;

        var savedId = Load();
        Selected = Branches.FirstOrDefault(b => b.Id == savedId) ?? Branches[0];
    }

    public void ApplyTo(FilteringRequest request)
    {
        if (BranchId is not long id) return;
        request.Filters ??= new();
        request.Filters["BranchId"] = [$"={id}"];
    }

    private static long Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
                return JsonSerializer.Deserialize<Dto>(File.ReadAllText(ConfigPath))?.BranchId ?? 0;
        }
        catch { }
        return 0;
    }

    private static void Save(long branchId)
    {
        try { File.WriteAllText(ConfigPath, JsonSerializer.Serialize(new Dto(branchId))); }
        catch { }
    }

    private sealed record Dto(long BranchId);
}
