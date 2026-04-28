# AGENTS.md - AI Coding Agent Guide for MudBlazor

This file is the short entry point for coding agents. Keep it compact and use the
linked reference docs for detailed rules.

## Start Here

- Keep changes focused and target only the projects relevant to the task.
- Do not run solution-wide commands unless explicitly requested.
- Follow `src/.editorconfig`; warnings are errors.
- Do not add new heavy dependencies or packages without approval.
- Prefer targeted, non-breaking changes. If broader follow-up work is useful,
  suggest it separately instead of expanding the current diff.
- Do not make speculative large changes when intent is unclear. Ask a clarifying
  question or propose a short plan.
- Treat `bin/`, `obj/`, `TestResults/`, generated files, and similar outputs as
  build artifacts unless the task explicitly targets them.

## Reference Docs

Read the smallest relevant set before editing:

- [Repo map](docs/agent/repo-map.md): project layout, target projects, SDK requirements.
- [Workflows](docs/agent/workflows.md): restore, build, test, Bun, formatting, and scoped verification.
- [Component authoring](docs/agent/component-authoring.md): parameters, `ParameterState<T>`, styling, accessibility, public API docs.
- [Docs pages](docs/agent/docs-pages.md): component docs pages and example rules.
- [Testing](docs/agent/testing.md): bUnit rules, test naming, test locations, generated docs tests.
- [Conventions](docs/agent/conventions.md): compatibility, analyzer rules, comments, and final checklist.

## Common Routing

- Component library changes usually touch `src/MudBlazor/` and validate through
  `src/MudBlazor.UnitTests/MudBlazor.UnitTests.csproj`.
- Docs changes usually touch `src/MudBlazor.Docs*` and may require
  `src/MudBlazor.UnitTests.Docs/MudBlazor.UnitTests.Docs.csproj` when generated
  docs tests are relevant.
- Analyzer and code-fix changes usually touch `src/MudBlazor.Analyzers*` and
  validate through `src/MudBlazor.UnitTests.Analyzers/MudBlazor.UnitTests.Analyzers.csproj`.
- Asset-pipeline, `TScripts`, style, CSS, SCSS, `src/package.json`, and
  `src/bun.lock` changes need the normal scoped build path; do not skip Bun for
  those.

## Verification Principles

- Reuse existing restore assets. Restore only when restore inputs changed, when
  assets are missing, or when a `--no-restore` build/test shows stale restore
  state.
- Prefer the narrowest valid `dotnet build` or `dotnet test` command for the
  files changed.
- For C# or Razor component behavior changes, prefer one filtered
  `dotnet test` against `src/MudBlazor.UnitTests/MudBlazor.UnitTests.csproj`.
- For documentation-only or repository-metadata-only changes outside build
  inputs, do not run `dotnet`.
- Run `dotnet format whitespace --no-restore --include <changed files>` from
  `src/` once at the end when source files under `src/` changed.

## Component Changes

- Component parameters must be auto-properties only.
- Do not overwrite component parameters directly. Use `ParameterState<T>` and
  update through `.Value` or `SetValueAsync`.
- Use `CssBuilder` for classes and styles, and use CSS variables/design tokens
  instead of hard-coded colors.
- Add XML summaries and appropriate `[Category(...)]` attributes for public
  component parameters.
- Component behavior changes require focused bUnit coverage and docs updates
  when public behavior or public API changes.

## Testing Rules

- Run the narrowest relevant test filter first.
- Prefer fail-first tests for behavior fixes.
- Never cache bUnit `Find()` or `FindAll()` results; re-query after interactions.
- Use async bUnit interactions such as `ClickAsync`, `ChangeAsync`, `BlurAsync`,
  and `InputAsync`.
- Test method names must not use `Test` or `Async` suffixes, must not contain
  `Test_`, and must not end with trailing underscores.

## Before Finishing

- Confirm formatting expectations were met for changed files.
- Confirm the relevant target project builds/tests cleanly when code, docs app,
  analyzer, or asset inputs changed.
- Confirm tests were updated and run when behavior changed.
- Confirm docs were updated when component behavior or public API changed.
- Confirm no new dependencies were added without approval.
