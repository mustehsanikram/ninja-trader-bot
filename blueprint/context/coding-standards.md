# Coding Standards

Stack: C# on NinjaTrader 8 (NinjaScript), .NET Framework 4.8, Windows-only.

> Written from NinjaTrader platform conventions. This is a greenfield project, so
> items marked TODO are decisions still to make, not facts to confirm against an
> existing codebase.

## Language and runtime

- NinjaScript compiles inside NinjaTrader, not with `dotnet`. Source lives under
  `bin/Custom/Strategies/`, `bin/Custom/Indicators/`, `bin/Custom/AddOns/`.
- Target is .NET Framework 4.8. No `System.Text.Json`, no `Span<T>` niceties, no
  nullable reference types.
- > TODO: confirm the C# language version this NT8 build accepts. Older NT8 builds
  > cap out well below current C#; pattern matching and newer syntax may not compile.
- One public class per file, file name matching the class. NinjaTrader requires it.

## Strategy lifecycle

`OnStateChange()` is the constructor equivalent, and each state has a strict job:

| State | What belongs here |
|---|---|
| `SetDefaults` | Property defaults, `Name`, `Calculate`, `IsOverlay`. No data access. |
| `Configure` | `AddDataSeries()`, indicator instantiation |
| `DataLoaded` | Series allocation that needs bar data to exist |
| `Historical` / `Realtime` | Mode-dependent setup only |
| `Terminated` | Cleanup. May fire without `Configure` ever running. |

Never touch `Close[0]` or any series in `SetDefaults`. It runs before data exists.

## Historical vs realtime

This is the single largest source of "it backtested differently" bugs, and the
brief names it directly. The rules:

- `Calculate.OnBarClose` and `Calculate.OnEachTick` produce different entries. State
  the mode explicitly, and know that historical bars replay as OHLC, not real ticks.
- Guard realtime-only work with `State == State.Realtime`. Guard historical-only
  work explicitly rather than assuming.
- `OnMarketData()` does not fire historically at all. Any setup logic depending on
  bid/ask or volume-at-price silently does nothing in a backtest.
- Intrabar granularity in backtest needs `AddDataSeries()` with a finer series, or
  Tick Replay enabled. Without it, intrabar entries are approximated.
- Prefer deriving setup state from bar data both modes share, so the same setup is
  reachable in Strategy Analyzer and live.

## Setup state

The engine pipeline is Market Structure, Setup, Confirmation, Qualification, Risk
Check, Entry, Management, Reset.

- Setup state must persist across bars in explicit fields, not be recomputed from
  scratch each `OnBarUpdate()`. A pending BOS retest spans bars by definition.
- Every state object carries why it exists and when it invalidates. A setup with no
  invalidation condition is a bug.
- Reset is a real stage, not an afterthought. Repeated-entry bugs almost always
  trace to a setup that fired but never cleared.
- > TODO: decide how setup state is modelled when the strategy phase begins.

## Orders and execution

- Pick managed (`EnterLong`, `SetStopLoss`) or unmanaged (`SubmitOrderUnmanaged`)
  and never mix them in one strategy. NinjaTrader will not stop you; behavior gets
  undefined.
- Signal names must be unique and stable. Order tracking, and every
  `OnOrderUpdate` / `OnExecutionUpdate` correlation, keys off them.
- Track orders by holding the `Order` reference from the entry call, and reconcile
  in `OnOrderUpdate()`. Do not assume submission order equals fill order.
- Guard every entry against existing position and existing working order. This is
  where duplicate trades come from.
- `TraceOrders = true` while debugging. It explains every ignored or rejected order.

## Performance

Optimization runs execute the strategy thousands of times, so the hot path matters:

- No LINQ, no allocation, no string formatting inside `OnBarUpdate()` or
  `OnMarketData()`.
- `Print()` is expensive. Gate debug output behind a user-settable bool property,
  default off.

## Properties and parameters

- Expose tunables as `[NinjaScriptProperty]` with `[Display]` grouping and order.
- Use `[Range]` to keep the optimizer inside sane values.
- Parameters that only exist to make a backtest look good are a smell. The brief is
  explicit about avoiding overfitting: prefer fewer parameters with a stated reason
  over many with a tuned value.

## Naming

- Types and members: `PascalCase`
- Locals and parameters: `camelCase`
- Private fields: `_camelCase`
- No Hungarian prefixes

## Testing

There is no unit test runner, and NinjaScript ships none. Testing is currently
manual through the platform:

- Strategy Analyzer for backtest and optimization
- Market Replay for realtime behavior without live risk
- Sim101 account for forward testing

**The test gate is off**, because `AGENTS.md` declares no `test` command. It turns on
only if a runner is added. That would mean extracting pure decision logic (structure
detection, qualification, invalidation) into plain classes with no NinjaTrader
dependency, testable outside the platform. That is a real option worth considering
for a commercial-grade engine, but it is a project decision, not a default.

Until then, logic changes are verified with Strategy Analyzer evidence and Market
Replay observation, not assertions.

## Verification

No browser, no HTTP surface. Evidence for a change means:

- A Strategy Analyzer run over a stated instrument, period, and data series
- Out-of-sample results reported separately from in-sample
- `TraceOrders` / `Print()` output showing the decision path for a specific trade
- For any entry-logic change: the reason the trade fired, traceable to a stage

## Code Quality

- No commented-out code unless there is a stated reason
- No unused usings or variables
- Keep methods under 50 lines; extract private helpers
- A method inside the entry decision path should be nameable as one pipeline stage

## Comments

Write code that explains itself; comment only what the code cannot say.

- Comment the **why**, not the **what**. Delete any comment that restates the code.
- No banner blocks or step-by-step narration of obvious code.
- A comment earns its place when it captures a non-obvious decision, a platform
  gotcha, or why a threshold is the value it is.
- Trading thresholds are the exception worth documenting: a magic number in entry
  logic needs its rationale, or it becomes untouchable later.
- When in doubt, leave the comment out.

## Writing

- No em dashes in generated content: docs, comments, commit messages, specs.
- Use a hyphen for separators; rephrase prose with commas, parentheses, or a colon.
- Avoid en dashes and the ellipsis character too.
