using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace MudSmoke.Components;

// Catches what a case throws while rendering or handling an event, and reports it to the runner.
public sealed class SmokeBoundary : ErrorBoundaryBase
{
    [Parameter]
    public Action<Exception>? OnError { get; set; }

    protected override Task OnErrorAsync(Exception exception)
    {
        OnError?.Invoke(exception);
        return Task.CompletedTask;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (CurrentException is null)
        {
            builder.AddContent(0, ChildContent);
        }
        else
        {
            builder.AddContent(1, "error: " + CurrentException.GetType().Name);
        }
    }
}
