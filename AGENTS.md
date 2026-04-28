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

- [Repo map](docs/repo-map.md): layout, project targets, and SDK requirements.
- [Workflows](docs/workflows.md): restore, build, test, Bun, formatting, and verification.
- [Component authoring](docs/component-authoring.md): parameters, state, styling, accessibility, and public API docs.
- [Docs pages](docs/docs-pages.md): component docs pages and example rules.
- [Testing](docs/testing.md): bUnit rules, test naming, test locations, and generated docs tests.
- [Conventions](docs/conventions.md): compatibility, analyzer rules, comments, and final checklist.

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

- New or modified component parameters should be auto-properties unless an
  existing compatibility pattern requires otherwise.
- Do not overwrite component parameters directly. Use `ParameterState<T>` and
  update through `.Value` or `SetValueAsync`.
- Use `CssBuilder` for classes, `StyleBuilder` for generated inline styles, and
  CSS variables/design tokens instead of hard-coded colors.
- Add XML summaries and appropriate `[Category(...)]` attributes for public
  component parameters.
- Component behavior changes require focused bUnit coverage and docs updates
  when public behavior or public API changes.

## Testing Rules

- Run the narrowest relevant test filter first.
- Prefer fail-first tests for behavior fixes.
- Do not rely on cached bUnit `Find()` or `FindAll()` results across renders;
  re-query after interactions.
- Prefer async bUnit interactions such as `ClickAsync`, `ChangeAsync`,
  `BlurAsync`, and `InputAsync` in new or modified tests.
- Prefer descriptive test method names. When adding new tests, avoid generic
  `Test` and `Async` suffixes unless matching nearby legacy tests.

## Before Finishing

- Confirm formatting expectations were met for changed files.
- Confirm the relevant target project builds/tests cleanly when code, docs app,
  analyzer, or asset inputs changed.
- Confirm tests were updated and run when behavior changed.
- Confirm docs were updated when component behavior or public API changed.
- Confirm no new dependencies were added without approval.
