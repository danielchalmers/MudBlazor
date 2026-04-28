# Testing Rules

## General Testing Guidance

- Run the narrowest relevant test filter first.
- Test logic rather than full HTML snapshots.
- Prefer a fail-first workflow: add or update the test to fail for the target
  behavior before implementing the fix.
- Keep tests isolated so they can run in parallel.
- If a test modifies shared or static state, restore it in `[TearDown]`.
- Use `[NonParallelizable]` only when isolation is not feasible.
- Prefer `TimeProvider` or `FakeTimeProvider` over `Task.Delay`.

## bUnit Rules

- Never cache `Find()` or `FindAll()` results. Re-query after interactions.
- Always use `InvokeAsync()` for parameter changes or method calls.
- Prefer async interactions such as `ClickAsync`, `ChangeAsync`, `BlurAsync`,
  and `InputAsync` over sync methods.

## Test Locations and Naming

- Test components belong in
  `src/MudBlazor.UnitTests.Viewer/TestComponents/<ComponentName>/`.
- Keep viewer test component file names at 40 characters or fewer. Prefer
  concise scenario names over long descriptive file names.
- Unit tests belong in
  `src/MudBlazor.UnitTests/Components/<ComponentName>Tests.cs`.
- Add a viewer test component only when the scenario is too cumbersome to
  express directly in bUnit C# syntax. In those cases, add the viewer component
  first, then the unit test.
- Test methods should be self-documenting and should not use XML documentation.
- Helper methods in test classes should include XML documentation when they are
  non-trivial or reused.
- When adding a test for a known issue, reference the issue number in the test
  name or nearby context for traceability.
- Test names must not use `Test` or `Async` suffixes, must not contain `Test_`
  in the middle, and must not end with trailing underscores.
- Reference tests: `TextTests.cs`, `ApiMemberTableTests.cs`.
