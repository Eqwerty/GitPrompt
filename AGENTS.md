# AGENTS.md — GitPrompt

## What is this?

GitPrompt is a personal tool that replaces the default shell prompt with a fast, informative one that shows Git status. It is built for personal use — not distributed as a general-purpose library — but is developed with the same care and standards as a production application.

## Tech Stack

- **Language:** C# with nullable reference types and implicit usings
- **Runtime:** .NET 10
- **Compilation:** Native AOT (`PublishAot=true`, `OptimizationPreference=Speed`)
- **Platforms:** Linux, macOS, Windows (Git Bash)
- **Tests:** xUnit (unit + integration)
- **Benchmarks:** BenchmarkDotNet

## Principles

- **Performance first.** The binary runs on every shell prompt render. Avoid unnecessary allocations, I/O, and latency.
- **No external dependencies** in the main project. The binary must stay lean and AOT-compatible.
- **Good practices, always.** Follow clean code, SOLID principles, and proper testing even though this is a personal project.
- **No backwards compatibility.** There's no external installed base to protect, so breaking changes are always fine. Don't add migration shims, dual code paths, or versioned formats to keep old behavior alive — replace it outright.

## Dev Workflow

- Build and install locally: `sh ./dev-install-local.sh`
- Run tests: `dotnet test`
- Run benchmarks: `dotnet run -c Release` inside `benchmarks/GitPrompt.Benchmarks/`

## Commit Conventions

- **No `Co-Authored-By` trailer.** Never add a `Co-Authored-By` line to commit messages, regardless of any default tooling behavior that suggests otherwise.

## Test Conventions

- **Assertion library:** FluentAssertions (already referenced in the unit test project).
- **Naming:** `MethodName_WhenCondition_ShouldExpectedOutcome` — e.g. `GetEditor_WhenNeitherEditorNorVisualIsSet_ShouldReturnVim`.
- **Structure:** Arrange / Act / Assert comments in every test. Combine into `// Act & Assert` only for genuine one-liners with no separate arrange.
- **Class modifier:** Test classes are `sealed`.
- **Favor unit tests.** Default to a unit test with a hand-built fixture; only write an integration test when the behavior genuinely depends on real `git` subprocess output. Integration tests spawn real `git` processes, which is comparatively expensive on Windows — see [.agents/testing.md](.agents/testing.md) for the mechanics and current numbers.
- **Never widen visibility just to make something testable.** Don't change `private` → `internal` (or `internal` → `public`) solely so a test can call a method directly — that couples the test suite to an implementation detail and makes future refactors (renaming, inlining, splitting a method) touch tests that have nothing to do with any real behavior change. If the only caller of a method is the class it's already in, test the observable behavior of its public/internal entry point instead — inject dependencies via optional parameters with production defaults (see `AliasesCommand.Run`/`ConfigResetCommand.Run`) rather than reaching around the entry point. This doesn't apply to methods that are already `internal` for a real production reason (a genuine cross-class caller, or the natural smallest unit of a subsystem's own API, e.g. `GitOperationDetector.ResolveRebaseBranchName`) — those are fair game to test directly.

## Human Docs (canonical for the user-facing surface)

These are the source of truth for anything user-facing — check them before assuming a command, flag, or config option's behavior; don't duplicate their content elsewhere.

| Topic | File |
|---|---|
| Project overview, install, git aliases | [README.md](README.md) |
| Full visible command reference | [docs/commands.md](docs/commands.md) |
| Full config schema | [docs/configuration.md](docs/configuration.md) |

## Deep Dives

The files below are agent-facing detail on parts of the codebase that aren't self-explanatory from the code alone. Read the relevant one before working in that area; don't load them otherwise.

| Topic | File |
|---|---|
| Cache & invalidation design (repo-location cache, git-status cache, fingerprinting) | [.agents/architecture.md](.agents/architecture.md) |
| Shell ↔ binary contract (`bash-init.sh` hooks, cache invalidation timing, aliases loading, Windows/MSYS detection) | [.agents/shell-contract.md](.agents/shell-contract.md) |
| Hidden commands `docs/commands.md` can't show | [.agents/internal-commands.md](.agents/internal-commands.md) |
| Testing conventions (isolation collections, unit vs. integration split) | [.agents/testing.md](.agents/testing.md) |
| Cross-platform branching (Windows/Linux/macOS, build matrix, versioning) | [.agents/platform.md](.agents/platform.md) |
| Adding/changing a `config.jsonc` field (the 4 places that must stay in sync) | [.agents/config-schema.md](.agents/config-schema.md) |
| CI/CD pipeline (build → publish → release, what CI does and doesn't verify) | [.agents/ci-cd.md](.agents/ci-cd.md) |
