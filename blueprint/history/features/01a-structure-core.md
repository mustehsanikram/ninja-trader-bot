# Feature: Structure core

**From build-plan:** feature 1a
**Status:** verified

## Goal

The non-repainting pivot detection engine, as plain C# classes with no
NinjaTrader types, plus the test project that proves the non-repainting
guarantee holds.

Everything else in the product reads from this. Features 2 through 7 consume its
output, the phase 2 strategy consumes the same types rather than re-deriving
structure, and the research harness runs it outside the platform entirely. It is
also the only part of the product that can be built and verified right now,
since NinjaTrader 8 is not installed.

## In scope

- `Bar`, `Swing`, `SwingKind`, `TrendLabel` types
- An incremental pivot detector: bars fed one at a time, swings returned when
  they become confirmed
- The `ConfirmedAtIndex` non-repainting guarantee, proven by test
- A `dotnet` test project that compiles the core source directly
- A `test` command in `AGENTS.md`, which turns the test gate on

## Out of scope

- The NinjaScript `Indicator`, any chart drawing, any NinjaTrader reference.
  That is feature 1b and needs NT8 installed.
- Populating `TrendLabel` with HH/HL/LH/LL. The field is declared here to keep
  the contract stable; feature 2 fills it.
- BOS, retest zones, failed BOS, CHoCH. Features 3 to 6.
- `Reason` strings and the explainability surface. Feature 7.

## Build loop

Build one step at a time, never the whole feature at once.

1. Plan mode lays out the step before any code.
2. The AI implements just that step.
3. It shows the diff (not full files); you read it and understand it.
4. You approve, then choose whether to commit a checkpoint or roll straight on.
   Checkpoints are optional; `/complete` makes the real feature-level commit at the end.

Never accept a step you haven't read. If a diff is too big to review, the step was too big, so split it.

## Build steps

- [x] **Step 1 - Types and test harness** - create `Bar`, `SwingKind`,
  `TrendLabel` and `Swing` under `src/Structure/`, plus a `net8.0` test project
  that compiles those source files directly rather than referencing a built
  assembly. Add the test command to `AGENTS.md`.
  *Done when:* `dotnet test tests/Structure.Tests` runs and passes, with a real
  assertion that `Swing.ConfirmedAtIndex` equals `Index + SwingStrength`.

- [x] **Step 2 - Incremental pivot detector** - `PivotDetector` with a
  `SwingStrength` setting and a streaming API: each call takes one bar and
  returns the swings that bar confirmed (usually none).
  *Done when:* a hand-built bar series with known pivots yields exactly those
  swings, each reported at the expected confirmation index, for at least
  `SwingStrength` values 1, 2 and 3.

- [x] **Step 3 - Non-repainting proof and edge cases** - tests covering the
  guarantee and the boundaries.
  *Done when:* all of these are covered and passing:
  - No swing is ever returned before its `ConfirmedAtIndex`
  - A series shorter than `2 * SwingStrength + 1` bars returns no swings and
    does not throw
  - A plateau of equal highs produces no pivot high
  - A completely flat series produces no swings
  - `SwingStrength` less than 1 is rejected at construction

## Files / areas

- `src/Structure/Bar.cs` - new
- `src/Structure/SwingKind.cs` - new
- `src/Structure/TrendLabel.cs` - new
- `src/Structure/Swing.cs` - new
- `src/Structure/PivotResult.cs` - new
- `src/Structure/PivotDetector.cs` - new
- `tests/Structure.Tests/Structure.Tests.csproj` - new
- `tests/Structure.Tests/SwingTests.cs` - new
- `tests/Structure.Tests/TestBars.cs` - new
- `tests/Structure.Tests/NonRepaintingTests.cs` - new
- `tests/Structure.Tests/PivotDetectorTests.cs` - new
- `AGENTS.md` - Commands section gains the test command

## Data / contracts

**Load-bearing. Features 2 to 7, the phase 2 strategy, and the research harness
all consume these.** Locked here per `project-overview.md`.

```csharp
public struct Bar
{
    public DateTime Time { get; set; }
    public double Open { get; set; }
    public double High { get; set; }
    public double Low { get; set; }
    public double Close { get; set; }
}

public enum SwingKind { High, Low }

public enum TrendLabel { Undetermined, HH, HL, LH, LL }

public sealed class Swing
{
    // ConfirmedAtIndex is derived here, never passed in, so the
    // non-repainting rule lives in exactly one place.
    public Swing(int index, double price, SwingKind kind, int swingStrength)

    public int Index { get; private set; }
    public double Price { get; private set; }
    public SwingKind Kind { get; private set; }
    public TrendLabel Label { get; set; }       // feature 2 populates this
    public int ConfirmedAtIndex { get; private set; }
}
```

**Pivot rule, decided here because neither plan defines it.** A pivot high
requires `High` strictly greater than every bar within `SwingStrength` on both
sides. A plateau of equal highs therefore produces no pivot. Strict comparison
is deterministic and needs no tie-break; mirror for pivot lows.

**Source layout, and why.** The core lives as `.cs` files that get compiled
twice: by `dotnet` for the tests, and by NinjaTrader from its own `bin/Custom`
folder in feature 1b. The test project links the source via `<Compile Include>`
rather than referencing a built DLL. This keeps the eventual NinjaScript export
self-contained, with no external assembly for users to install, which matches
the no-dependencies decision in the plan.

## Testing

The test gate is **off** right now (`AGENTS.md` declares no `test` command).
**Step 1 turns it on.** From that point every logic-bearing step in this and all
later features must ship a passing test in the same diff.

- In-scope for tests: `PivotDetector` in full. It is exactly the kind of pure
  logic the gate exists for, with real edge cases.
- The types are data holders; step 1 needs only the one `ConfirmedAtIndex`
  assertion to prove the harness works.
- No browser, no screenshots, nothing to click. Evidence is `dotnet test` output.
- There is no NinjaTrader involvement in this feature, so nothing here needs NT8
  to verify.

## Notes for the AI

- **NinjaTrader 8 compiles this source too.** Its accepted C# language version
  is unconfirmed (open TODO in `project-plan.md` section 6). Until it is
  verified, write conservative C#: no `readonly struct`, no `in` parameters, no
  pattern matching, no target-typed `new`, no records. Plain properties and
  methods only.
- **No NinjaTrader types in `src/Structure/`.** Not `Bars`, not `ISeries`, not
  `Draw`. That isolation is the entire point of this feature; if anything from
  the platform leaks in, the tests and the research harness both stop working.
- **No LINQ and no allocation in the per-bar path.** `coding-standards.md`
  requires it, and phase 2 optimization runs will execute this thousands of
  times.
- The detector is **streaming, not batch**. NinjaTrader delivers one bar at a
  time through `OnBarUpdate`, so a batch-only API would force a rewrite in 1b.
- Nothing may be emitted before `ConfirmedAtIndex`. This is the product's central
  claim, not an implementation detail.
- No em dashes in code, comments, or commit messages.
