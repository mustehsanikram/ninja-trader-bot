# Fix: Reset and bar-index safety in the structure core

**Type:** Fix
**Status:** verified
**Fixes:** F-01

## The problem

From the ledger entry F-01, plus one related defect found while scoping it.

**1. No way to clear state.** Neither `PivotDetector` nor `SwingSequence` has a
`Reset()`. NinjaTrader re-initialises indicators on reload, parameter change and
data refresh, so feature 1b has no way to start clean short of constructing new
instances.

**2. A repeated bar corrupts the window.** `PivotDetector.OnBar`
(`src/Structure/PivotDetector.cs:38`) unconditionally writes to the ring buffer,
advances `_writeIndex` and increments `_barsSeen`. NinjaScript calls
`OnBarUpdate` once per bar under `Calculate.OnBarClose`, but repeatedly for the
forming bar under `Calculate.OnEachTick` and `Calculate.OnPriceChange`. Feeding
those calls straight through double counts and shifts every later
`candidateIndex` off the true bar index.

**3. Bar indices assume the feed starts at zero.** `candidateIndex` is derived
from `_barsSeen`, so the first bar fed is treated as index 0. NinjaScript
indicators do not necessarily process from bar 0, and `CurrentBar` is the real
index. Today every emitted `Swing.Index` and `ConfirmedAtIndex` would be offset
by however many bars the platform skipped. This was not in F-01; it surfaced
while scoping the repeat guard, and the same change fixes both.

## The fix

Give `OnBar` the bar's real index instead of counting internally, and ignore a
repeat of the index already seen.

```csharp
public PivotResult OnBar(int barIndex, Bar bar)
```

That single change covers problems 2 and 3: a repeated `OnBarUpdate` call arrives
with the same `barIndex` and is ignored, and pivot indices become the platform's
indices rather than an internal count.

Then add `Reset()` to both classes for problem 1.

**Why the guard goes in the core rather than the 1b adapter.** The adapter could
gate on `IsFirstTickOfBar` and keep the core unaware. That was the original
suggestion in F-01. Putting it in the core is better here because it is testable
today with no NinjaTrader present, it protects against any caller rather than one
correct caller, and it fixes problem 3 at the same time, which an adapter-side
guard would not. An `int` parameter is not a NinjaTrader type, so the isolation
rule still holds.

**Must not break.** The non-repainting guarantee, the strict pivot comparison, the
plateau rule, or any existing label behaviour. All 62 existing tests must still
pass, adjusted only where the `OnBar` signature changed.

## Build steps

- [x] **Step 1 - OnBar takes an explicit bar index** - change the signature to
  `OnBar(int barIndex, Bar bar)`, derive `candidateIndex` from it, ignore a call
  whose `barIndex` matches the previous one, and reject one that goes backwards.
  Update the existing tests and `TestBars.Feed` for the new signature.
  *Done when:* the full suite passes, and new tests show that feeding the same
  bar index twice produces the same result as feeding it once, that a series
  starting at a non-zero index reports pivot indices in that same numbering, and
  that a backwards index is rejected.

- [x] **Step 2 - Reset on both classes** - `PivotDetector.Reset()` clears the ring
  buffer and counters; `SwingSequence.Reset()` clears `LastHigh` and `LastLow`.
  *Done when:* tests show a detector run, reset, then run again over the same
  series produces identical results to a fresh instance, and the same for
  `SwingSequence`.

## Files / areas

- `src/Structure/PivotDetector.cs` - signature, index handling, `Reset()`
- `src/Structure/SwingSequence.cs` - `Reset()`
- `tests/Structure.Tests/TestBars.cs` - `Feed` passes the index
- `tests/Structure.Tests/PivotDetectorTests.cs` - signature
- `tests/Structure.Tests/NonRepaintingTests.cs` - signature
- `tests/Structure.Tests/StructurePipelineTests.cs` - signature
- `tests/Structure.Tests/BarIndexTests.cs` - new
- `tests/Structure.Tests/ResetTests.cs` - new

## Contract change

`OnBar` is a **load-bearing signature change** to code shipped in feature 1a.
Nothing consumes it yet, since feature 1b does not exist, so the cost is limited
to this repo's tests. It will be far more expensive after the adapter is written.

`Swing.Index` and `ConfirmedAtIndex` keep their meaning but now carry the
caller's numbering rather than an internal count. For a caller that starts at
zero and feeds every bar, the values are unchanged.

## Verify

```
dotnet test tests/Structure.Tests
```

All 62 existing tests pass, plus the new index and reset tests. The mutation
checks from 1a and 2a should still fail the suite if reapplied, in particular the
off-by-one in the confirmation index.

## Notes for the AI

- Conservative C# only. `LangVersion` is pinned to 6 and NinjaTrader compiles this
  source.
- No NinjaTrader types in `src/Structure/`. An `int` index is fine.
- No allocation in the per-bar path. The repeat check is one comparison.
- Ignoring a repeat means returning an empty `PivotResult`, not reprocessing.
  Re-emitting an already reported pivot would be a repaint.
- Do not change the pivot rule, the plateau rule, or any labelling behaviour.
  This fix is about when and how bars arrive, nothing else.
- No em dashes in code, comments, or commit messages.

## Findings

### reset-and-bar-index/F-01 [P1] closed - PivotDetector has no reset and no guard against a repeated bar

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
**Resolution:** Fixed in `fix/reset-and-bar-index`, but placed the other way
round: the guard went into the core, not the adapter. `OnBar` now takes an
explicit bar index, ignores a repeat of the index already seen, and rejects a
backwards or gapped one. `Reset()` added to both `PivotDetector` and
`SwingSequence`. The core placement also fixed a defect this finding did not
name: `candidateIndex` was derived from an internal count, so every reported
pivot index assumed the feed starts at bar zero.

Re-reviewed 2026-08-31 by /audit (scope: current) and closed. `Reset()` clears
every mutable field on both classes: `_barsSeen`, `_writeIndex`, `_lastBarIndex`
and `_hasBar` on the detector, `_lastHigh` and `_lastLow` on the sequence. The
three remaining detector fields are `readonly` construction config and correctly
untouched. Four mutations were applied and each confirmed caught: repeat guard
disabled (2 failures), `candidateIndex` reverted to the internal count (3),
`Reset` forgetting `_hasBar` (1), sequence `Reset` clearing only the high (1).
78 tests pass, build clean. The repair introduced no defect in the repaired code
itself, but did introduce F-10 and F-11 below.
