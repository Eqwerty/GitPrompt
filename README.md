# Prompt

This repository publishes a single prompt binary and auto-publishes GitHub Releases.

## What Happens On Push

The workflow in `.github/workflows/release.yml` runs when either is true:

- You push to `master` and at least one release-relevant path changed:
  - `src/**`
  - `tests/**`
  - `Prompt.slnx`
  - `.github/workflows/release.yml`
- You trigger it manually with `workflow_dispatch`

When it runs, it:

- Builds cross-platform binaries
- Packages release artifacts
- Replaces a fixed `latest` release tag with new artifacts

Stable asset URL pattern:

- `https://github.com/Eqwerty/Prompt/releases/download/latest/<asset-name>`

Current build targets:

- Linux amd64: `prompt_linux_amd64.tar.gz`
- macOS amd64: `prompt_darwin_amd64.tar.gz`
- Windows amd64: `prompt_windows_amd64.zip`

## Install (Linux, macOS, and Windows Git Bash)

The installer downloads the latest GitHub release asset for your OS and installs the prompt executable with this default layout:

- Linux/macOS: `$HOME/.local/bin/gitprompt`
- Windows Git Bash: `$HOME/prompt/gitprompt.exe`

Install:

    curl -fsSL https://raw.githubusercontent.com/Eqwerty/Prompt/master/install.sh | sh

Notes:

- Installer supports Linux, macOS, and Windows Git Bash on amd64.
- Installer options are intentionally minimal: `-h` / `--help` only.
- On Linux and macOS, rerunning the installer replaces the binary atomically, so self-updates from your shell prompt work without `Text file busy` errors.

## Update

Run the same install command again to update to the newest release artifact:

    curl -fsSL https://raw.githubusercontent.com/Eqwerty/Prompt/master/install.sh | sh

Optional alias (Windows Git Bash, with schannel workaround):

    alias updateprompt='curl -fsSL --ssl-no-revoke https://raw.githubusercontent.com/Eqwerty/Prompt/master/install.sh | sh'

## Local Development Loop (No Release Needed)

For day-to-day prompt changes, you can test locally without pushing to `master` or publishing a release.

Run:

    sh ./dev-install-local.sh

What it does:

- Restores solution packages
- Builds the solution in Release
- Runs tests in Release
- Publishes a local Release binary for your OS
- Installs it to the same default location as `install.sh`
  - Linux/macOS: `$HOME/.local/bin/gitprompt`
  - Windows Git Bash: `$HOME/prompt/gitprompt.exe`

By default the script keeps `dotnet` output quiet and shows a single-line spinner while each step runs, then prints each completed step with a dimmed duration plus a total overall duration at the end. If you want to stream the underlying `dotnet` output, use verbose mode:

    sh ./dev-install-local.sh --verbose

Optional (faster inner loop, still restores/builds but skips only the test execution and marks that step as skipped):

    sh ./dev-install-local.sh --skip-tests

Short flags also work:

    sh ./dev-install-local.sh -s -v

## Performance Checks (Optional)

If you want to watch for performance regressions while refactoring, run the benchmark suite. All benchmarks run by default:

Local run:

    dotnet run -c Release --project benchmarks/Prompt.Benchmarks/Prompt.Benchmarks.csproj

This runs all benchmarks (parser, display rendering, context building) and writes results to:

- `benchmarks/Prompt.Benchmarks/BenchmarkDotNet.Artifacts/`

Optional: to run only a specific benchmark class, use `--filter`:

    dotnet run -c Release --project benchmarks/Prompt.Benchmarks/Prompt.Benchmarks.csproj -- --filter GitStatusParserBenchmarks

CI run:

- Trigger `.github/workflows/perf.yml` manually with `workflow_dispatch`.
- The workflow uploads benchmark reports as a `benchmark-results` artifact.

## Bash Prompt Setup

After install, set `PS1` and you are done.

Linux/macOS:

    PS1='$($HOME/.local/bin/gitprompt)'

Windows Git Bash:

    PS1='$(~/prompt/gitprompt.exe)'
