# Fix: Reject bar shapes the two argument helper cannot honour

**Type:** Fix
**Status:** verified
**Fixes:** F-12

## The problem

`TestBars.Make(high, low)` documents that it "can never accidentally satisfy a
close-beyond-a-level condition", and puts the close at the midpoint to deliver
that. The guarantee holds whenever `high > low`.

It breaks when `high == low`. A zero range bar makes the midpoint equal to both
extremes, so `Close == High`, which is exactly the state
`test-bar-open-close/F-02` was raised to remove. The three theory cases pinning
the strictly-between property all use `high > low`, so nothing catches it.

No live defect: no caller passes a zero range bar, and `BuildSeries` always
produces a range of 2.0. The exposure is future, and feature 3 is the likely
place to hit it, since a bar with a single trade is a real market shape and an
obvious edge case to test a break against.

**A second gap found while scoping this.** The two argument form also accepts
`high < low`, silently producing a bar whose high sits below its low. That is not
a bar at all. `PivotDetector` would compare the values as given and report
nonsense, with nothing anywhere to say the input was impossible. This is not in
F-12; the same guard closes both.

## The fix

Validate at the boundary, and make each rule state why it exists.

**Two argument form:** reject `high <= low`. A zero range bar cannot get a
midpoint close strictly inside its range, so rather than silently breaking the
guarantee, the helper refuses and points at the four argument overload, which
handles that shape honestly.

**Four argument form:** reject `high < low` only. `high == low` is allowed and is
the supported way to build a flat bar, with the caller stating the close
explicitly rather than inheriting a misleading default.

**Deliberately not validated:** whether `open` and `close` fall inside the
range on the four argument form. That form is the escape hatch for a caller who
knows the shape it wants, and a deliberately impossible bar may be useful for
testing robustness later. Worth revisiting only if it causes a real problem.

**Must not break.** No existing caller passes `high <= low`, so the 84 current
tests must all still pass untouched. If one fails, a test was relying on a bar
shape the helper should never have accepted, which is worth stopping to discuss
rather than patching around.

## Build steps

- [x] **Step 1 - Guard both overloads** - add the two guards with messages that
  name the reason, and tests pinning each.
  *Done when:* `dotnet test tests/Structure.Tests` reports the 84 existing tests
  still passing plus the new ones, `git diff --stat src/` is empty, no existing
  test file is edited, and new tests show that
  - the two argument form throws for `high == low` and for `high < low`
  - the four argument form accepts `high == low` and preserves the close given
  - the four argument form throws for `high < low`
  - the strictly-between guarantee now holds for every input the two argument
    form accepts, since the only counterexample is rejected

## Files / areas

- `tests/Structure.Tests/TestBars.cs` - two guard clauses
- `tests/Structure.Tests/TestBarsTests.cs` - tests for both

No change to `src/`. No existing test file should need editing.

## Verify

```
dotnet test tests/Structure.Tests
```

All 84 existing tests pass. `git diff --stat src/` empty.

## Notes for the AI

- Conservative C# only. `LangVersion` is pinned to 6.
- Throw `ArgumentOutOfRangeException` with a message naming the parameter and the
  reason, matching how `PivotDetector` and `Swing` already guard their inputs.
- The two argument message should point the caller at the four argument overload,
  since that is the fix rather than a dead end.
- Update the two argument form's doc comment: the guarantee is now unconditional
  for accepted input, which is the whole point of the guard.
- Do not add validation to `open` or `close`. See Deliberately not validated.
- No em dashes in code, comments, or commit messages.

## Findings

### reject-degenerate-bars/F-12 [P3] closed - Zero range bar defeats the midpoint close guarantee

**File:** tests/Structure.Tests/TestBars.cs:22
**Found:** 2026-09-03 by /audit (scope: current; lens: tests)
**Why it matters:** The two argument `Make` documents that it "can never
accidentally satisfy a close-beyond-a-level condition". That holds whenever
`high > low`, because the midpoint sits strictly inside the range.

It does not hold when `high == low`. A zero range bar makes the midpoint equal to
both extremes, so `Close == High`, which is exactly the state
`test-bar-open-close/F-02` was raised to remove. The doc comment therefore overclaims, and the three theory cases pinning
the strictly-between property all use `high > low`, so nothing catches it.

No live defect: no caller passes a zero range bar, and `BuildSeries` always
produces a range of 2.0. The exposure is future. A zero range bar is a real
market shape, a bar with a single trade, and feature 3 is exactly the kind of work
that would reach for one.

**Suggested fix:** Either reject `high == low` in the two argument form and make
callers use the four argument overload for that shape, or soften the doc comment
to state the precondition. Rejecting is preferable: it keeps the guarantee
absolute rather than conditional, and the four argument form already covers the
case properly.
**Resolution:** Fixed in `fix/reject-degenerate-bars` by rejecting. The two
argument form now throws `ArgumentOutOfRangeException` for `high <= low`, with a
message pointing at the four argument overload. That overload rejects `high < low`
only, so `high == low` is the supported way to build a flat bar with the close
stated explicitly. The doc comment no longer claims a conditional guarantee.

Scoping also closed a gap the finding did not name: the two argument form
previously accepted `high < low`, producing a bar whose high sat below its low
with nothing to say the input was impossible.

84 existing tests pass alongside 5 new ones. Three mutations, each verified
present in source before running: removing the two argument guard failed 2,
weakening it to let zero range through failed 2, removing the four argument guard
failed 1. Note from the first mutation: the inverted case is also caught by the
four argument guard downstream, so that one theory case does not isolate the two
argument guard. Behaviour is correct either way, but the coverage is narrower than
the test name suggests.

Re-reviewed 2026-09-07 by /audit (scope: current) and closed. The two argument
form rejects `high <= low`, so no zero range bar can reach the midpoint path and
the guarantee is now unconditional for accepted input. The four argument form
rejects `high < low` only, so a flat bar is still constructible with the close
stated. Both doc comments match the code. 89 tests pass, build clean, and only
the two files the spec names were touched.

One note for the archive: this spec's done-when said "no existing test file is
edited" while its own Files section listed `TestBarsTests.cs` as a file to
change. `TestBarsTests.cs` existed before this fix, so the criterion as written
was not met, though the intent, leaving unrelated test files alone, was. The
wording should have been "no test file other than the two named". Not a code
defect.
