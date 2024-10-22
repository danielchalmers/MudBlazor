﻿// Copyright (c) MudBlazor 2021
// MudBlazor licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Services;
using MudBlazor.Utilities;
using MudBlazor.Utilities.Exceptions;

namespace MudBlazor
{
#nullable enable
    public partial class MudSelect<T> : MudBaseInput<T>, IMudSelect, IMudShadowSelect
    {
        private string? _activeItemId;
        private bool? _selectAllChecked;
        private string? _multiSelectionText;
        private IEqualityComparer<T?>? _comparer;
        private TaskCompletionSource? _renderComplete;
        private MudInput<string> _elementReference = null!;
        private HashSet<T?> _selectedValues = new HashSet<T?>();
        protected internal List<MudSelectItem<T>> _items = new();
        private string _elementId = Identifier.Create("select");

        protected string OuterClassname =>
            new CssBuilder("mud-select")
                .AddClass(OuterClass)
                .Build();

        protected string Classname =>
            new CssBuilder("mud-select")
                .AddClass(Class)
                .Build();

        protected string InputClassname =>
            new CssBuilder("mud-select-input")
                .AddClass(InputClass)
                .Build();

        [Inject]
        private IKeyInterceptorService KeyInterceptorService { get; set; } = null!;

        [Inject]
        private IScrollManager ScrollManager { get; set; } = null!;

        private Task SelectNextItem() => SelectAdjacentItem(+1);

        private Task SelectPreviousItem() => SelectAdjacentItem(-1);

        private async Task SelectAdjacentItem(int direction)
        {
            if (_items.Count == 0)
                return;
            var index = _items.FindIndex(x => x.ItemId == _activeItemId);
            if (direction < 0 && index < 0)
                index = 0;
            MudSelectItem<T>? item = null;
            // the loop allows us to jump over disabled items until we reach the next non-disabled one
            for (var i = 0; i < _items.Count; i++)
            {
                index += direction;
                if (index < 0)
                    index = 0;
                if (index >= _items.Count)
                    index = _items.Count - 1;
                if (_items[index].Disabled)
                    continue;
                item = _items[index];
                if (!MultiSelection)
                {
                    _selectedValues.Clear();
                    _selectedValues.Add(item.Value);
                    await SetValueAsync(item.Value, updateText: true);
                    HighlightItem(item);
                    break;
                }

                // in multiselect mode don't select anything, just highlight.
                // selecting is done by Enter
                HighlightItem(item);
                break;
            }
            await _elementReference.SetText(Text);
            await ScrollToItemAsync(item);
        }
        private ValueTask ScrollToItemAsync(MudSelectItem<T>? item)
            => item != null ? ScrollManager.ScrollToListItemAsync(item.ItemId) : ValueTask.CompletedTask;

        private async Task SelectFirstItem(string? startChar = null)
        {
            if (_items.Count == 0)
                return;
            var items = _items.Where(x => !x.Disabled);
            if (!string.IsNullOrWhiteSpace(startChar))
            {
                // find first item that starts with the letter
                var currentItem = items.FirstOrDefault(x => x.ItemId == _activeItemId);
                if (currentItem != null &&
                    Converter.Set(currentItem.Value)?.ToLowerInvariant().StartsWith(startChar) == true)
                {
                    // this will step through all items that start with the same letter if pressed multiple times
                    items = items.SkipWhile(x => x != currentItem).Skip(1);
                }
                items = items.Where(x => Converter.Set(x.Value)?.ToLowerInvariant().StartsWith(startChar) == true);
            }
            var item = items.FirstOrDefault();
            if (item == null)
                return;
            if (!MultiSelection)
            {
                _selectedValues.Clear();
                _selectedValues.Add(item.Value);
                await SetValueAsync(item.Value, updateText: true);
                HighlightItem(item);
            }
            else
            {
                HighlightItem(item);
            }
            await _elementReference.SetText(Text);
            await ScrollToItemAsync(item);
        }

        private async Task SelectLastItem()
        {
            if (_items.Count == 0)
                return;
            var item = _items.LastOrDefault(x => !x.Disabled);
            if (item == null)
                return;
            if (!MultiSelection)
            {
                _selectedValues.Clear();
                _selectedValues.Add(item.Value);
                await SetValueAsync(item.Value, updateText: true);
                HighlightItem(item);
            }
            else
            {
                HighlightItem(item);
            }
            await _elementReference.SetText(Text);
            await ScrollToItemAsync(item);
        }

