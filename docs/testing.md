# Testing Rules

## General Testing Guidance

- Run the narrowest relevant test filter first.
- Test logic rather than full HTML snapshots.
- Tests use NUnit and AwesomeAssertions. Follow the assertion style already used
  in the file you are editing.
- Prefer a fail-first workflow: add or update the test to fail for the target
  behavior before implementing the fix.
- Keep tests isolated so they can run in parallel.
- If a test modifies shared or static state, restore it in `[TearDown]`.
- Use `[NonParallelizable]` only when isolation is not feasible.
- Prefer `TimeProvider` or `FakeTimeProvider` over `Task.Delay`.

## bUnit Rules

- Do not rely on cached `Find()` or `FindAll()` results across renders. Re-query
  after interactions or parameter changes.
- Always use `InvokeAsync()` for parameter changes or method calls.
- Prefer async interactions such as `ClickAsync`, `ChangeAsync`, `BlurAsync`,
  and `InputAsync` in new or modified tests. Existing tests still contain sync
  bUnit calls; do not churn unrelated tests just to modernize them.

## Test Locations and Naming

- Test components belong in
  `src/MudBlazor.UnitTests.Viewer/TestComponents/<ComponentName>/`.
- Prefer viewer test component file names at 40 characters or fewer. Existing
  files have some longer legacy names, but new names should stay concise.
- Unit tests usually belong in
  `src/MudBlazor.UnitTests/Components/<ComponentName>Tests.cs`. Follow nearby
  file names when a component has an established legacy name such as
  `DrawerTest.cs` or `ButtonsTests.cs`.
- Add a viewer test component only when the scenario is too cumbersome to
  express directly in bUnit C# syntax. In those cases, add the viewer component
  first, then the unit test.
- Test methods should be self-documenting and should not use XML documentation.
- Helper methods in test classes should include XML documentation when they are
  non-trivial or reused.
- When adding a test for a known issue, reference the issue number in the test
  name or nearby context for traceability.
- Prefer descriptive behavior names for new tests. Avoid generic `Test` and
  `Async` suffixes when practical, but follow nearby legacy naming when
  extending an existing test class.
- Reference tests: `TextTests.cs`, `ApiMemberTableTests.cs`.
