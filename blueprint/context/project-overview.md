# PK NinjaTrader Product Line - Project Overview

<!-- blueprint:source-hash b0a56bb3607502a1f2a3282282db293f2cee69810eae46433174b1e57a1f64cb -->

> **Generated file. Don't hand-edit.** Re-run `/overview` when `project-plan.md`
> or `build-plan.md` changes materially.

> A non-repainting market-structure indicator for NinjaTrader 8, sold to retail
> futures traders, with an automated strategy deferred to phase 2 and gated on
> research.

## Problem

Traders who trade market structure identify it by eye and do it inconsistently:
where a swing sits, whether a level truly broke, whether a break failed. The
existing tooling category is crowded but weak, dominated by naive N-bar pivot
detection, labels that repaint as new bars arrive, cluttered output, and no way
to ask why a label was applied.

Repainting is the defect that matters most, because it makes both live trading
and honest backtesting impossible.

## Users

| User | What they need |
|---|---|
| Retail NT8 discretionary traders (primary) | Structure marked reliably and legibly so their reads stay consistent. Buying clarity and correctness, not returns. |
| Traders wanting automation (phase 2) | A separate product with a much higher evidence bar. |
| The builder | The indicator doubles as the research instrument for the open question of whether structure scalping has an edge on NQ. |

Mostly index futures (NQ, MNQ, ES, MES), but the indicator stays
instrument-agnostic.

## Features

MVP is the phase 1 indicator, in build-plan order.

1. **Structure core and adapter** - sets the architecture everything else
   inherits. Split into two.
   - **1a. Structure core** - **shipped.** Non-repainting pivot detection as
     plain C# with no NinjaTrader types, with tests proving the guarantee.
   - **1b. NinjaScript adapter** - the thin `Indicator` that feeds bars into the
     core and draws confirmed swings. **Needs NT8 installed.**
2. **Trend labelling** - classify the swing sequence as HH, HL, LH, LL.
3. **Break of Structure** - identify the structural level, draw it, mark the bar
   whose close breaks it.
4. **Retest zones** - mark the retest area after a break and track it to
   resolution.
5. **Failed BOS** - mark breaks that do not hold within the failure window.
6. **Change of character** - mark the first break against prevailing trend and
   reset trend state.
7. **Explainability surface** - the headline differentiator. Ask why any label
   was applied and get the reasoning, not just the conclusion. Nothing else in
   the category does this.
8. **Presentation and configuration** - full parameter surface, colorblind-safe
   defaults, everything switchable, verified light and dark.
9. **Packaging and release** - export, versioning, changelog, licence check,
   docs. Requires the product name.

Post-MVP: alerts (10), multi-timeframe structure (11).

Phase 2, gated: structure event API (12), research harness (13). Item 13 decides
whether anything past it gets built.

> Feature 2 does not depend on 1b. Trend labelling is pure logic over the swing
> sequence, so it can be built and tested in the core while NT8 is unavailable.

## Structure definitions

Fixed decisions, not implementation detail. Build steps implement these rather
than relitigating them.

**Break confirmation:** a level is broken when a bar **closes** beyond it by at
least `BreakBuffer` ticks. Wick-only penetration is a **sweep**, not a break.
Close-based confirmation is what makes the non-repainting guarantee possible;
the cost is labels appearing one bar later than wick-based tools show them.

Defined for an uptrend, mirrored for a downtrend:

| Event | Definition |
|---|---|
| BOS | Close above the most recent confirmed swing high. Continuation. |
| Failed BOS | After a confirmed BOS, a close back below the broken level within `FailureWindow` bars. |
| CHoCH | Close below the most recent confirmed swing low. Trend state flips to undetermined. |

Failed BOS and CHoCH are distinct and can both fire, in that order.

| Parameter | Meaning | Default |
|---|---|---|
| `SwingStrength` | Bars either side to confirm a pivot | 3 |
| `BreakBuffer` | Ticks beyond a level for a valid break close | 0 |
| `FailureWindow` | Bars after a BOS to watch for failure | 5 |
| `RequireCloseBeyond` | Close-based rather than wick-based | true |

Defaults are starting points, never tuned to make a result look good.

**Pivot rule (settled in 1a):** a pivot high requires `High` strictly greater
than every bar within `SwingStrength` on both sides. A plateau of equal highs
produces no pivot. Mirrored for lows.

## Data model

No database. State is in-memory and rebuilt from bars; only user settings
persist, through the platform's own property serialization into workspaces and
chart templates.

### Swing (built in 1a)

- `Index` (int) - bar index of the pivot
- `Price` (double)
- `Kind` (enum: High, Low)
- `Label` (enum: Undetermined, HH, HL, LH, LL) - feature 2 populates this
- `ConfirmedAtIndex` (int) - `Index + SwingStrength`, derived in the constructor

> `ConfirmedAtIndex` is load-bearing. It is what makes the non-repainting claim
> checkable: nothing may be drawn or acted on before this bar. It is computed in
> `Swing`'s constructor rather than passed in, so no caller can report a pivot
> earlier than it became known.

### PivotResult (built in 1a)

What a single bar confirmed: `PivotHigh` and `PivotLow`, either of which may be
null. Both are populated when an outside bar engulfs its neighbours on both
sides. Returning a struct keeps the common no-pivot case allocation-free.

### StructureEvent (not built yet, features 3 to 6)

- `Kind` (enum: Bos, FailedBos, Choch, Sweep)
- `BarIndex` (int) - the bar whose close produced the event
- `Direction` (enum: Up, Down)
- `Level` (double) - the price level broken or swept
- `OriginSwing` (Swing) - the swing that defined the level
- `Reason` (string) - the explainability payload for feature 7

