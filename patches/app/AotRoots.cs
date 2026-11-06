using MudBlazor;

namespace MudSliderHybridRepro;

// Never called. Constructing the closed slider types here lets Mono's AOT compiler see them and emit their methods,
// instead of leaving MudSlider<int> and MudSlider<double> to shared generic code.
internal static class AotRoots
{
    internal static object[] Sliders() => [new MudSlider<int>(), new MudSlider<double>()];
}
