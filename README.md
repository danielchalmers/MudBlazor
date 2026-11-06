# MudBlazor#14134 iOS AOT repro

Investigation branch for [MudBlazor#14134](https://github.com/MudBlazor/MudBlazor/issues/14134), where moving a MudSlider crashes iOS apps built with Mono full AOT. Both workflows run on the `xcode-27` image (Xcode 27.1); the macos-26 and macos-15 arm64 images never got a runner. Results are in the job summaries; logs and crash reports are in the artifacts.

## Reporter's app

`reporter-app.yml` builds [techscex/MudBlazorTestOne](https://github.com/techscex/MudBlazorTestOne) at a pinned commit and opens its `/matched-sliders` page with the app's synthetic driver, which moves one real MudBlazor 9.12.0 MudSlider through four values. `run-reporter.sh` runs each case twice after a warm-up launch, in Debug and Release under full AOT and in Debug with the interpreter as a control.

It runs on two workload sets that pin the same iOS (27.1.10018) and MAUI (10.0.110) and differ only in Mono: 10.0.402.1 (runtime 10.0.13, the reporter's) and 10.0.403 (runtime 10.0.14).

## Minimal repro

`ios-aot-repro.yml` copies the shape of `MudSlider<T>.SetValueTextAsync` with no MAUI, Blazor or MudBlazor, and runs each case in a fresh launch. On runtime 10.0.14 every case passed in Debug and Release full AOT; an earlier run's two failures were launch flakes that died before any app code ran.

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

To run on a Mac instead: `bash run-sim.sh` or `bash run-reporter.sh` (after cloning the reporter's repo into `reporter/`), adding `-p:MtouchInterpreter=all` for the control.
