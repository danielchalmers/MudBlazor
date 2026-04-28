# Agent Conventions

## Breaking Changes and Compatibility

- Avoid breaking changes whenever possible.
- Prefer additive APIs, safe defaults, or obsoleting old behavior while keeping
  the current PR scoped to the requested fix or feature.
- If a breaking change is required, call it out explicitly in the PR description
  and update docs and tests accordingly.
- For parameter renames or removals, consider `[Obsolete]` with a clear message
  and migration path.

## Code Style and Analyzer Rules

- Fix new warnings instead of suppressing them.
- Comments should usually explain why a decision exists, not restate what the
  code already shows or describe straightforward mechanics.
- Keep `src/MudBlazor/TScripts/entrypoint.js` in sync with files in
  `src/MudBlazor/TScripts/` except `entrypoint.js`.

## Change Checklist

Before finishing, verify all of the following:

- Formatting was run for relevant changed files.
- The relevant target project builds cleanly with no new warnings when code,
  docs app, analyzer, or asset inputs changed.
- Tests were updated and run when behavior changed.
- Docs were updated when component behavior or public API changed.
- No new dependencies were added without approval.
