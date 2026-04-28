# Agent Repo Map

## Repository Layout

- `src/` contains the product code and nearly all project work. Expect the main
  library, docs app, tests, analyzers, benchmarks, and related support projects
  to live here.
- `src/MudBlazor/` is the core component library. Most component, utility,
  styling, `TScripts`, and `wwwroot` changes land here.
- `src/MudBlazor.UnitTests*` contains test projects and test support code. Look
  here for component tests, shared test infrastructure, viewer-only helpers, and
  docs-related tests.
- `src/MudBlazor.Docs*` contains the documentation site, examples, and docs
  build support. Update docs here when component behavior or public API changes.
- `src/MudBlazor.Analyzers*` contains analyzer, code-fix, and analyzer-test
  projects.
- `src/MudBlazor.Examples.Data/` contains shared example data used by docs and
  samples.
- `src/MudBlazor.Benchmarks/` contains benchmark projects. Only touch or build
  it for benchmark work.
- Repo-wide build configuration is centered in `src/`, especially
  `src/Directory.Build.*` and `src/.editorconfig`.
- Tooling and automation live primarily in `tools/`, `.config/`, and `.github/`.
- Treat `bin/`, `obj/`, `TestResults/`, generated files, and similar outputs as
  build artifacts unless the task explicitly targets them.

## Environment Requirements

- The required .NET SDK is defined in `global.json`; currently it pins
  `10.0.100` with `rollForward` set to `latestFeature`.
- The library targets `net8.0`, `net9.0`, and `net10.0`.
- The test projects target `net10.0`.
- Verify the active SDK with `dotnet --version`.

## Project Targets

- Components: `src/MudBlazor/MudBlazor.csproj` and
  `src/MudBlazor.UnitTests/MudBlazor.UnitTests.csproj`
- Docs: `src/MudBlazor.Docs.Compiler/MudBlazor.Docs.Compiler.csproj`,
  `src/MudBlazor.Docs/MudBlazor.Docs.csproj`,
  `src/MudBlazor.Docs.Wasm/MudBlazor.Docs.Wasm.csproj`,
  `src/MudBlazor.Docs.Server/MudBlazor.Docs.Server.csproj`, and
  `src/MudBlazor.Docs.WasmHost/MudBlazor.Docs.WasmHost.csproj`
- Docs tests: `src/MudBlazor.UnitTests.Docs/MudBlazor.UnitTests.Docs.csproj`
- Analyzers and code fixes:
  `src/MudBlazor.Analyzers/MudBlazor.Analyzers.csproj`,
  `src/MudBlazor.Analyzers.CodeFixes/MudBlazor.Analyzers.CodeFixes.csproj`,
  `src/MudBlazor.Analyzers.TestComponents/MudBlazor.Analyzers.TestComponents.csproj`,
  and `src/MudBlazor.UnitTests.Analyzers/MudBlazor.UnitTests.Analyzers.csproj`
- Benchmarks: `src/MudBlazor.Benchmarks/MudBlazor.Benchmarks.csproj`