> **Lock this shape.** Features 7, 12, and 13 all consume it. The research
> harness analyses a stream of these, and the phase 2 strategy consumes the same
> type rather than re-deriving structure, so indicator and strategy can never
> disagree.

### RetestZone (not built yet, feature 4)

- `SourceEvent` (StructureEvent) - the break that created it
- `Level` (double)
- `UpperBound` / `LowerBound` (double)
- `CreatedAtIndex` (int)
- `Status` (enum: Open, Touched, Expired, Invalidated)

## Tech stack

- **NinjaTrader 8** - host platform, Windows only. Compiles NinjaScript
  in-platform into `NinjaTrader.Custom.dll`. **Not installed on this machine.**
- **C# on .NET Framework 4.8** - the runtime NT8 targets. Not .NET 8.
- **`Indicator` base class** - phase 1. `Strategy` for phase 2.
- **`Draw.*` helpers** - chart rendering, moving to `OnRender` with SharpDX if
  performance demands.
- **xUnit on net8.0** - test project for the core, added in 1a.
- **No third-party dependencies** in shipped code.

### Architecture: testable core, thin adapter

The structure engine is plain C# classes taking bar data (time, open, high, low,
close) and returning structure events, with **no NinjaTrader types in them**.
The NinjaScript `Indicator` feeds bars in and draws what comes out.

The core is compiled twice: by `dotnet` for the tests, and by NinjaTrader from
its own `bin/Custom` folder. The test project links the source with
`<Compile Include>` rather than referencing a built assembly, so the eventual
export stays self-contained with no external DLL for customers to install.

`tests/Structure.Tests` pins `LangVersion` to 6 so syntax NinjaTrader cannot
accept fails at build here rather than at first import.

### Prerequisites (not build items)

1. Install NinjaTrader 8. Blocks feature 1b and everything visual after it.
2. Establish a data connection with real intraday history. A free end-of-day
   feed is not sufficient; a broker sim or demo account is the usual route.
3. Confirm what depth and granularity of NQ history that connection provides.

> TODO: confirm the C# language version this NT8 build accepts at first compile,
> and raise `LangVersion` to match. The current pin of 6 is a conservative guess.

## Commands

- Test: `dotnet test tests/Structure.Tests`
- Build (NinjaScript): not available, needs an NT8 install
- Verify: not set up. `/ci` owns that.

**The test gate is on.** Any step adding logic must ship a passing test in the
same diff, and the suite must be green before a checkpoint commit or `/complete`.

## Monetization

Paid indicator, **one-time purchase**. Subscriptions suit signals and
strategies; for a tool, one-time means lower support burden, no churn, no
billing infrastructure in v1.

Distribution: direct first, NinjaTrader vendor ecosystem later for discovery
once stable. Verify current vendor terms before committing.

**Sells on clarity and algorithmic correctness, never on performance.** This is
load-bearing, not marketing preference. See Constraints.

## UI/UX

The interface is the chart. No HTML, no stylesheet, no routes.

| Surface | What's there |
|---|---|
| Chart overlay | Swings, trend labels, BOS levels and break markers, retest zones, failed-break and CHoCH marks |
| Explainability | The reason behind any given label, exposed on demand |
| Indicator settings | Parameters via `[NinjaScriptProperty]` with `[Display]` grouping and ordering |

Principles: legible at a glance (clutter is this category's failure mode), every
element individually switchable, readable on light and dark chart backgrounds,
colorblind-safe defaults where colour never carries meaning alone.

## Deployment

Not web deployment. Shipping means a NinjaScript export users import through
Tools, Import.

- Version explicitly with a changelog; users update by re-importing.
- Ship install instructions and a settings guide.
- Copy protection: a machine-ID licence check plus obfuscation. Deters casual
  sharing; determined copying of a decompilable .NET assembly cannot be
  prevented. Belongs in item 9 and must not shape the architecture earlier.

No build command, no test command for the NinjaScript half, no CI. NT8 compiles
in-platform.

## Constraints

**No performance claims, anywhere.** No backtest equity curves, no win rates, no
testimonials about returns, in any marketing, documentation, or product surface.

Advertising hypothetical performance attaches CFTC Rule 4.41 disclaimer
obligations and sharpens the CTA registration question under CFTC and NFA rules.
The plan assumes the US regime regardless of jurisdiction, because it is the
strictest and meeting it is safe anywhere looser. Legal review is required before
selling; no build work is blocked on it.

**No repainting.** Nothing may be drawn or acted on before its
`ConfirmedAtIndex`. This is the product's central claim and the main technical
risk.

**No NinjaTrader types in `src/Structure/`.** The isolation is what makes the
tests and the phase 2 research harness possible.

## Non-goals

The automated strategy (phase 2, gated), PK Trade Accounting integration,
prop-account (Apex-style) protections, multi-timeframe structure, alerts, and
instrument-specific tuning.

## Open questions

1. **Product name.** Required by item 9; nothing earlier blocks on it. Working
   name in use meanwhile.
2. **Jurisdiction and legal review.** Required before selling, not before
   building.
3. **Licensing conflict.** The repository carries an MIT licence, which grants
   anyone the right to use, modify, distribute, and sell the code. That
   contradicts the commercial model in `project-plan.md` section 7 and the
   licence check planned in item 9. **Not recorded in either plan**, so this
   entry does not survive the next regeneration. Add it to `project-plan.md`
   section 12 to make it durable.
