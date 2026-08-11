# Testing Conventions

Read this before writing a test that touches shared static state (`ConfigReader`, the two shared caches, `PromptDiagnostics`), or before deciding whether a new test belongs in the unit or integration project.

## Isolation collections — one per shared static, and they don't fully work today

`GitPrompt.Tests.Unit` has four `[CollectionDefinition(..., DisableParallelization = true)]` classes, each meant to serialize tests that mutate one specific piece of shared static state (xUnit disables parallelism *within* a collection, but collections still run concurrently *with each other*):

| Collection | `Name` | File | Guards |
|---|---|---|---|
| `CacheIsolationCollection` | `"CacheIsolation"` | `Git/CacheIsolationCollection.cs` | `GitRepositorySharedCache` / `GitStatusSharedCache` static overrides (time provider, cache directory, cleanup schedule) |
| `DiagnosticsIsolationCollection` | `"DiagnosticsIsolation"` | `Diagnostics/DiagnosticsIsolationCollection.cs` | `PromptDiagnostics` statics |
| `ConfigIsolationCollection` | `"GitConfigIsolation"` | `Git/ConfigIsolationCollection.cs` | `ConfigReader`'s static override |
| `ConfigIsolationCollection` | `"ConfigIsolation"` | `Prompting/ConfigIsolationCollection.cs` | `ConfigReader`'s static override |

**Known bug, not just a naming quirk:** the last two rows are two separately-defined classes that happen to share a name but have different `Name` strings (`"GitConfigIsolation"` vs `"ConfigIsolation"`) — and both exist to guard the exact same `ConfigReader.OverrideForTesting` static. Because they're different xUnit collections, tests in one CAN run concurrently with tests in the other, so this doesn't actually isolate the static it's meant to protect. Confirmed by grep: `Git/GitStatusDisplayFormatter*Tests.cs` (in `"GitConfigIsolation"`) and `Prompting/{PromptSymbolBuilder,PromptResult,CommandDurationSegmentBuilder,ContextSegmentBuilder}Tests.cs` (in `"ConfigIsolation"`) all call `ConfigReader.OverrideForTesting` — and `Git/GitRepositorySharedCacheTests.cs`/`GitStatusSharedCacheTests.cs` call it too, from inside a *third* collection (`CacheIsolationCollection`). So three collections currently step on the same static. This should eventually be collapsed into one real collection — until then, if you add a test that calls `ConfigReader.OverrideForTesting`, pick one of the three existing collections rather than inventing a fourth.

**What actually happens if a test mutates one of these statics without joining the right collection:** not a compile error, not a crash — xUnit runs test *classes* in parallel by default, so another parallel test reading the same static mid-mutation gets a flaky, hard-to-reproduce wrong result. If a new test in this area behaves inconsistently between runs, suspect a missing/mismatched collection attribute before anything else.

## Unit vs integration is a mechanical line, not just a folder split

- **`GitPrompt.Tests.Unit`** never spawns a real `git` process. It fakes `PlatformProvider` via `Prompting/TestPlatformProvider.cs` (a constructor-injectable double covering every `PlatformProvider` member) and builds `.git` fixtures by hand on disk via `Git/TemporaryDirectory.cs` — a self-cleaning temp directory that retries deletion up to 5 times with backoff (survives Windows file-lock timing, not just a plain `Directory.Delete`).
- **`GitPrompt.Tests.Integration`** always shells out to a real `git` binary via `TestHelpers.RunGitAsync` / `RunGitAllowFailureAsync`, against its own (simpler, no-retry) `TestHelpers.TemporaryDirectory`. `TestHelpers.ConfigureGitIdentityAsync` sets `user.name`/`user.email` per throwaway repo so commits succeed without touching the machine's global git config.

When adding a test: if it needs to prove behavior against real git plumbing (refs, index, config file parsing), it belongs in integration. If it's testing GitPrompt's own logic given a described repository state, build the fixture by hand in unit — don't reach for a real `git` subprocess there.

## Default to unit tests — this is a real performance decision, not just style

Spawning a process is comparatively expensive on Windows (`CreateProcess`) versus Unix (`fork`+`exec`), and every integration test spawns several real `git` processes. Today the suite is ~453 unit tests vs. 15 integration tests, and those 15 tests already make roughly 110 `git` subprocess calls between them (some individual tests, like the fetch/ahead-behind scenario in `GitStatusCacheIntegrationTests`, make 10+). That ratio — heavily unit-weighted, integration used sparingly — is intentional and should stay that way: **treat "can this be a unit test with a hand-built fixture?" as the default question**, and only add an integration test when the behavior genuinely can't be verified without real `git` output.

When you do need an integration test, keep its process-spawn count down rather than writing one assertion per freshly-created repo:

- Set up one repository per test and assert at multiple points along a sequence of real git operations (this is already the existing style — see `GitStatusCacheIntegrationTests`' fetch test: one clone, several commits/pushes/fetches, with assertions interleaved rather than each in its own test).
- Don't split a scenario into several tests just for readability if it means re-running `git init`/`clone`/`commit` setup multiple times for what's really one continuous story.

## Integration tests: one coarse collection, not several fine ones

Unlike the unit project's per-concern collections, the entire integration assembly shares a single collection (`IntegrationTestCollection`, `"IntegrationTests"`, `DisableParallelization = true`) — every integration test runs sequentially. Integration tests mutate real filesystem/git state and are already slow (spawning `git` processes), so the unit project's fine-grained splitting isn't worth the added complexity here.

## Simulating a slow `git` for timeout tests

`TestHelpers.FakeSlowGitOverride` (`tests/GitPrompt.Tests.Integration/TestHelpers.cs`) is the existing pattern for command-timeout tests — reuse it instead of writing a new one. On Unix it prepends a fake `git` shell script to `PATH`. On Windows it swaps in `ping.exe -n 31 127.0.0.1` (sleeps ~30s, always available) via `Utilities.OverrideProcessStartInfoForTesting`, because Windows' `CreateProcess` can't exec a shell script directly — the `PATH` trick is skipped entirely on Windows for that reason.
