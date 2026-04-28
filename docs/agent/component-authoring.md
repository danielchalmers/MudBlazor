# Component Authoring Rules

## Parameters and State

- Component parameters must be auto-properties only. Do not put logic in getters
  or setters.
- Do not overwrite component parameters directly. Use the backing
  `ParameterState<T>` and update through `.Value` or `SetValueAsync`.
- Do not set other component parameters via `@ref` (`BL0005`). Use declarative
  binding instead.
- Use `ParameterState<T>` for parameter updates and change handlers.
- Parameters managed through the parameter-state framework should be annotated
  with `[Parameter, ParameterState]`.

## Styling and Naming

- Use `CssBuilder` for classes and styles.
- Use CSS variables and design tokens. Do not hard-code colors.
- Prefer positive parameter names. Avoid names like `DisableGutters`; prefer
  `Gutters`.

## Public API Documentation

- Add XML `<summary>` documentation for all public properties.
- Prefer concise summaries that describe behavior, not "Gets or sets..."
  boilerplate.
- Add `<remarks>` for public parameters when useful, including the default value
  when relevant.
- Add the appropriate `[Category(CategoryTypes....)]` attribute to public
  component parameters.

Example:

```csharp
/// <summary>
/// Uses compact vertical padding.
/// </summary>
/// <remarks>
/// Defaults to <c>false</c>.
/// </remarks>
[Parameter]
[Category(CategoryTypes.Radio.Appearance)]
public bool Dense { get; set; }
```

or

```csharp
/// <summary>
/// Prevents interaction with background elements while this list is open.
/// </summary>
/// <remarks>
/// Defaults to <see cref="PopoverOptions.ModalOverlay" />.
/// </remarks>
[Parameter]
[Category(CategoryTypes.FormComponent.ListBehavior)]
public bool? Modal { get; set; }
```

## Parameter Registration Pattern

- Register parameters in the constructor with `CreateRegisterScope()`.
- Use `.WithParameter(...)`, `.WithEventCallback(...)`, and
  `.WithChangeHandler(...)` where appropriate.
- Put reaction logic in the change handler, not in the property setter.
- Prefer method-group handlers for shared logic.

Example:

```csharp
private readonly ParameterState<bool> _expandedState;

[Parameter]
public bool Expanded { get; set; }

[Parameter]
public EventCallback<bool> ExpandedChanged { get; set; }

public MudExample()
{
    using var registerScope = CreateRegisterScope();
    _expandedState = registerScope.RegisterParameter<bool>(nameof(Expanded))
        .WithParameter(() => Expanded)
        .WithEventCallback(() => ExpandedChanged)
        .WithChangeHandler(OnExpandedChangedAsync);
}

private Task ToggleAsync()
{
    return _expandedState.SetValueAsync(!_expandedState.Value);
}
```

## Accessibility and Behavior

- Add `[CascadingParameter] public bool RightToLeft { get; set; }` when layout
  depends on direction.
- Follow best ARIA practices without adding noise.
- When generating HTML or ARIA attributes in component code, prefer fallback
  values so caller-provided attributes can override them whenever feasible; do
  not hard-force generated attributes unless the behavior truly requires it.
- Ensure keyboard navigation works for interactive components.
- Provide accessible names for interactive controls through a label,
  `aria-label`, or `aria-labelledby`.
- Components with logic require bUnit tests and a docs page at
  `src/MudBlazor.Docs/Pages/Components/<ComponentName>.razor`.