        /// <summary>
        /// The outer div's classnames, separated by space.
        /// </summary>
        [Category(CategoryTypes.FormComponent.Appearance)]
        [Parameter]
        public string? OuterClass { get; set; }

        /// <summary>
        /// Input's classnames, separated by space.
        /// </summary>
        [Category(CategoryTypes.FormComponent.Appearance)]
        [Parameter]
        public string? InputClass { get; set; }

        /// <summary>
        /// Fired when dropdown opens.
        /// </summary>
        [Category(CategoryTypes.FormComponent.Behavior)]
        [Parameter]
        public EventCallback OnOpen { get; set; }

        /// <summary>
        /// Fired when dropdown closes.
        /// </summary>
        [Category(CategoryTypes.FormComponent.Behavior)]
        [Parameter]
        public EventCallback OnClose { get; set; }

        /// <summary>
        /// Add the MudSelectItems here
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.ListBehavior)]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// User class names for the popover, separated by space
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.ListAppearance)]
        public string? PopoverClass { get; set; }

        /// <summary>
        /// User class names for the internal list, separated by space
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.ListAppearance)]
        public string? ListClass { get; set; }

        /// <summary>
        /// If true, compact vertical padding will be applied to all Select items.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.ListAppearance)]
        public bool Dense { get; set; }

        /// <summary>
        /// The Open Select Icon
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.Appearance)]
        public string OpenIcon { get; set; } = Icons.Material.Filled.ArrowDropDown;

        /// <summary>
        /// The Close Select Icon
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.Appearance)]
        public string CloseIcon { get; set; } = Icons.Material.Filled.ArrowDropUp;

        /// <summary>
        /// If set to true and the MultiSelection option is set to true, a "select all" checkbox is added at the top of the list of items.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.ListBehavior)]
        public bool SelectAll { get; set; }

        /// <summary>
        /// Define the text of the Select All option.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.ListAppearance)]
        public string SelectAllText { get; set; } = "Select all";

        /// <summary>
        /// Fires when SelectedValues changes.
        /// </summary>
        [Parameter]
        public EventCallback<IEnumerable<T?>?> SelectedValuesChanged { get; set; }

        /// <summary>
        /// Function to define a customized multiselection text.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.Behavior)]
        public Func<List<string?>?, string>? MultiSelectionTextFunc { get; set; }

        /// <summary>
        /// Parameter to define the delimited string separator.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.Behavior)]
        public string Delimiter { get; set; } = ", ";

        /// <summary>
        /// Set of selected values. If MultiSelection is false it will only ever contain a single value. This property is two-way bindable.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.Data)]
        public IEnumerable<T?>? SelectedValues
        {
            get
            {
                return _selectedValues;
            }
            set
            {
                var set = value ?? new HashSet<T?>(_comparer);
                var selectedValues = SelectedValues ?? new HashSet<T>(_comparer);
                if (selectedValues.Count() == set.Count() && _selectedValues.All(x => set.Contains(x)))
                    return;
                _selectedValues = new HashSet<T?>(set, _comparer);
                SelectionChangedFromOutside?.Invoke(_selectedValues);
                if (!MultiSelection)
                    SetValueAsync(_selectedValues.FirstOrDefault()).CatchAndLog();
                else
                {
                    //Warning. Here the Converter was not set yet
                    if (MultiSelectionTextFunc != null)
                    {
                        SetCustomizedTextAsync(string.Join(Delimiter, _selectedValues.Select(Converter.Set)),
                            selectedConvertedValues: _selectedValues.Select(Converter.Set).ToList(),
                            multiSelectionTextFunc: MultiSelectionTextFunc).CatchAndLog();
                    }
                    else
                    {
                        SetTextAsync(string.Join(Delimiter, _selectedValues.Select(Converter.Set)), updateValue: false).CatchAndLog();
                    }
                }
                SelectedValuesChanged.InvokeAsync(new HashSet<T?>(_selectedValues, _comparer));
                if (MultiSelection && typeof(T) == typeof(string))
                    SetValueAsync((T?)(object?)Text, updateText: false).CatchAndLog();
            }
        }

        /// <summary>
        /// The Comparer to use for comparing selected values internally.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.Behavior)]
        public IEqualityComparer<T?>? Comparer
        {
            get => _comparer;
            set
            {
                _comparer = value;
                // Apply comparer and refresh selected values
                _selectedValues = new HashSet<T?>(_selectedValues, _comparer);
                SelectedValues = _selectedValues;
            }
        }

        private Func<T?, string?>? _toStringFunc = x => x?.ToString();

        /// <summary>
        /// Defines how values are displayed in the drop-down list
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.ListBehavior)]
        public Func<T?, string?>? ToStringFunc
        {
            get => _toStringFunc;
            set
            {
                if (_toStringFunc == value)
                    return;
                _toStringFunc = value;
                Converter = new Converter<T>
                {
                    SetFunc = _toStringFunc ?? (x => x?.ToString()),
                    //GetFunc = LookupValue,
                };
            }
        }

        public MudSelect()
        {
            Adornment = Adornment.End;
            IconSize = Size.Medium;
        }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);
            if (firstRender && Value != null)
            {
                // we need to render the initial Value which is not possible without the items
                // which supply the RenderFragment. So in this case, a second render is necessary
                StateHasChanged();
            }
            UpdateSelectAllChecked();
            lock (this)
            {
                if (_renderComplete != null)
                {
                    _renderComplete.TrySetResult();
                    _renderComplete = null;
                }
            }
        }

        private Task WaitForRender()
        {
            Task? t;
            lock (this)
            {
                if (_renderComplete != null)
                    return _renderComplete.Task;
                _renderComplete = new TaskCompletionSource();
                t = _renderComplete.Task;
            }
            StateHasChanged();
            return t;
        }

        /// <summary>
        /// Returns whether or not the Value can be found in items. If not, the Select will display it as a string.
        /// </summary>
        protected bool CanRenderValue
        {
            get
            {
                if (Value == null)
                    return false;
                if (!_shadowLookup.TryGetValue(Value, out var item))
                    return false;
                return item.ChildContent != null;
            }
        }

        protected bool IsValueInList
        {
            get
            {
                if (Value == null)
                    return false;
                return _shadowLookup.TryGetValue(Value, out _);
            }
        }

        protected RenderFragment? GetSelectedValuePresenter()
        {
            if (Value == null)
                return null;
            if (!_shadowLookup.TryGetValue(Value, out var item))
                return null; //<-- for now. we'll add a custom template to present values (set from outside) which are not on the list?
            return item.ChildContent;
        }

        protected override Task UpdateValuePropertyAsync(bool updateText)
        {
            // For MultiSelection of non-string T's we don't update the Value!!!
            if (typeof(T) == typeof(string) || !MultiSelection)
                base.UpdateValuePropertyAsync(updateText);
            return Task.CompletedTask;
        }

        protected override Task UpdateTextPropertyAsync(bool updateValue)
        {
            // when multiselection is true, we return
            // a comma separated list of selected values
            if (MultiSelectionTextFunc != null)
            {
                return MultiSelection
                    ? SetCustomizedTextAsync(string.Join(Delimiter, SelectedValues!.Select(Converter.Set)),
                        selectedConvertedValues: SelectedValues!.Select(Converter.Set).ToList(),
                        multiSelectionTextFunc: MultiSelectionTextFunc)
                    : base.UpdateTextPropertyAsync(updateValue);
            }

            return MultiSelection
                ? SetTextAsync(string.Join(Delimiter, SelectedValues!.Select(Converter.Set)))
                : base.UpdateTextPropertyAsync(updateValue);
        }

        internal event Action<ICollection<T?>>? SelectionChangedFromOutside;

        private bool _multiSelection;
        /// <summary>
        /// If true, multiple values can be selected via checkboxes which are automatically shown in the dropdown
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.ListBehavior)]
        public bool MultiSelection
        {
            get => _multiSelection;
            set
            {
                if (_multiSelection != value)
                {
                    _multiSelection = value;
                    UpdateTextPropertyAsync(false).CatchAndLog();
                }
            }
        }

        /// <summary>
        /// The collection of items within this select
        /// </summary>
        public IReadOnlyList<MudSelectItem<T>> Items => _items;

