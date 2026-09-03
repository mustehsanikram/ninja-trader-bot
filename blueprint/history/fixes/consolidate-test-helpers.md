# Fix: Consolidate duplicated test helpers

**Type:** Fix
**Status:** verified
**Fixes:** F-03, F-10

## The problem

Three different conventions now exist for the same two jobs, across five test
files.

**Swing construction.** `SwingSequenceTests.cs:11` and `TrendStateTests.cs:10`
each declare their own byte-identical `High(int, double)` and `Low(int, double)`,
along with the `private const int Strength = 3` they depend on.
`ResetTests.cs` does neither and constructs swings inline with `new Swing(...)`.

**Swing comparison.** `AssertSameSwing(Swing, Swing)` is byte-identical in
`NonRepaintingTests.cs:150` and `ResetTests.cs:28`. `ResetTests` also has
`AssertSameConfirmations`, while `NonRepaintingTests` does the same comparison
with an inline loop.

Neither is a live defect. The risk is drift: if `Swing` gains a field that
matters, one copy of `AssertSameSwing` gets updated and the other silently keeps
passing, which is the worst failure mode a test helper has. Features 3 to 6 will
each want the same helpers, so the duplication compounds per file unless it is
consolidated now.

`TestBars` already exists as the shared location and is where F-03 and F-10 both
point.

## The fix

Two shared classes rather than one, split by what they depend on.

**`TestBars`** keeps its current job, building data, and gains `High` and `Low`
plus the shared `Strength` constant. It currently imports only `System`,
`System.Collections.Generic` and `PkStructure`.

**`SwingAssert`** is new and holds `AssertSameSwing` and
`AssertSameConfirmations`.

The split exists because the assertion helpers need `Xunit` and the data builders
do not. Folding assertions into `TestBars` would pull a test framework into a
class whose whole job is constructing plain objects. Both findings say "move it
into `TestBars`"; this is the same consolidation with one extra file, and worth
the deviation.

**Must not break.** This is a pure test refactor. No file under `src/` changes,
no assertion changes meaning, and no test is removed. The suite must still report
**exactly 78 passing** afterwards. A lower number means a test was lost in the
move, which is the main risk in a mechanical change like this.

## Build steps

- [x] **Step 1 - Consolidate into TestBars and SwingAssert** - add `High`, `Low`
  and `Strength` to `TestBars`; create `SwingAssert` with `AssertSameSwing` and
  `AssertSameConfirmations`; delete every local copy and switch all five test
  files to the shared versions, including replacing `ResetTests`' inline
  `new Swing(...)` calls and `NonRepaintingTests`' inline comparison loop.
  *Done when:* `dotnet test tests/Structure.Tests` reports exactly 78 passed,
  `git diff` shows no change under `src/`, and a search for
  `private static Swing High(`, `private static Swing Low(`,
  `private static void AssertSameSwing` and `private const int Strength`
  across `tests/` returns nothing.

## Files / areas

- `tests/Structure.Tests/TestBars.cs` - gains `High`, `Low`, `Strength`
- `tests/Structure.Tests/SwingAssert.cs` - new
- `tests/Structure.Tests/SwingSequenceTests.cs` - drop local helpers
- `tests/Structure.Tests/TrendStateTests.cs` - drop local helpers
- `tests/Structure.Tests/NonRepaintingTests.cs` - drop local `AssertSameSwing`
- `tests/Structure.Tests/ResetTests.cs` - drop local helpers, use shared builders

No change to `src/`.

## Verify

```
dotnet test tests/Structure.Tests
```

Exactly 78 passing, the same count as before the refactor. `git diff --stat src/`
must be empty.

## Notes for the AI

- Conservative C# only. `LangVersion` is pinned to 6.
- Do not change what any assertion checks. Moving an assertion is in scope;
  strengthening or weakening one is not, and would hide whether the move was
  faithful.
- Keep `TestBars` free of `Xunit`. That separation is the reason for the second
  class.
- `StructurePipelineTests` compares its own `LabelledSwing` type, not `Swing`.
  Leave its local helpers alone; they are not duplicates.
- No em dashes in code, comments, or commit messages.

## Findings

### consolidate-test-helpers/F-03 [P2] closed - Swing test helpers duplicated across two test files

**File:** tests/Structure.Tests/TrendStateTests.cs:10
**Found:** 2026-08-31 by /audit (scope: full; lens: quality)
**Why it matters:** `High(int, double)` and `Low(int, double)` are byte identical
in `SwingSequenceTests.cs:11` and `TrendStateTests.cs:10`, including the private
`Strength` constant they depend on. `TestBars` already exists as the shared helper
location. Features 3 to 6 will each want the same two helpers, so this duplicates
again on every future test file unless it is moved now.

**Suggested fix:** Move both into `TestBars` alongside `Make` and `Feed`, and
delete the local copies.
**Resolution:** Fixed in `fix/consolidate-test-helpers`. `High`, `Low` and the
shared `Strength` constant now live in `TestBars`; the local copies in
`SwingSequenceTests` and `TrendStateTests` are gone, and `ResetTests` uses the
shared builders instead of inline `new Swing(...)`, so all three files now share
one convention. Suite still reports exactly 78 passing and `git diff src/` is
empty, so nothing was lost or changed in behaviour.

Re-reviewed 2026-09-03 by /audit (scope: current) and closed. Searches for
`private static Swing High(`, `private static Swing Low(` and
`private const int Strength` across `tests/` return nothing. The call-site
rewrite touched only helper invocations: `LastHigh`, `LastLow`, `PivotHigh` and
`PivotLow` are untouched in the diff. No new defect introduced by this half of
the repair.
