# Copilot Instructions — GitPrompt

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

## Dev Workflow

- Build and install locally: `sh ./dev-install-local.sh`
- Run tests: `dotnet test`
- Run benchmarks: `dotnet run -c Release` inside `benchmarks/GitPrompt.Benchmarks/`
