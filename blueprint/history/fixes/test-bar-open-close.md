# Fix: Test bars can carry a real open and close

**Type:** Fix
**Status:** verified
**Fixes:** F-02

## The problem

`TestBars.Make(high, low)` builds every bar the same way:

```csharp
bar.Open  = low;
bar.High  = high;
bar.Low   = low;
bar.Close = high;
```

So every bar in every test is a full range bar whose close sits exactly on its
high. Harmless for features 1a and 2a, because `PivotDetector` reads only `High`
and `Low`, and nothing reads `Open` or `Close` anywhere in `src/` today.

It becomes a trap at feature 3. The structure definitions in
`project-overview.md` confirm a break when a bar **closes** beyond a level, and
record wick-only penetration as a **sweep** rather than a break. That distinction
is what the non-repainting claim rests on.

A helper where close always equals high cannot express the difference at all. A
test for "price wicked above the level but closed below it" is impossible to
write with it, and worse, a test for "price closed above the level" would pass
for the wrong reason, because the close is pinned to the extreme the test is
checking against.

## The fix

Two changes to `TestBars`.

**1. Add a four argument overload** so a test can state a real bar:

```csharp
public static Bar Make(double open, double high, double low, double close)
```

**2. Move the two argument form's open and close to the midpoint** of high and
low, instead of pinning them to the extremes.

The second change is not in the recorded finding, and it is the more important
one. Leaving `Close = high` means the default quietly *satisfies* close-based
conditions. A feature 3 test that forgets to use the four argument form would
still pass its "closed above the level" assertion, and the gap would be invisible.
A midpoint close satisfies nothing, so a test that needs a specific close has to
say so.

**Must not break.** Nothing in `src/` reads `Open` or `Close`, so changing the
two argument form's values cannot alter any existing behaviour. The suite must
still report **exactly 78 passing**, and that count is the proof: if a test does
depend on `Close == High`, it fails here rather than silently later.

**Out of scope.** `BuildSeries` still emits only highs and lows. Feature 3 will
need closes across a realistic series, but what those closes should look like
depends on what feature 3 is testing, so it belongs there rather than being
guessed at now.

## Build steps

- [x] **Step 1 - Real open and close on test bars** - add the four argument
  `Make` overload, repoint the two argument form at the midpoint, and add tests
  pinning both behaviours.
  *Done when:* `dotnet test tests/Structure.Tests` reports exactly 78 existing
  tests still passing plus the new ones, `git diff --stat src/` is empty, and
  the new tests assert that
  - the four argument form preserves all four values exactly
  - the two argument form produces a close strictly between low and high, so it
    can never accidentally satisfy a close-beyond-a-level condition

## Files / areas

- `tests/Structure.Tests/TestBars.cs` - overload plus the midpoint change
- `tests/Structure.Tests/TestBarsTests.cs` - new

No change to `src/`. No existing test file needs editing, since every current
caller uses the two argument form and none reads `Open` or `Close`.

## Verify

```
dotnet test tests/Structure.Tests
```

The 78 existing tests must all still pass. `git diff --stat src/` must be empty.

## Notes for the AI

- Conservative C# only. `LangVersion` is pinned to 6.
- Keep `TestBars` free of `Xunit`; the new tests go in their own file, and
  assertions that compare swings still belong in `SwingAssert`.
- Do not change `BuildSeries`. See Out of scope.
- Do not touch any existing test file. If one needs editing, the midpoint change
  broke an assumption and that is worth stopping to discuss rather than patching.
- No em dashes in code, comments, or commit messages.

## Findings

### test-bar-open-close/F-02 [P2] closed - Shared bar helper hardcodes Close equal to High

**File:** tests/Structure.Tests/TestBars.cs:22
**Found:** 2026-08-31 by /audit (scope: full; lens: tests)
**Why it matters:** `TestBars.Make` sets `Open = low` and `Close = high`, so every
bar in every test is a full range bar whose close sits exactly on its high. That
is harmless for features 1a and 2a, which only read `High` and `Low`.

It becomes a trap at feature 3. The structure definitions confirm a break when a
bar **closes** beyond a level, and record wick-only penetration as a sweep rather
than a break. A test series where close always equals high cannot distinguish
those two cases at all, so tests written against this helper would pass while the
close versus wick logic went unexercised. That distinction is what the
non-repainting claim rests on.

**Suggested fix:** Add a four argument overload taking open, high, low and close,
and keep the two argument form for pivot tests. Do this before feature 3 is
spec'd, not during it.
**Resolution:** Fixed in `fix/test-bar-open-close`, with one change beyond the
suggestion. The four argument overload exists as recorded. The two argument form
also stopped pinning open and close to the extremes and now puts both at the
midpoint, because leaving `Close = high` would have let a feature 3 test pass a
"closed beyond the level" assertion for the wrong reason, which is the failure
this finding exists to prevent.

Nothing under `src/` reads `Open` or `Close`, so the change could not alter
behaviour, and no existing test file needed editing. 78 existing tests still pass
alongside 6 new ones. Verified enforced, not decorative: restoring the old
`Close = high` behaviour failed 3 tests.

Re-reviewed 2026-09-03 by /audit (scope: current) and closed. Both `Make`
overloads read as intended, the two argument form still reports `High` and `Low`
exactly so `PivotDetector` is unaffected, and no caller outside the new tests
reads `Open` or `Close`. The original defect is gone for every input any caller
uses. One narrower case survives and is recorded separately as F-12.
