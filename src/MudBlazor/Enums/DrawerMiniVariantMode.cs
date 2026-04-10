namespace MudBlazor;

/// <summary>
/// Indicates how a <see cref="DrawerVariant.Mini"/> <see cref="MudDrawer"/> behaves below its breakpoint.
/// </summary>
public enum DrawerMiniVariantMode
{
    /// <summary>
    /// The drawer stays compact when closed.
    /// </summary>
    Compact,

    /// <summary>
    /// The drawer behaves like <see cref="DrawerVariant.Temporary"/> below its breakpoint.
    /// </summary>
    Temporary
}
