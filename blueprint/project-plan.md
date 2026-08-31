# Project Plan

## 1. Problem - What problem are we solving?

Traders on NinjaTrader 8 who trade market structure have to identify that
structure by eye, bar by bar, and they do it inconsistently. Where a swing high
sits, whether a level genuinely broke, whether a break failed: these get decided
differently at 9:45 than at 14:15, and differently again in review.

Tooling exists but is largely poor. The market-structure and "Smart Money
Concepts" category is crowded, including free options, yet most implementations
share the same defects:

- Naive swing detection using fixed N-bar pivots, which misses structure on
  fast moves and invents it in chop.
- Repainting. Labels appear, then move or vanish as later bars arrive, which
  makes both live trading and honest backtesting impossible.
- Cluttered output that becomes unreadable once more than one concept is drawn.
- No explanation. The indicator asserts "BOS" and gives no way to ask why.

The gap this product fills: a rigorous, non-repainting, readable structure
indicator that can show its reasoning.

A second problem, deferred to phase 2: automated structure-based scalping
engines commonly enter trades that cannot be explained afterwards, and keep
trading through conditions where their edge does not exist.

## 2. Users - Who is this for?

**Primary: retail NinjaTrader 8 discretionary traders** who trade price
structure, mostly on index futures (NQ, MNQ, ES, MES) but not exclusively. They
want structure marked reliably and legibly so their reads are consistent. They
are buying clarity and correctness, not a promise of returns.

**Secondary, phase 2: traders who want the structure logic automated.** A
different purchase with a much higher evidence bar, sold separately.

**Also the builder.** The indicator doubles as the research instrument for the
open question of whether structure-based scalping has an edge on NQ at all.
Phase 2 is gated on what phase 1 reveals.

## 3. Features - What does the MVP need?

MVP is the phase 1 indicator.

- Non-repainting swing detection engine (the foundation everything reads from)
- Trend labelling: HH, HL, LH, LL across the swing sequence
- Break of Structure: the level that defined structure, and the bar that broke it
- Retest zones: the area to watch after a break, tracked until resolved
- Failed BOS marking: breaks that do not hold
- Change of character marking: the first break against the prevailing trend
- Full configuration surface: colors, toggles, thresholds, grouped sensibly
- Explainability: a way to ask why any label was applied

## 4. Structure definitions

These are product decisions, not implementation details. Different answers
produce visibly different labels, so they are fixed here.

### Break confirmation

A level is broken when a bar **closes** beyond it by at least `BreakBuffer`
ticks. Wick-only penetration is recorded as a **sweep**, not a break.

This choice is load-bearing. Close-based confirmation is what makes the
non-repainting guarantee possible; wick-based confirmation repaints intrabar.
The cost is that labels appear one bar later than a wick-based indicator would
show them. For a product whose central claim is that it does not repaint, this
is the correct trade, and the marketing should say so rather than hide it.

### The three break events

Defined for an uptrend. Mirror for a downtrend.

| Event | Definition |
|---|---|
| BOS | Close above the most recent confirmed swing high. Continuation. |
| Failed BOS | After a confirmed BOS, a close back below the broken level within `FailureWindow` bars. The break did not hold. |
| CHoCH | Close below the most recent confirmed swing low, the higher low that anchored the move. Trend state flips to undetermined until a new sequence establishes. |

Failed BOS and CHoCH are distinct events and can both fire, in that order, as a
move deteriorates. They are separate marks with no overlap ambiguity. An earlier
draft of this plan merged them into one feature, which would have produced
confusing labels.

### Parameters

| Parameter | Meaning | Default |
|---|---|---|
| `SwingStrength` | Bars either side required to confirm a pivot | 3 |
| `BreakBuffer` | Ticks beyond a level for a close to count as a break | 0 |
| `FailureWindow` | Bars after a BOS to watch for failure | 5 |
| `RequireCloseBeyond` | Close-based confirmation rather than wick-based | true |

Defaults are starting points, not tuned values. Tuning them to make any
particular result look good is the failure mode this project exists to avoid.

## 5. Data - What are we storing?

Very little, and none of it in a database. This is a chart indicator, not an
application.

Runtime state, in memory, rebuilt from bars:

| State | Purpose |
|---|---|
| Swing list | Confirmed pivot highs and lows with bar index and price |
| Trend sequence | The HH/HL/LH/LL classification per swing |
| Active BOS levels | Structural levels currently unbroken |
| Retest zones | Open zones after a break, with their resolution status |
| Label reasons | Why each mark was applied, for the explainability surface |

Persisted: user settings only, through the platform's own property
serialization into workspaces and chart templates. Nothing we manage.

## 6. Tech - What stack are we using?

| Concern | Choice |
|---|---|
| Platform | NinjaTrader 8, Windows only |
| Language | C# on NinjaScript, .NET Framework 4.8 |
| Base class | `Indicator` for phase 1, `Strategy` for phase 2 |
| Rendering | `Draw.*` helpers, moving to `OnRender` with SharpDX if performance demands |
| Build | Compiled in-platform into `NinjaTrader.Custom.dll`. No dotnet CLI, no CI. |
| Dependencies | None. Third-party assemblies complicate distribution and are avoided. |

