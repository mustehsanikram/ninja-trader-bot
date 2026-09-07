# Fix: Make an empty test run fail

**Type:** Fix
**Status:** verified
**Fixes:** F-13

## The problem

`dotnet test` cannot distinguish "everything passed" from "nothing ran".
Measured without a pipe, so the exit code is the runner's own:

| Run | Exit |
|---|---|
| `dotnet test tests/Structure.Tests --filter "<matches nothing>"` | 0 |
| `dotnet test tests/Structure.Tests` | 0 |

The test command is this project's only automated gate. Anything consuming it,
the Blueprint loop included, reads a run that discovered no tests as a clean
pass. The suite currently finds all 89, so there is no live failure; the risk is
a future change that breaks discovery quietly, such as a renamed assembly, an
unresolved reference, a stray filter in a script, or a build that emits no test
DLL. On this project that failure mode is silent and total.

`coding-standards.md` also states, as of commit `8aa61bc` on `main`, that "An
empty suite should fail rather than pass, so 'no tests ran' never reads as
'passed'." That sentence is currently false, which makes the document defining
the gate wrong about the gate.

## The fix

A `tests.runsettings` file carrying `TreatNoTestsAsError`, wired into the project
so no command line flag is needed:

```xml
<RunSettingsFilePath>$(MSBuildProjectDirectory)/tests.runsettings</RunSettingsFilePath>
```

**Three routes were probed during scoping. Only one works, and it is not the one
the finding suggested.**

| Route | Empty run exit | Verdict |
|---|---|---|
| `<TreatNoTestsAsError>` as an MSBuild property in the csproj | 0 | **No effect.** This was F-13's suggested fix. |
| `dotnet test -- RunConfiguration.TreatNoTestsAsError=true` | 1 | Works, but needs a flag on every invocation |
| `tests.runsettings` plus `RunSettingsFilePath` in the csproj | 1 | Works with the bare command |

The third is the only one that fixes the command `AGENTS.md` actually declares.
The second would have required changing that declared command, leaving anyone who
runs `dotnet test tests/Structure.Tests` by hand with the broken behaviour.

**Must not break.** The normal run must still exit 0 and report 89 passing. A
runsettings file can change discovery or reporting, so the full run is as much
part of the check as the empty one.

## Build steps

- [x] **Step 1 - Wire the runsettings file in** - add
  `tests/Structure.Tests/tests.runsettings` with `TreatNoTestsAsError` set true,
  reference it from the csproj with `RunSettingsFilePath`, and correct the
  sentence in `coding-standards.md` so it describes what the tooling now does.
  *Done when:* both exit codes are measured **without a pipe**, since a pipe
  reports the last command's status rather than the runner's, and
  - `dotnet test tests/Structure.Tests --filter "<matches nothing>"` exits
    non-zero
  - `dotnet test tests/Structure.Tests` exits 0 and reports 89 passing
  - `git diff --stat src/` is empty

## Files / areas

- `tests/Structure.Tests/tests.runsettings` - new
- `tests/Structure.Tests/Structure.Tests.csproj` - one property
- `blueprint/context/coding-standards.md` - the claim now matches reality

No change to `src/`. No test file changes.

## Verify

```
dotnet test tests/Structure.Tests
```

89 passing, exit 0. Then the empty-filter run, which must exit non-zero.

## Notes for the AI

- Write the csproj change with an editor that does literal replacement. Three
  stream-edit attempts during scoping corrupted this exact line: `sed` turned
  `\t` into a tab and `perl` interpolated `$(MSBuildProjectDirectory)`.
- Do not capture an exit code through a pipe. Redirect to a file and read `$?`
  immediately, or the measurement is worthless.
- The mutation check for this step is the empty-filter run itself: it is the
  failing case the change exists to produce. Record both exit codes as evidence.
- Keep the runsettings file minimal. It is a guard, not a place to configure the
  runner generally.
- No em dashes in code, comments, or commit messages.

## Findings

### empty-test-run-fails/F-13 [P2] closed - An empty test run exits zero, so "no tests ran" reads as "passed"

**File:** tests/Structure.Tests/Structure.Tests.csproj:4
**Found:** 2026-09-03 by /audit (scope: current; lens: tests)
**Why it matters:** Measured directly, without a pipe swallowing the code:

    dotnet test ... --filter "<matches nothing>"   exit 0, zero tests run
    dotnet test ...                                exit 0, 84 tests run

The exit code cannot tell the two apart. Anything that consumes the test command
as a gate, this project's own workflow included, would read a run that discovered
no tests as a clean pass.

The suite currently discovers all 84 tests, so there is no live failure. The risk
is a future change that breaks discovery quietly: a renamed assembly, a project
reference that stops resolving, a filter left in a script, or a build that emits
no test DLL. In a project whose test gate is the only automated verification,
that failure mode is silent and total.

`coding-standards.md` now asserts "An empty suite should fail rather than pass, so
'no tests ran' never reads as 'passed'." That sentence is currently false. It was
carried over from the Blueprint template's generic guidance and restated during
this session's rewrite without being checked, which is the exact habit the
mutation-checking rule directly above it was added to prevent.

**Suggested fix:** Add `<TreatNoTestsAsError>true</TreatNoTestsAsError>` to the
test project's `PropertyGroup`, then re-run the empty-filter command and confirm
it now exits non-zero. If that property does not behave as expected on this SDK,
soften the sentence in `coding-standards.md` to say what is actually true rather
than leaving an unenforced claim in the document that defines the gate.
**Resolution:** Fixed in `fix/empty-test-run-fails`, but **not by the route this
finding suggested**. Probing during `/fix` scoping found three candidates and only
one works with the command `AGENTS.md` declares:

| Route | Empty run exit |
|---|---|
| `<TreatNoTestsAsError>` as a csproj MSBuild property, as suggested here | 0, no effect |
| `dotnet test -- RunConfiguration.TreatNoTestsAsError=true` | 1, but needs a flag every call |
| `tests.runsettings` plus `RunSettingsFilePath` in the csproj | 1, with the bare command |

Took the third. Measured without a pipe, since a pipe reports the last command's
status rather than the runner's: empty run now exits 1, full run exits 0 with 89
passing. Mutation check: removing `RunSettingsFilePath` while leaving the
runsettings file in place returns the empty run to exit 0, so the property is what
fires the guard.

The false sentence in `coding-standards.md` is replaced with a description of the
mechanism, including the note that the csproj-property route does not work.

Re-reviewed 2026-09-07 by /audit (scope: current) and closed. With the declared
command `dotnet test tests/Structure.Tests`, the empty run exits 1 and the full
run exits 0 with 89 passing, both measured without a pipe. Build is clean.
`tests.runsettings` is not gitignored, so the guard travels with the repository
rather than working only on this machine, and it carries a single setting so it
cannot quietly change discovery or reporting. The standards text now describes
the mechanism instead of asserting an outcome.

Checked and benign: `dotnet test` run from the repository root fails with
MSB1003, because there is no solution file there. That predates this change,
reproduces on `main`, and fails loudly rather than passing silently, so it is not
a gate risk. Worth knowing that a root-level exit 1 means MSBuild refused to
start, not that the guard fired.
