# Findings

> **Generated file.** The findings ledger: review findings raised by `/audit`
> against the work in progress, each with a durable ID, severity (P0-P3), and
> status. `/implement` marks repaired findings `fixed`, a later `/audit` pass
> moves them to `closed`, and `/complete` refuses to merge while any P0 or P1
> finding is `open` or `fixed`, then archives resolved findings with the work
> and resets this file.

### F-02 [P2] open - Shared bar helper hardcodes Close equal to High

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
**Resolution:**

### F-04 [P3] open - Bar.Time is written but never read, and no session logic exists

**File:** src/Structure/Bar.cs:7
**Found:** 2026-08-31 by /audit (scope: full; lens: quality)
**Why it matters:** `Bar.Time` is set by `TestBars.Make` to a constant and read
nowhere in `src/` or `tests/`. It is not dead code, since it is part of the locked
`Bar` contract and NinjaTrader will populate it, but nothing exercises it.

The absence points at a real missing capability rather than an unused field: the
detector has no concept of a session boundary. A pivot window spanning an
overnight gap compares bars hours apart as though they were adjacent, and `Time`
is the field that would let it know better. On NQ, with a daily maintenance break,
this affects the first `SwingStrength` bars of every session.

**Suggested fix:** Do not remove the field. Decide whether session awareness is in
scope for the product, and if so add it to the build plan as its own item.
**Resolution:**

### F-05 [P3] open - Bar is a mutable struct

**File:** src/Structure/Bar.cs:5
**Found:** 2026-08-31 by /audit (scope: full; lens: quality)
**Why it matters:** `Bar` is a struct with settable auto properties. Struct
assignment copies, so mutating a `Bar` read out of a collection changes the copy
and not the stored value, silently. No live defect exists because the only
consumer, `PivotDetector`, reads a copy and never writes to it.

`readonly struct` with a constructor would remove the risk outright, but it needs
C# 7.2 and `LangVersion` is pinned to 6 pending the NinjaTrader check.

**Suggested fix:** Revisit once the real NT8 language version is confirmed. If it
allows 7.2 or later, make `Bar` a `readonly struct` with a constructor.
**Resolution:**

### F-06 [unverified] - Exact double comparison assumes tick aligned prices

**File:** src/Structure/SwingSequence.cs:52
**Found:** 2026-08-31 by /audit (scope: full; lens: quality)
**Why it matters:** Both `Classify` and `PivotDetector` compare prices with `>`,
`<` and implicit equality on `double`. The comment argues this is safe because
prices are discrete tick values. That holds for a clean feed, but has never been
tested against real market data, and providers differ in how they round and
adjust. If a price arrives a fraction off a tick boundary, an intended equality
silently becomes a strict inequality and a double top is labelled HH.

**Suggested fix:** Validate against a real NQ series before deciding. If drift
appears, compare on a tick-rounded value rather than adding an epsilon.
**Resolution:**

### F-07 [unverified] - Equal price rule may leave trend undetermined far more often on real data

**File:** src/Structure/SwingSequence.cs:56
**Found:** 2026-08-31 by /audit (scope: full; lens: quality)
**Why it matters:** Equal consecutive same kind swings produce `Undetermined`,
and `State` requires both sides to carry a definite label. On the synthetic sine
series exact equality effectively never occurs, so the tests never see the
compound effect. Real NQ repeatedly tests the same round numbers, so equal swing
prices are common rather than exceptional. Trend state could sit undetermined
through long stretches of normal trading.

**Suggested fix:** Measure the label and state distribution over a real NQ series
before changing anything. The rule may be correct; it is simply unmeasured.
**Resolution:**

### F-08 [unverified] - Whole engine validated only against a synthetic series

**File:** tests/Structure.Tests/TestBars.cs:66
**Found:** 2026-08-31 by /audit (scope: full; lens: tests)
**Why it matters:** Every test that exercises a realistic series derives its bars
from `BuildSeries`, two summed
sine functions. It is smooth and continuous, with no gaps, no repeated prices, no
session breaks, no spikes and no flat periods. The suite proves the engine is
internally consistent and does not repaint. It cannot say anything about how the
engine behaves on the input it was built for.

F-02, F-04, F-06 and F-07 are all specific instances of this one gap.

**Suggested fix:** Obtain a historical NQ minute or tick CSV and run the existing
pipeline over it, reporting swing count, label distribution and trend state
distribution. This needs no NinjaTrader install and no new production logic.
**Resolution:**

### F-09 [unverified] - LangVersion 6 pin is an untested guess