### Architecture: testable core, thin adapter

The structure engine is plain C# classes that take bar data (time, open, high,
low, close) and return structure events, with no NinjaTrader types anywhere in
them. The NinjaScript `Indicator` is a thin adapter: it feeds bars in and draws
what comes out.

This is a deliberate decision with a real cost, one layer of indirection, and
four reasons behind it:

1. It is the only route to an automated test gate on this stack. NinjaScript
   cannot be unit tested in-platform; plain classes can, from a normal .NET test
   project.
2. Phase 2 becomes nearly free. The strategy consumes the same core rather than
   re-deriving structure, so indicator and strategy can never disagree.
3. The research harness can run outside the platform entirely, over exported
   history, at whatever speed is useful.
4. This is a commercial product. Regression safety on the core algorithm matters
   once people are paying for it.

### Prerequisites, not build items

1. Install NinjaTrader 8.
2. Establish a data connection. A free end-of-day feed is not sufficient;
   intraday history is required. A broker sim or demo account is the usual route.
3. Confirm what depth and granularity of NQ history that connection actually
   provides before planning research on top of it.

### First-compile check

Confirm the C# language version this NT8 build accepts. Older builds cap well
below current C#, so avoid newer syntax until verified.

## 7. Monetize - How will this make money?

Paid indicator, sold to retail NT8 traders.

Pricing: one-time purchase. Subscriptions suit signals and strategies where
ongoing value is visible. For a tool, one-time pricing means lower support
burden, no churn management, and no billing infrastructure in v1.

Distribution: direct first, vendor ecosystem later. Selling direct gets the
product shipping without an approval process. The NinjaTrader vendor ecosystem
is for discovery once the product is stable. Verify their current vendor terms
before committing to that route.

The positioning is deliberate and load-bearing: this sells on clarity and
algorithmic correctness, never on performance. It shows the market; it does not
promise returns. That keeps the regulatory surface small and is honest given
that no edge has been established.

Phase 2, if it happens, is a separate product with a separate and much higher
evidence bar.

## 8. UI/UX - How should this look and feel?

The interface is the chart. There is no HTML, no stylesheet, no routes.

Design principles:

- Legible at a glance. The failure mode of this whole category is clutter. Every
  element must earn its pixels, and everything must be individually switchable
  off.
- Readable on light and dark chart backgrounds. Both are common.
- Colorblind-safe defaults. Red and green alone must never carry meaning; pair
  colour with shape or position.
- Settings that explain themselves. Parameters exposed through
  `[NinjaScriptProperty]` with `[Display]` grouping and ordering, named so a
  trader understands them without documentation.

## 9. Deployment - Where and how will this ship?

Not web deployment. Shipping means a NinjaScript export that users import into
their own platform.

- Package as a NinjaScript archive for import through Tools, Import.
- Version explicitly, with a changelog, since users update by re-importing.
- Ship install instructions and a settings guide.

Copy protection: ship something, do not over-invest. A machine-ID licence check
plus obfuscation deters casual sharing. Determined copying of a decompilable
.NET assembly cannot be prevented, and elaborate protection built before there
are customers is premature. This belongs in the packaging work and must not
shape the architecture before then.

Compliance: build to the US regime regardless of jurisdiction. It is the
strictest, so meeting it is safe anywhere looser. In practice it reduces to one
rule the positioning already satisfies: make no performance claim, ever. No
backtest equity curves, no win rates, no testimonials about returns.
Advertising hypothetical performance attaches CFTC Rule 4.41 disclaimer
obligations and sharpens the CTA registration question under CFTC and NFA rules.
Legal review is still required before selling, but no build work is blocked on
it.

## 10. Non-goals

Explicitly out of scope for the MVP:

- The automated strategy. Phase 2, gated on research.
- PK Trade Accounting integration.
- Prop-account (Apex-style) protections.
- Multi-timeframe structure. Valuable, deferred to keep the first release honest.
- Alerts. Deferred, likely the first post-MVP addition.
- Instrument-specific tuning. The indicator stays instrument-agnostic.

## 11. Risks and assumptions

| Risk | Standing |
|---|---|
| Crowded market | Accepted. Differentiation rests on correctness, clarity, and explainability. |
| Repainting | The central technical risk. Designed into the swing engine from the first commit, not fixed later. |
| No established edge | Accepted and explicit. Phase 2 is conditional on research, not assumed. |
| New to NinjaScript | Accepted. Platform gotchas surfaced as encountered rather than front-loaded. |
| New to futures | Accepted. The reason the indicator, not the strategy, ships first. |
| No environment yet | Blocking. See prerequisites in section 6. |
| Regulatory exposure | Reduced by the no-performance-claims constraint; legal review still required. |

Hard constraint: no performance claims in any marketing, documentation, or
product surface. This is not a preference. It is what keeps the regulatory
position defensible and the product honest.

## 12. Open items

Two, neither blocking:

1. Product name. Needed by the packaging and release work, which is the first
   point it is actually required. Everything before that can proceed under a
   working name.
2. Jurisdiction and legal review. Needed before selling, not before building.
   Section 9 assumes the strictest regime in the meantime.
