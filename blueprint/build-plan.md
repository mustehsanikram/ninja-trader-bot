# Build Plan

Prerequisites (NinjaTrader 8 installed, data connection established) live in
`project-plan.md` section 6, not here. They are setup, not features.

Structure definitions that these items implement are fixed in `project-plan.md`
section 4.

## MVP - Phase 1 indicator

- [ ] 1. **Structure core and adapter** - establishes the architecture every
  later item builds on.
  - [x] 1a. **Structure core** - non-repainting pivot detection as plain C#
    classes with no NinjaTrader types, with a test project proving the
    non-repainting guarantee. Buildable without NinjaTrader installed.
  - [ ] 1b. **NinjaScript adapter** - the thin `Indicator` that feeds bars into
    the core and draws confirmed swings. Requires NT8 installed.
- [ ] 2. **Trend labelling** - classify the swing sequence and show it.
  - [x] 2a. **Trend classification** - assign HH, HL, LH, LL to each confirmed
    swing and derive trend state from the sequence, in the core. Buildable
    without NinjaTrader.
  - [ ] 2b. **Swing labels on chart** - render the labels. Requires NT8 and 1b.
- [ ] 3. **Break of Structure** - identify the structural level from the swing
  sequence, draw it, and mark the bar whose close breaks it.
- [ ] 4. **Retest zones** - after a break, mark the retest area and track it
  until it resolves or expires.
- [ ] 5. **Failed BOS** - mark breaks that do not hold within the failure window.
- [ ] 6. **Change of character** - mark the first break against the prevailing
  trend and reset trend state to undetermined.
- [ ] 7. **Explainability surface** - a way to ask why any label was applied,
  exposing the reasoning rather than only the conclusion.
- [ ] 8. **Presentation and configuration** - full parameter surface, grouped and
  ordered, colorblind-safe defaults, every element individually switchable,
  verified on light and dark chart backgrounds.
- [ ] 9. **Packaging and release** - NinjaScript export, versioning, changelog,
  licence check, install and settings documentation. Product name is required by
  this item.

## Post-MVP

- [ ] 10. **Alerts** - push structure events to the trader in real time.
- [ ] 11. **Multi-timeframe structure** - higher-timeframe structure rendered on
  a lower-timeframe chart.

## Phase 2 - gated on research

Not scheduled. Item 13 decides whether anything after it gets built.

- [ ] 12. **Structure event API** - expose the core so a strategy can consume
  structure events rather than re-deriving them.
- [ ] 13. **Research harness** - run the core over exported history outside the
  platform, log structure events with their reasons, and analyse whether BOS and
  retest sequences precede anything on NQ. Defines its own output format. This
  is the gate.
