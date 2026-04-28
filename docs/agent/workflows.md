# Agent Workflows

## Scope and Defaults

- Target specific projects only. Solution-wide commands are too slow unless
  explicitly requested.
- Keep diffs small and focused. Avoid repo-wide rewrites unless explicitly
  asked.
- Prefer targeted, non-breaking changes unless the task explicitly requires
  broader or breaking work.
- If broader follow-up improvements are identified, suggest them for a separate
  PR instead of expanding the current diff.
- Do not add new heavy dependencies or packages without approval.
- Do not make speculative large changes when the intent is unclear. Ask a
  clarifying question or propose a short plan instead.
- Follow `src/.editorconfig`.
- Treat warnings as errors. Do not ignore analyzer warnings.
- Do not run solution-wide commands unless explicitly requested.
- Do not make `dotnet clean` part of the normal local loop. Use it only when
  incremental build state is clearly stale or corrupted.
- If no code, project, test, docs app, or asset-pipeline inputs changed, do not
  call `dotnet`. Changes limited to files such as `README.md`, changelog text,
  issue templates, or other repo metadata do not require restore, build, test,
  or format.
- Prefer a single scoped `dotnet build` or `dotnet test` command as the first
  verification step. Split build and test only when you will reuse the build
  outputs for multiple test runs.
- Do not build `src/MudBlazor/MudBlazor.csproj` immediately before testing
  `src/MudBlazor.UnitTests/MudBlazor.UnitTests.csproj`; the test project already
  builds `MudBlazor`, `MudBlazor.UnitTests.Shared`, and
  `MudBlazor.UnitTests.Viewer`.

## Restore

Do not run restore automatically at the start of every session. Reuse existing
assets in the working tree.

Run restore only when restore inputs changed, when the target project's
`obj/project.assets.json` is missing, or when a `--no-restore` build or test
fails because restore data is stale.

Restore only the project graph you are about to validate:

```bash
dotnet restore src/MudBlazor.UnitTests/MudBlazor.UnitTests.csproj
dotnet restore src/MudBlazor.UnitTests.Analyzers/MudBlazor.UnitTests.Analyzers.csproj
dotnet restore src/MudBlazor.UnitTests.Docs/MudBlazor.UnitTests.Docs.csproj
dotnet restore src/MudBlazor.Docs.Server/MudBlazor.Docs.Server.csproj
dotnet tool restore --tool-manifest .config/dotnet-tools.json
```

Re-run `dotnet restore` if any of these change:

- `*.csproj`
- `src/Directory.Build.*`
- `Directory.Packages.props`, if added later
- `NuGet.Config` or other NuGet restore configuration files, if added later

If `.config/dotnet-tools.json` changes, run:

```bash
dotnet tool restore --tool-manifest .config/dotnet-tools.json
```

If `src/package.json` or `src/bun.lock` changes, run a normal scoped build
without `SkipBunCompile` for the affected project so the frontend asset pipeline
runs.

## Default Local Loop for C# or Razor Component Changes

- For a single validation pass, prefer one filtered `dotnet test` command. This
  builds the component library plus the relevant test graph and runs the
  selected tests in one invocation.
- Use `/p:SkipBunCompile=true` in this loop because it targets C#, Razor, and
  test validation that does not depend on regenerated frontend assets.

```bash
dotnet test src/MudBlazor.UnitTests/MudBlazor.UnitTests.csproj --filter "FullyQualifiedName~MenuTests" --no-restore /p:SkipBunCompile=true --nologo --blame-hang --blame-hang-timeout 30s
```

If you expect to run multiple filtered test commands against the same edits,
build once and then reuse the outputs with `--no-build`:

```bash
dotnet build src/MudBlazor.UnitTests/MudBlazor.UnitTests.csproj --no-restore /p:SkipBunCompile=true --nologo
dotnet test src/MudBlazor.UnitTests/MudBlazor.UnitTests.csproj --filter "FullyQualifiedName~MenuTests" --no-build --no-restore --nologo --blame-hang --blame-hang-timeout 30s
dotnet test src/MudBlazor.UnitTests/MudBlazor.UnitTests.csproj --filter "FullyQualifiedName~PopoverTests" --no-build --no-restore --nologo --blame-hang --blame-hang-timeout 30s
```

## Bun

- Frontend asset builds use the local `bundotnet.cli` tool from
  `.config/dotnet-tools.json`, not a separately installed global Bun.
- If Bun-related commands fail after tool or config changes, re-run
  `dotnet tool restore --tool-manifest .config/dotnet-tools.json`.
- `/p:SkipBunCompile=true` skips the Bun-driven frontend asset compilation steps
  that normally run during build.
- Use it when the goal is to validate .NET, C#, or Razor changes and you do not
  need regenerated frontend assets as part of verification.
- It is typically safe for C#-only changes, Razor logic or markup changes, test
  changes, and documentation-only changes.
- Do not use it when changes touch `TScripts`, styles, CSS, SCSS, asset pipeline
  inputs, or tooling files that affect frontend bundles such as
  `src/package.json` or `src/bun.lock`.
- Do not use it when the change depends on rebuilt generated JavaScript, CSS, or
  other static assets being present or up to date.
- If you are unsure whether the build output depends on regenerated frontend
  assets, run the normal scoped build without `SkipBunCompile`.

## Formatting

Run `dotnet format whitespace --no-restore --include <path/to/changed/files>`
once at the very end of the task as a final pre-PR pass to catch
whitespace/newline/charset mistakes. Do not run it repeatedly during the normal
edit-build-test loop.

Run this command from the `src` directory. When using `--include`, pass file
paths relative to `src`, for example:
`--include MudBlazor/Components/List/MudListItem.razor.cs`.

If `src/.editorconfig` changed, format the whole `src` tree:

```bash
dotnet format --no-restore
```

## Choose the Smallest Valid Verification Loop

- For repository metadata or prose-only changes outside the build inputs, such
  as `README.md`, `CHANGELOG.md`, or `.github/` text-only edits: do not run
  `dotnet`.
- For component `.cs` or `.razor` changes with behavior coverage: prefer a
  single filtered `dotnet test` against
  `src/MudBlazor.UnitTests/MudBlazor.UnitTests.csproj` with
  `/p:SkipBunCompile=true`. Build
  `src/MudBlazor.UnitTests/MudBlazor.UnitTests.csproj` first only when you plan
  to reuse the outputs for multiple test filters.
- For component `.cs` or `.razor` changes that only need compile validation:
  build `src/MudBlazor/MudBlazor.csproj` with `/p:SkipBunCompile=true`.
- For `TScripts` or `Styles`: run a normal scoped project build.
- For docs changes: build the relevant docs project. Avoid docs host run loops
  during agent verification.
- For docs example or API-page changes that need parity with CI, run
  `dotnet test src/MudBlazor.UnitTests.Docs/MudBlazor.UnitTests.Docs.csproj /p:GenerateDocsTests=true`.
- For analyzer or code-fix changes: prefer a single filtered `dotnet test` from
  `src/MudBlazor.UnitTests.Analyzers/MudBlazor.UnitTests.Analyzers.csproj`.
  Build that project first only when you plan multiple filtered test runs.
- Prefer the narrowest relevant test filter over running an entire test project.
- Use `dotnet clean <project.csproj>` only when incremental outputs are clearly
  stale or corrupted.
