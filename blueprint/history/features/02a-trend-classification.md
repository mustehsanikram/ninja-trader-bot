# Feature: Trend classification

**From build-plan:** feature 2a
**Status:** verified

## Goal

Turn the stream of confirmed swings from feature 1a into a read of the market:
label each swing HH, HL, LH or LL against its predecessor, and derive an overall
trend state from that sequence.

This is the second half of "what is the market doing structurally." Features 3
through 6 all need it: a Break of Structure is only meaningful relative to a
trend direction, and a change of character is defined as the first break against
it. Nothing here touches NinjaTrader, so it is buildable and provable today.

## In scope

- `TrendState` enum
- `SwingSequence`: accepts confirmed swings in order, sets each one's `Label`,
  and exposes the current trend state and the most recent swing of each kind
- The equal-price rule, decided below
- Tests, including an end-to-end pass from raw bars through `PivotDetector` into
  `SwingSequence`

## Out of scope

- Drawing labels on a chart. That is feature 2b and needs NT8 plus 1b.
- BOS, retest zones, failed BOS, CHoCH. Features 3 to 6. `SwingSequence` exposes
  what they will need but implements none of it.
- `StructureEvent` and `Reason` strings. Features 3 to 7.
- Any change to `PivotDetector`. It already emits what this consumes.

## Build loop

Build one step at a time, never the whole feature at once.

1. Plan mode lays out the step before any code.
2. The AI implements just that step.
3. It shows the diff (not full files); you read it and understand it.
4. You approve, then choose whether to commit a checkpoint or roll straight on.
   Checkpoints are optional; `/complete` makes the real feature-level commit at the end.

Never accept a step you haven't read. If a diff is too big to review, the step was too big, so split it.

## Build steps

- [x] **Step 1 - Label assignment** - `SwingSequence.Add(Swing)` sets the swing's
  `Label` by comparing its price to the previous swing of the same kind. Highs
  and lows are tracked independently.
  *Done when:* tests cover all six outcomes - HH, LH on the high side, HL, LL on
  the low side, `Undetermined` for the first swing of each kind, and
  `Undetermined` when two consecutive same-kind swings share a price.

- [x] **Step 2 - Trend state** - derive `State` from the two most recent labels.
  Uptrend needs the latest high labelled HH and the latest low labelled HL;
  downtrend needs LH and LL; anything else is `Undetermined`.
  *Done when:* tests cover an established uptrend, an established downtrend, a
  mixed sequence resolving to `Undetermined`, and a sequence with too few swings
  to decide.

- [x] **Step 3 - End to end over a real series** - feed the 200-bar test series
  through `PivotDetector` into `SwingSequence` and assert the whole pipeline
  behaves.
  *Done when:* over that series -
  - both `Uptrend` and `Downtrend` are reached at some point
  - every swing after the first of its kind carries a label that matches a direct
    price comparison against its predecessor
  - no swing is labelled before its `ConfirmedAtIndex`, so the 1a guarantee still
    holds through the pipeline

## Files / areas

- `src/Structure/TrendState.cs` - new
- `src/Structure/SwingSequence.cs` - new
- `tests/Structure.Tests/SwingSequenceTests.cs` - new
- `tests/Structure.Tests/TrendStateTests.cs` - new
- `tests/Structure.Tests/StructurePipelineTests.cs` - new

No existing file changes. `Swing.Label` was left settable in 1a precisely for
this.

## Data / contracts

**Load-bearing. Features 3 to 6 read all of this.**

```csharp
public enum TrendState { Undetermined, Uptrend, Downtrend }

public sealed class SwingSequence
{
    // Sets swing.Label as a side effect. Swings must arrive in confirmation
    // order, which is what PivotDetector emits.
    public void Add(Swing swing);

    public TrendState State { get; }

    // The structural levels features 3 to 6 break, retest and invalidate.
    public Swing LastHigh { get; }   // null until a high has arrived
    public Swing LastLow { get; }    // null until a low has arrived
}
```

**Labelling rule.** Compare each swing to the previous swing of the same kind:

| Comparison | High side | Low side |
|---|---|---|
| Higher than predecessor | HH | HL |
| Lower than predecessor | LH | LL |
| Equal to predecessor | Undetermined | Undetermined |
| No predecessor | Undetermined | Undetermined |

**Equal-price rule, decided here because no plan defines it.** Two consecutive
same-kind swings at exactly the same price produce `Undetermined`, not a carried
forward label. This matches the strict comparison already used for pivots in 1a:
where the market has not actually made a higher high, the engine does not claim
one. A double top is genuinely ambiguous, and saying so is more honest than
guessing.

**Trend state rule.** `Uptrend` when the latest high is HH and the latest low is
HL. `Downtrend` when the latest high is LH and the latest low is LL. Everything
else, including any `Undetermined` on either side, is `Undetermined`. Both sides
must agree, so a market making higher highs on lower lows is reported as
undetermined rather than forced into a direction.

**Ordering property.** Highs and lows are compared independently, so when an
outside bar confirms both a high and a low on the same bar, the order the caller
adds them in does not change either label. Worth a test.

## Testing

The test gate is **on** (`AGENTS.md` declares `dotnet test tests/Structure.Tests`).
Every step here adds logic, so every step ships tests in the same diff.

- In-scope: `SwingSequence` in full. Pure logic with real edge cases, exactly
  what the gate is for.
- `TrendState` is an enum and needs no direct test.
- Step 3 is an integration test across `PivotDetector` and `SwingSequence`, still
  pure logic with no external dependency, so it belongs in the same suite.
- Evidence is `dotnet test` output. No browser, no NinjaTrader, nothing to click.
- Reuse `TestBars` from 1a rather than writing new helpers.

## Notes for the AI

- **Conservative C# only.** `LangVersion` is pinned to 6 in the test project and
  NinjaTrader compiles this same source. No pattern matching, no records, no
  target-typed `new`.
- **No NinjaTrader types in `src/Structure/`.** Unchanged from 1a and still the
  point of the whole architecture.
- **No LINQ and no allocation in `Add`.** It runs per confirmed swing, which is
  far rarer than per bar, but phase 2 optimization runs make the whole path hot.
- `Add` mutates the `Swing` it is given. That is deliberate: `Label` is the one
  settable property on an otherwise immutable type.
- Do not infer trend from a single side. Both the high and low sequence must
  agree, per the rule above.
- No em dashes in code, comments, or commit messages.
