// Copyright (c) MudBlazor 2021
// MudBlazor licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Docs.Models;
using MudBlazor.Docs.Services;
using MudBlazor.Utilities;
namespace MudBlazor.Docs.Shared;

#nullable enable
public partial class Appbar
{
    private const string SearchKeyShortcuts = "Control+K";
    private static readonly IEnumerable<JsKeyModifier> _searchHotkeyLeftModifiers = [JsKeyModifier.ControlLeft];
    private static readonly IEnumerable<JsKeyModifier> _searchHotkeyRightModifiers = [JsKeyModifier.ControlRight];
    private bool _searchDialogOpen;
    private bool _searchDialogAutocompleteOpen;
    private int _searchDialogReturnedItemsCount;
    private MudAutocomplete<ApiLinkServiceEntry> _searchBarAutocomplete = null!;
    private MudAutocomplete<ApiLinkServiceEntry> _searchDialogAutocomplete = null!;
    private readonly DialogOptions _dialogOptions = new()
    {
        Position = DialogPosition.Center,
        MaxWidth = MaxWidth.Large,
        FullWidth = true,
        NoHeader = true
    };

    public bool IsSearchDialogOpen
    {
        get => _searchDialogOpen;
        set
        {
            _searchDialogAutocompleteOpen = default;
            _searchDialogReturnedItemsCount = default;
            _searchDialogOpen = value;
        }
    }

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private IApiLinkService ApiLinkService { get; set; } = null!;

    [Inject]
    private LayoutService LayoutService { get; set; } = null!;

    [Parameter]
    public EventCallback<MouseEventArgs> DrawerToggleCallback { get; set; }

    [Parameter]
    public bool DisplaySearchBar { get; set; } = true;

    private async Task OnSearchResult(ApiLinkServiceEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        NavigationManager.NavigateTo(entry.Link);
        IsSearchDialogOpen = false;
        await ClearSearchAsync();
    }

    private string GetActiveClass(DocsBasePage page)
    {
        return page == LayoutService.GetDocsBasePage(NavigationManager.Uri) ? "mud-chip-text mud-chip-color-primary mx-1 px-3" : "mx-1 px-3";
    }

    private Task<IReadOnlyCollection<ApiLinkServiceEntry>> Search(string text, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(ApiLinkService.GetAllEntries());
        }

        return ApiLinkService.Search(text);
    }

    private Task OpenSearchDialogAsync()
    {
        IsSearchDialogOpen = true;
        return Task.CompletedTask;
    }

    private async Task OpenSearchFromHotkeyAsync()
    {
        if (DisplaySearchBar)
        {
            await FocusSearchBarAsync();
            return;
        }

        await OpenSearchDialogAsync();
    }

    private async Task FocusSearchBarAsync()
    {
        if (_searchBarAutocomplete is null)
        {
            return;
        }

        await _searchBarAutocomplete.FocusAsync();
        await _searchBarAutocomplete.OpenMenuAsync();
    }

    private async Task ClearSearchAsync()
    {
        if (_searchBarAutocomplete is not null)
        {
            await _searchBarAutocomplete.ClearAsync();
        }

        if (_searchDialogAutocomplete is not null)
        {
            await _searchDialogAutocomplete.ClearAsync();
        }
    }

    private Task HandleSearchDialogKeyUp(KeyboardEventArgs args)
    {
        if (args.Key == "Escape")
        {
            IsSearchDialogOpen = false;
        }

        return Task.CompletedTask;
    }
}
