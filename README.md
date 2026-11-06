# MudBlazor#14134 iOS AOT repro

Investigation branch for [MudBlazor#14134](https://github.com/MudBlazor/MudBlazor/issues/14134), where moving a MudSlider crashes iOS apps built with Mono full AOT. Both workflows run on the `xcode-27` image (Xcode 27.1); the macos-26 and macos-15 arm64 images never got a runner. Results are in the job summaries; logs and crash reports are in the artifacts.

## Reporter's app

`reporter-app.yml` builds [techscex/MudBlazorTestOne](https://github.com/techscex/MudBlazorTestOne) at a pinned commit and opens its `/matched-sliders` page with the app's synthetic driver, which moves one real MudBlazor 9.12.0 MudSlider through four values. `run-reporter.sh` runs each case twice after a warm-up launch, in Debug and Release under full AOT and in Debug with the interpreter as a control.

It runs on two workload sets that pin the same iOS (27.1.10018) and MAUI (10.0.110) and differ only in Mono: 10.0.402.1 (runtime 10.0.13, the reporter's) and 10.0.403 (runtime 10.0.14).

Both runtimes crash under full AOT in every case, with `Attempting to JIT compile method 'void MudBlazor.MudSlider`1/<SetValueTextAsync>d__91<double>:MoveNext ()' while running in aot-only mode`, and pass with the interpreter. Updating .NET doesn't fix it.

## Patches

`slider-patches.yml` builds MudBlazor 9.12.0 from source with one patch from `patches/` and swaps the assembly into the reporter's app, or adds a file from `patches/app/` to the app. Each variant runs the same cases on runtime 10.0.14 in Debug and Release full AOT.

| Variant | Change | Result |
| --- | --- | --- |
| `stock` | None; a source build of the tag | Crashes like the package |
| `parse-helper` | `SetValueTextAsync` stays async; `T.TryParse` moves to a separate method | Crashes; Mono now tries to JIT that method |
| `sync-setter` | `SetValueTextAsync` isn't async; the second `SetValueAsync` is chained through a non-generic helper | No crash, but the app stops responding after the first input |
| `typed-parse` | Built-in number types parse with their own `TryParse`; only other types reach `T.TryParse`, in a separate method | Crashes; Mono tries to JIT the fallback even though int and double never call it |
| `app-instantiate` | The app constructs `MudSlider<int>` and `MudSlider<double>` in a method it never calls, so Mono precompiles those types | Passes |
| `double-parse` | Parses a double, then `T.CreateSaturating` | Passes |
| `typed-only` | Built-in number types only, with no `T.TryParse` anywhere (experiment) | Passes |
| `decimal-direct` | An earlier shape of MudBlazor#14136: `decimal.TryParse` behind a `typeof(T)` check and a box cast, a double and `T.CreateSaturating` for everything else | Passes ([run](https://github.com/danielchalmers/MudBlazor/actions/runs/42298999676)) |
| `float-or-decimal` | An earlier shape of MudBlazor#14136: float, double and Half parse a double, everything else a decimal, both through `T.CreateSaturating` | Passes ([run](https://github.com/danielchalmers/MudBlazor/actions/runs/42306603096)) |
| `decimal-then-double` | The shape in MudBlazor#14136: types other than float, double and Half try a decimal, then everything tries a double, both through `T.CreateSaturating` | Passes ([run](https://github.com/danielchalmers/MudBlazor/actions/runs/42312425037)) |

When Mono hasn't compiled `MudSlider<T>` for the concrete `T`, it runs shared generic code, which can't contain `T.TryParse`, and starting a shared method resolves everything it calls. `T.Zero`, `T.One` and `T.CreateTruncating` in the constructor work in that mode.

## Minimal repro

`ios-aot-repro.yml` copies the shape of `MudSlider<T>.SetValueTextAsync` with no MAUI, Blazor or MudBlazor, and runs each case in a fresh launch on runtime 10.0.14.

| Case | What it isolates | Full AOT |
| --- | --- | --- |
| `svm-async-int` | Slider shape: async over struct `T`, `T.TryParse`, reached through a Blazor-style binder and `DynamicInvoke` | Passes |
| `svm-async-double` | Same with `double` | Passes |
| `svm-async-int-direct` | Same method called through the delegate directly, no binder | Passes |
| `nosvm-async-int` | Async over `T` without the static abstract call | Passes |
| `svm-sync-int` | Static abstract call, no async state machine over `T` | Passes |
| `nosvm-sync-int` | Neither | Passes |
| `svm-async-long-shared` | Slider shape, but the closed type is only a type argument to a shared generic method called from the app, like `OpenComponent<MudSlider<double>>` | Crashes in Debug and Release |
| `nosvm-async-long-shared` | Same reach, no `T.TryParse` | Passes |
| `sat-async-long-shared` | Same reach, `double.TryParse` then `T.CreateSaturating` (the fix) | Passes |
| `bare-long-shared` | Smallest form: a non-async method calling `T.TryParse`, reached through a shared generic factory | Throws `ExecutionEngineException` in Debug and Release |
| `bare-short-new` | Same method on a type the app constructs with `new` | Passes |

The first six pass because `typeof(SvmAsyncHolder<int>)` in the library lets the AOT compiler see the closed types. Everything passes with the interpreter.

`Issue14134.Shapes` always builds with `Optimize=true`, because MudBlazor ships optimized and that makes its async state machines structs.

The same cases passed under CoreCLR (`dotnet run check.cs`) and under WebAssembly AOT, where the real MudSlider also worked, so neither stands in for iOS.

To run on a Mac instead: `bash run-sim.sh` or `bash run-reporter.sh` (after cloning the reporter's repo into `reporter/`), adding `-p:MtouchInterpreter=all` for the control.
