# CI/CD Pipeline

Five workflows in `.github/workflows/`. Read this before changing any of them, or before assuming CI verifies something it doesn't.

## The main pipeline: build → publish → release

`build.yml` triggers on push to `master` (paths: `src/**`, `tests/**`, `GitPrompt.slnx`, the workflow files themselves, `.github/scripts/**`) or manual dispatch. A workflow-level `concurrency` group cancels any in-progress `build.yml` run when a new push arrives, so the latest commit always wins.

Separately, `release.yml`'s `release` job and `aliases.yml`'s `release` job both declare a job-level `concurrency: group: gh-release-latest, cancel-in-progress: false`. This is a different mechanism for a different problem: a single push can touch both `src/**` and `git_aliases.sh` at once, triggering both workflows on the same commit — without this, their release-mutating steps (deleting/recreating `latest`, moving its tag) could interleave and corrupt the release or drop assets. `cancel-in-progress: false` here is deliberate: the second one should wait its turn, not cancel the first mid-`gh release delete`/`create`.

1. **`build` job — matrix over `ubuntu-latest`/`windows-latest`/`macos-latest`** (`fail-fast: false`, so one OS failing doesn't hide the others' results). Each leg: `dotnet restore` → `dotnet build -c Release` → `dotnet test GitPrompt.slnx -c Release` — the *entire* solution's test suite (unit + integration) on all three platforms. `publish` only starts once every matrix leg passes.
2. **`publish` job** (`needs: build`, calls `publish.yml`) — matrix-builds unsigned binaries for `linux-x64`/`osx-x64`/`win-x64` (all amd64, see [platform.md](platform.md)) via `dotnet publish` and `.github/scripts/publish-artifact.sh` (which stamps `-p:SourceRevisionId="$(git rev-parse --short HEAD)"` and packages a `.tar.gz`/`.zip`). **No tests run here** — this job only cross-compiles. Each platform's archive is uploaded as a build artifact.
3. **`release` job** (`needs: publish`, calls `release.yml`) — downloads all platform artifacts, and if a release tagged `latest` already exists, deletes it (`gh release delete latest --yes --cleanup-tag`) before recreating it pointing at the current commit SHA, with auto-generated notes, attaching all platform archives plus `git_aliases.sh`.

**`latest` is a floating release, not a version.** There's no semver and no changelog — every successful push to `master` deletes and recreates the same `latest` tag pointing at the new HEAD. `install.sh`/`gitprompt update` always fetch whatever `latest` currently points to.

## `aliases.yml` — a separate, narrower pipeline with a subtly different release step

Triggers only when `git_aliases.sh` or `aliases.yml` itself changes. Its `validate` job runs the **same full-solution** `restore`/`build`/`test` matrix as `build.yml` (`ubuntu-latest`/`windows-latest`/`macos-latest`) — so an aliases-only change still runs all unit and integration tests on all three platforms, even though almost none of them touch aliases.

Its `release` job is **not** the same as `release.yml`: instead of deleting and recreating the release, it force-moves the `latest` git tag to the current commit (`git tag -f latest <sha>` + `git push --force`) before uploading. A GitHub Release resolves its displayed target commit live from its tag's ref, so this makes `latest` reflect the newest commit — full-build or aliases-only — without needing to re-fetch and re-attach platform binaries (which this workflow never builds in the first place). If a `latest` release doesn't exist yet, it falls back to `gh release create --target "${{ github.sha }}"`. Either way, only the `git_aliases.sh` asset is replaced (`--clobber`); existing platform binaries are left untouched. Consequence: `latest`'s tag is always current, but its *asset set* can still mix commits — the aliases file may be newer than the attached binaries, or vice versa, depending on which pipeline ran most recently. `gitprompt update aliases` only ever fetches the `git_aliases.sh` asset, so this is fine for that use case — just worth knowing before reasoning about what "the latest release" contains as a whole for any given commit.

## `performance.yml` — manual only, not a gate

`workflow_dispatch` only — never runs on push, never blocks a build. Runs the BenchmarkDotNet suite (`benchmarks/GitPrompt.Benchmarks`) and uploads results as a plain workflow artifact (JSON + Markdown exporters). Not attached to any release; purely for on-demand manual performance investigation.

## What CI does not verify

- **`bash-init.sh` and `git_aliases.sh` are never actually executed.** The `dotnet test` matrix covers the C#-level platform branches (`UninstallCommand`'s running-`.exe` rename-then-delete dance, `XdgPaths`' Windows env-var resolution, `TestHelpers.FakeSlowGitOverride`'s `ping.exe` fallback — see [testing.md](testing.md) and [platform.md](platform.md)) on all three OSes, since those run through the xUnit test host regardless of which shell invoked `dotnet test`. But nothing in CI sources `bash-init.sh` or `git_aliases.sh` in a real shell and checks their behavior — `GitAliasesTests.cs` only does static analysis of `git_aliases.sh`'s text (alias/function name checks), it never runs it. The actual Git Bash startup hooks, `PROMPT_COMMAND` cycle, and aliases you use interactively are only ever verified by manually using the tool.
