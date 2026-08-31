# Findings

> **Generated file.** The findings ledger: review findings raised by `/audit`
> against the work in progress, each with a durable ID, severity (P0-P3), and
> status. `/implement` marks repaired findings `fixed`, a later `/audit` pass
> moves them to `closed`, and `/complete` refuses to merge while any P0 or P1
> finding is `open` or `fixed`, then archives resolved findings with the work
> and resets this file.

### F-01 [P1] open - PivotDetector has no reset and no guard against a repeated bar

**File:** src/Structure/PivotDetector.cs:38
**Found:** 2026-08-31 by /audit (scope: full; lens: quality)
**Why it matters:** `OnBar` unconditionally writes to the ring buffer, advances
`_writeIndex` and increments `_barsSeen`. Feeding the same bar twice therefore
occupies two slots and double counts, which shifts every subsequent
`candidateIndex` away from the true bar index. There is no `Reset()` on either
`PivotDetector` or `SwingSequence`, so accumulated state cannot be cleared.

Both matter for feature 1b specifically. NinjaScript calls `OnBarUpdate` once per
bar under `Calculate.OnBarClose`, but repeatedly for the forming bar under
`Calculate.OnEachTick` and `Calculate.OnPriceChange`. NinjaTrader also
re-initialises indicators on reload, parameter change and data refresh. The core
currently has no way to survive either.

This is also the historical versus realtime divergence class: historical
processing is bar by bar regardless of the `Calculate` setting, so the same code
would be fed once per bar in Strategy Analyzer and many times per bar live.

**Suggested fix:** Add `Reset()` to `PivotDetector` and `SwingSequence`. Decide in
the 1b spec where the repeat guard belongs: the adapter gating on
`IsFirstTickOfBar`, or `OnBar` taking a bar index and ignoring a repeat. The
adapter-side guard is simpler and keeps the core free of platform assumptions.
**Resolution:**

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

### F-03 [P2] open - Swing test helpers duplicated across two test files

**File:** tests/Structure.Tests/TrendStateTests.cs:10
**Found:** 2026-08-31 by /audit (scope: full; lens: quality)
**Why it matters:** `High(int, double)` and `Low(int, double)` are byte identical
in `SwingSequenceTests.cs:11` and `TrendStateTests.cs:10`, including the private
`Strength` constant they depend on. `TestBars` already exists as the shared helper
location. Features 3 to 6 will each want the same two helpers, so this duplicates
again on every future test file unless it is moved now.

**Suggested fix:** Move both into `TestBars` alongside `Make` and `Feed`, and
delete the local copies.
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
**Why it matters:** All 62 tests derive their bars from `BuildSeries`, two summed
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