#nullable disable
        protected Dictionary<T, MudSelectItem<T>> _valueLookup = new();
        protected Dictionary<T, MudSelectItem<T>> _shadowLookup = new();
#nullable enable

        internal bool Add(MudSelectItem<T>? item)
        {
            if (item == null)
                return false;
            bool? result = null;
            if (!_items.Select(x => x.Value).Contains(item.Value))
            {
                _items.Add(item);

                if (item.Value != null)
                {
                    _valueLookup[item.Value] = item;
                    if (item.Value.Equals(Value) && !MultiSelection)
                        result = true;
                }
            }
            UpdateSelectAllChecked();
            if (result.HasValue == false)
            {
                result = item.Value?.Equals(Value);
            }
            return result == true;
        }

        internal void Remove(MudSelectItem<T> item)
        {
            _items.Remove(item);
            if (item.Value != null)
                _valueLookup.Remove(item.Value);
        }

        /// <summary>
        /// Sets the maxheight the Select can have when open.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.ListAppearance)]
        public int MaxHeight { get; set; } = 300;

        /// <summary>
        /// Set the anchor origin point to determine where the popover will open from.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.ListAppearance)]
        public Origin AnchorOrigin { get; set; } = Origin.BottomLeft;

        /// <summary>
        /// Sets the transform origin point for the popover.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.ListAppearance)]
        public Origin TransformOrigin { get; set; } = Origin.TopLeft;

        /// <summary>
        /// If true, the Select's input will not show any values that are not defined in the dropdown.
        /// This can be useful if Value is bound to a variable which is initialized to a value which is not in the list
        /// and you want the Select to show the label / placeholder instead.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.Behavior)]
        public bool Strict { get; set; }

        /// <summary>
        /// Show clear button.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.Behavior)]
        public bool Clearable { get; set; } = false;

        /// <summary>
        /// Custom clear icon when <see cref="Clearable"/> is enabled.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.Appearance)]
        public string ClearIcon { get; set; } = Icons.Material.Filled.Clear;

        /// <summary>
        /// If true, prevent scrolling while dropdown is open.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.ListBehavior)]
        public bool LockScroll { get; set; } = false;

        /// <summary>
        /// Button click event for clear button. Called after text and value has been cleared.
        /// </summary>
        [Parameter]
        public EventCallback<MouseEventArgs> OnClearButtonClick { get; set; }

        internal bool _open;

        public string? _currentIcon { get; set; }

        public async Task SelectOption(int index)
        {
            if (index < 0 || index >= _items.Count)
            {
                if (!MultiSelection)
                    await CloseMenu();
                return;
            }
            await SelectOption(_items[index].Value);
        }

        public async Task SelectOption(object? obj)
        {
            var value = (T?)obj;
            if (MultiSelection)
            {
                // multi-selection: menu stays open
                if (!_selectedValues.Add(value))
                    _selectedValues.Remove(value);

                if (MultiSelectionTextFunc != null)
                {
                    await SetCustomizedTextAsync(string.Join(Delimiter, SelectedValues!.Select(Converter.Set)),
                        selectedConvertedValues: SelectedValues!.Select(Converter.Set).ToList(),
                        multiSelectionTextFunc: MultiSelectionTextFunc);
                }
                else
                {
                    await SetTextAsync(string.Join(Delimiter, SelectedValues!.Select(Converter.Set)), updateValue: false);
                }

                UpdateSelectAllChecked();
                await BeginValidateAsync();
            }
            else
            {
                // single selection
                // CloseMenu(true) doesn't close popover in BSS
                await CloseMenu(false);

                if (EqualityComparer<T>.Default.Equals(Value, value))
                {
                    StateHasChanged();
                    return;
                }

                await SetValueAsync(value);
                _elementReference.SetText(Text).CatchAndLog();
                _selectedValues.Clear();
                _selectedValues.Add(value);
            }

            HighlightItemForValueAsync(value);
            await SelectedValuesChanged.InvokeAsync(SelectedValues);
            if (MultiSelection && typeof(T) == typeof(string))
                await SetValueAsync((T?)(object?)Text, updateText: false);
            await InvokeAsync(StateHasChanged);
        }

        private async void HighlightItemForValueAsync(T? value)
        {
            if (value == null)
            {
                HighlightItem(null);
                return;
            }
            await WaitForRender();
            _valueLookup.TryGetValue(value, out var item);
            HighlightItem(item);
        }

        private async void HighlightItem(MudSelectItem<T>? item)
        {
            _activeItemId = item?.ItemId;
            // we need to make sure we are just after a render here or else there will be race conditions
            await WaitForRender();
            // Note: this is a hack, but I found no other way to make the list highlight the currently highlighted item
            // without the delay it always shows the previously highlighted item because the popup items don't exist yet
            // they are only registered after they are rendered, so we need to render again!
            await Task.Delay(1);
            StateHasChanged();
        }

        private async Task HighlightSelectedValue()
        {
            await WaitForRender();
            if (MultiSelection)
                HighlightItem(_items.FirstOrDefault(x => !x.Disabled));
            else
                HighlightItemForValueAsync(Value);
        }

        private void UpdateSelectAllChecked()
        {
            if (MultiSelection && SelectAll)
            {
                if (_selectedValues.Count == 0)
                {
                    _selectAllChecked = false;
                }
                else if (_items.Count(x => !x.Disabled) == _selectedValues.Count)
                {
                    _selectAllChecked = true;
                }
                else
                {
                    _selectAllChecked = null;
                }
            }
        }

        public async Task ToggleMenu()
        {
            if (GetDisabledState() || GetReadOnlyState())
                return;
            if (_open)
                await CloseMenu(true);
            else
                await OpenMenu();
        }

        public async Task OpenMenu()
        {
            if (GetDisabledState() || GetReadOnlyState())
                return;
            _open = true;
            UpdateIcon();
            StateHasChanged();
            await HighlightSelectedValue();
            //Scroll the active item on each opening
            if (_activeItemId != null)
            {
                var index = _items.FindIndex(x => x.ItemId == _activeItemId);
                if (index > 0)
                {
                    var item = _items[index];
                    await ScrollToItemAsync(item);
                }
            }
            //disable escape propagation: if selectmenu is open, only the select popover should close and underlying components should not handle escape key
            await KeyInterceptorService.UpdateKeyAsync(_elementId, new("Escape", stopDown: "key+none"));

            await OnOpen.InvokeAsync();
        }

        public async Task CloseMenu(bool focusAgain = true)
        {
            _open = false;
            UpdateIcon();
            if (focusAgain)
            {
                StateHasChanged();
                await OnBlur.InvokeAsync(new FocusEventArgs());
                _elementReference.FocusAsync().CatchAndLog(ignoreExceptions: true);
                StateHasChanged();
            }

            //enable escape propagation: the select popover was closed, now underlying components are allowed to handle escape key
            await KeyInterceptorService.UpdateKeyAsync(_elementId, new("Escape", stopDown: "none"));

            await OnClose.InvokeAsync();
        }

        private void UpdateIcon()
        {
            _currentIcon = !string.IsNullOrWhiteSpace(AdornmentIcon) ? AdornmentIcon : _open ? CloseIcon : OpenIcon;
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            UpdateIcon();
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            UpdateIcon();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var options = new KeyInterceptorOptions(
                    "mud-input-control",
                    [
                        // prevent scrolling page, toggle open/close
                        new(" ", preventDown: "key+none"),
                        // prevent scrolling page, instead highlight previous item
                        new("ArrowUp", preventDown: "key+none"),
                        // prevent scrolling page, instead highlight next item
                        new("ArrowDown", preventDown: "key+none"),
                        new("Home", preventDown: "key+none"),
                        new("End", preventDown: "key+none"),
                        new("Escape"),
                        new("Enter", preventDown: "key+none"),
                        new("NumpadEnter", preventDown: "key+none"),
                        // select all items instead of all page text
                        new("a", preventDown: "key+ctrl"),
                        // select all items instead of all page text
                        new("A", preventDown: "key+ctrl"),
                        // for our users
                        new("/./", subscribeDown: true, subscribeUp: true)
                    ]);

                await KeyInterceptorService.SubscribeAsync(_elementId, options, keyDown: HandleKeyDownAsync, keyUp: HandleKeyUpAsync);
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        public void CheckGenericTypeMatch(object selectItem)
        {
            var itemT = selectItem.GetType().GenericTypeArguments[0];
            if (itemT != typeof(T))
                throw new GenericTypeMismatchException("MudSelect", "MudSelectItem", typeof(T), itemT);
        }

        public override ValueTask FocusAsync()
        {
            return _elementReference.FocusAsync();
        }

        public override ValueTask BlurAsync()
        {
            return _elementReference.BlurAsync();
        }

        public override ValueTask SelectAsync()
        {
            return _elementReference.SelectAsync();
        }

        public override ValueTask SelectRangeAsync(int pos1, int pos2)
        {
            return _elementReference.SelectRangeAsync(pos1, pos2);
        }

        /// <summary>
        /// Extra handler for clearing selection.
        /// </summary>
        protected async ValueTask SelectClearButtonClickHandlerAsync(MouseEventArgs e)
        {
            await SetValueAsync(default, false);
            await SetTextAsync(default, false);
            _selectedValues.Clear();
            await BeginValidateAsync();
            StateHasChanged();
            await SelectedValuesChanged.InvokeAsync(_selectedValues);
            await OnClearButtonClick.InvokeAsync(e);
        }

        protected async Task SetCustomizedTextAsync(string text, bool updateValue = true,
            List<string?>? selectedConvertedValues = null,
            Func<List<string?>?, string>? multiSelectionTextFunc = null)
        {
            // The Text property of the control is updated
            Text = multiSelectionTextFunc?.Invoke(selectedConvertedValues);

            // The comparison is made on the multiSelectionText variable
            if (_multiSelectionText != text)
            {
                _multiSelectionText = text;
                if (!string.IsNullOrWhiteSpace(_multiSelectionText))
                    Touched = true;
                if (updateValue)
                    await UpdateValuePropertyAsync(false);
                await TextChanged.InvokeAsync(_multiSelectionText);
            }
        }

        /// <summary>
        /// Custom checked icon.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.ListAppearance)]
        public string CheckedIcon { get; set; } = Icons.Material.Filled.CheckBox;

        /// <summary>
        /// Custom unchecked icon.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.ListAppearance)]
        public string UncheckedIcon { get; set; } = Icons.Material.Filled.CheckBoxOutlineBlank;

        /// <summary>
        /// Custom indeterminate icon.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.ListAppearance)]
        public string IndeterminateIcon { get; set; } = Icons.Material.Filled.IndeterminateCheckBox;

        /// <summary>
        /// The checkbox icon reflects the select all option's state
        /// </summary>
        protected string SelectAllCheckBoxIcon
        {
            get => _selectAllChecked.HasValue ? _selectAllChecked.Value ? CheckedIcon : UncheckedIcon : IndeterminateIcon;
        }

        internal async Task HandleKeyDownAsync(KeyboardEventArgs obj)
        {
            if (GetDisabledState() || GetReadOnlyState())
                return;
            var key = obj.Key.ToLowerInvariant();
            if (_open && key.Length == 1 && key != " " && !(obj.CtrlKey || obj.ShiftKey || obj.AltKey || obj.MetaKey))
            {
                await SelectFirstItem(key);
                return;
            }
            switch (obj.Key)
            {
                case "Tab":
                    await CloseMenu(false);
                    break;
                case "ArrowUp":
                    if (obj.AltKey)
                    {
                        await CloseMenu();
                        break;
                    }

                    if (_open == false)
                    {
                        await OpenMenu();
                        break;
                    }

                    await SelectPreviousItem();
                    break;
                case "ArrowDown":
                    if (obj.AltKey)
                    {
                        await OpenMenu();
                        break;
                    }

                    if (_open == false)
                    {
                        await OpenMenu();
                        break;
                    }

                    await SelectNextItem();
                    break;
                case " ":
                    await ToggleMenu();
                    break;
                case "Escape":
                    await CloseMenu(true);
                    break;
                case "Home":
                    await SelectFirstItem();
                    break;
                case "End":
                    await SelectLastItem();
                    break;
                case "Enter":
                case "NumpadEnter":
                    var index = _items.FindIndex(x => x.ItemId == _activeItemId);
                    if (!MultiSelection)
                    {
                        if (!_open)
                        {
                            await OpenMenu();
                            break;
                        }

                        // this also closes the menu
                        await SelectOption(index);
                        break;
                    }

                    if (!_open)
                    {
                        await OpenMenu();
                        break;
                    }

                    await SelectOption(index);
                    await _elementReference.SetText(Text);
                    break;
                case "a":
                case "A":
                    if (obj.CtrlKey)
                    {
                        if (MultiSelection)
                        {
                            await SelectAllClickAsync();
                            //If we didn't add delay, it won't work.
                            await WaitForRender();
                            await Task.Delay(1);
                            StateHasChanged();
                            //It only works when selecting all, not render unselect all.
                            //UpdateSelectAllChecked();
                        }
                    }
                    break;
            }

            await OnKeyDown.InvokeAsync(obj);
        }

        internal Task HandleKeyUpAsync(KeyboardEventArgs obj)
        {
            return OnKeyUp.InvokeAsync(obj);
        }

        /// <summary>
        /// Clear the selection
        /// </summary>
        public async Task Clear()
        {
            await SetValueAsync(default, false);
            await SetTextAsync(default, false);
            _selectedValues.Clear();
            await BeginValidateAsync();
            StateHasChanged();
            await SelectedValuesChanged.InvokeAsync(_selectedValues);
        }

        private async Task SelectAllClickAsync()
        {
            // Manage the fake tri-state of a checkbox
            if (!_selectAllChecked.HasValue)
                _selectAllChecked = true;
            else if (_selectAllChecked.Value)
                _selectAllChecked = false;
            else
                _selectAllChecked = true;
            // Define the items selection
            if (_selectAllChecked.Value)
                await SelectAllItems();
            else
                await Clear();
        }

        private async Task SelectAllItems()
        {
            if (!MultiSelection)
                return;
            var selectedValues = new HashSet<T?>(_items.Where(x => !x.Disabled && x.Value != null).Select(x => x.Value), _comparer);
            _selectedValues = new HashSet<T?>(selectedValues, _comparer);
            if (MultiSelectionTextFunc != null)
            {
                await SetCustomizedTextAsync(string.Join(Delimiter, SelectedValues!.Select(Converter.Set)),
                    selectedConvertedValues: SelectedValues!.Select(Converter.Set).ToList(),
                    multiSelectionTextFunc: MultiSelectionTextFunc);
            }
            else
            {
                await SetTextAsync(string.Join(Delimiter, SelectedValues!.Select(Converter.Set)), updateValue: false);
            }
            UpdateSelectAllChecked();
            _selectedValues = selectedValues; // need to force selected values because Blazor overwrites it under certain circumstances due to changes of Text or Value
            await BeginValidateAsync();
            await SelectedValuesChanged.InvokeAsync(SelectedValues);
            if (MultiSelection && typeof(T) == typeof(string))
                SetValueAsync((T?)(object?)Text, updateText: false).CatchAndLog();
        }

        public void RegisterShadowItem(MudSelectItem<T>? item)
        {
            if (item == null || item.Value == null)
                return;
            _shadowLookup[item.Value] = item;
        }

        public void UnregisterShadowItem(MudSelectItem<T>? item)
        {
            if (item == null || item.Value == null)
                return;
            _shadowLookup.Remove(item.Value);
        }

        private async Task OnFocusOutAsync(FocusEventArgs focusEventArgs)
        {
            if (_open)
            {
                // when the menu is open we immediately get back the focus if we lose it (i.e. because of checkboxes in multi-select)
                // otherwise we can't receive key strokes any longer
                await FocusAsync();
            }
        }

        internal Task OnBlurAsync(FocusEventArgs obj)
        {
            return base.OnBlur.InvokeAsync(obj);
        }

        /// <inheritdoc />
        protected override async ValueTask DisposeAsyncCore()
        {
            await base.DisposeAsyncCore();

            if (IsJSRuntimeAvailable)
            {
                await KeyInterceptorService.UnsubscribeAsync(_elementId);
            }
        }

        /// <summary>
        /// Fixes issue #4328
        /// Returns true when MultiSelection is true and it has selected values(Since Value property is not used when MultiSelection=true
        /// </summary>
        /// <param name="value"></param>
        /// <returns>True when component has a value</returns>
        protected override bool HasValue(T? value)
        {
            if (MultiSelection)
                return SelectedValues?.Any() ?? false;
            return base.HasValue(value);
        }

        public override async Task ForceUpdate()
        {
            await base.ForceUpdate();
            if (MultiSelection == false)
            {
                SelectedValues = new HashSet<T?>(_comparer) { Value };
            }
            else
            {
                await SelectedValuesChanged.InvokeAsync(new HashSet<T?>(SelectedValues!, _comparer));
            }
        }
    }
}