**File:** tests/Structure.Tests/Structure.Tests.csproj:9
**Found:** 2026-08-31 by /audit (scope: full; lens: quality)
**Why it matters:** The pin exists so syntax NinjaTrader cannot accept fails at
build. The value was chosen conservatively without access to NinjaTrader, which is
not installed. If the real cap is lower than 6 the guard does not guard; if it is
higher, the codebase is needlessly restricted and F-05 stays unfixable.

**Suggested fix:** Confirm at first compile inside NinjaTrader and set the pin to
the real value.
**Resolution:**

### F-10 [P2] open - Swing assertion helper duplicated across two test files

**File:** tests/Structure.Tests/ResetTests.cs:28
**Found:** 2026-08-31 by /audit (scope: current; lens: quality)
**Why it matters:** `AssertSameSwing(Swing, Swing)` now exists in near identical
form in `NonRepaintingTests.cs:150` and `ResetTests.cs:28`. Both compare `Index`,
`Price`, `Kind` and `ConfirmedAtIndex` and both treat null as expected-null.

Introduced by the `reset-and-bar-index/F-01` repair, so this is new rather than
pre-existing. It is a
second instance of the pattern already recorded in `consolidate-test-helpers/F-03`: shared test helpers are
being written locally per file instead of in `TestBars`. The risk is drift. If
`Swing` gains a field that matters, one copy gets updated and the other silently
keeps passing.

`ResetTests.cs` also constructs swings inline with `new Swing(...)` while
`SwingSequenceTests.cs` and `TrendStateTests.cs` use their own local `High()` and
`Low()` helpers, so there are now three conventions for the same job.

**Suggested fix:** Move `AssertSameSwing`, `AssertSameConfirmations`, `High` and
`Low` into `TestBars` and delete every local copy. Fix alongside `consolidate-test-helpers/F-03`, since it
is the same cleanup.
**Resolution:** Fixed in `fix/consolidate-test-helpers`, alongside `consolidate-test-helpers/F-03`, but into
a new `SwingAssert` class rather than `TestBars` as suggested. `TestBars` had no
`Xunit` reference and its job is building data; folding assertions in would have
pulled a test framework into it. `SwingAssert.SameSwing` and
`SwingAssert.SameConfirmations` now serve `NonRepaintingTests` and `ResetTests`,
and both local copies are gone. Verified reached, not bypassed: weakening
`SameSwing` to compare `Index + 1` failed 8 tests.

Re-reviewed 2026-09-03 by /audit (scope: current). **Repair is incomplete, so
status returns to `open` rather than closing.** The `AssertSameSwing` half is
genuinely resolved. The confirmation-comparison half is not:
`NonRepaintingTests.cs:47-55` still carries an inline loop that is line for line
identical to `SwingAssert.SameConfirmations`, including the `Assert.Equal` on
`Count`, the `BarIndex` comparison and both `SameSwing` calls. The repair swapped
the inner assertion for the shared one but left the surrounding loop in place, so
the duplication this finding names still exists in a different shape.

Remaining work: replace that loop with a single
`SwingAssert.SameConfirmations(expected, prefix)` call.
`StructurePipelineTests` has a similar loop but over its own `LabelledSwing`
type, not `Confirmation`, so it is correctly out of scope.

### F-11 [P2] open - A gapped bar index throws into the platform

**File:** src/Structure/PivotDetector.cs:57
**Found:** 2026-08-31 by /audit (scope: current; lens: quality)
**Why it matters:** `OnBar` throws `ArgumentOutOfRangeException` when `barIndex`
is anything other than the next one. That covers going backwards, which the fix
spec asked for, and also a forward gap, which it did not.

Throwing is the right instinct for a product whose central claim is that its
output can be trusted: silently accepting a gap would corrupt the pivot
arithmetic, because the ring buffer assumes its bars are adjacent. But the
exception surfaces inside `OnBarUpdate` once feature 1b exists, and an unhandled
exception there disables the indicator on the user's chart and writes to the NT8
log. For a paid product that is a hard failure mode reached through a condition
nobody has yet proven cannot occur.

Whether NinjaTrader can ever deliver a non-contiguous `CurrentBar` is unknown
without the platform installed. Candidate paths worth checking: historical bar
revision on reload, `BarsRequiredToPlot`, and switching a chart's data series.

**Suggested fix:** Decide in the 1b spec how the adapter handles it. Catching the
exception and calling `Reset()` is one option; proving the gap cannot occur and
leaving the throw as a contract assertion is another. Do not remove the guard and
let a gap through silently.
**Resolution:**
