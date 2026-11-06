# MudBlazor#14134 iOS AOT repro

Investigation branch for [MudBlazor#14134](https://github.com/MudBlazor/MudBlazor/issues/14134), where moving a MudSlider crashes iOS apps built with Mono full AOT. It copies the shape of `MudSlider<T>.SetValueTextAsync` (9.12.0) with no MAUI, Blazor or MudBlazor, and runs each case in a fresh launch on an Apple Silicon simulator.

The job builds Debug and Release under full AOT, then Debug with the interpreter as a control, on the `xcode-27` image (Xcode 27.1, iOS workload 27.1.10018). Results are in the job summary; logs and crash reports are in the artifact.

| Case | What it isolates |
| --- | --- |
| `svm-async-int` | Slider shape: async over struct `T`, `T.TryParse`, reached through a Blazor-style binder and `DynamicInvoke` |
| `svm-async-double` | Same with `double` |
| `svm-async-int-direct` | Same method called through the delegate directly, no binder |
| `nosvm-async-int` | Candidate fix 2: async over `T` without the static abstract call |
| `svm-sync-int` | Candidate fix 1: static abstract call, no async state machine over `T` |
| `nosvm-sync-int` | Both fixes together |

`Issue14134.Shapes` always builds with `Optimize=true`, because MudBlazor ships optimized and that makes its async state machines structs.

The same cases passed under CoreCLR (`dotnet run check.cs`) and under WebAssembly AOT, where the real MudSlider also worked, so neither stands in for iOS.

To run on a Mac instead: `bash run-sim.sh`, or `bash run-sim.sh -p:MtouchInterpreter=all` for the control.
