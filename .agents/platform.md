# Cross-Platform Support

GitPrompt ships for Linux, macOS, and Windows (via Git Bash) — see the build matrix below for exactly which combinations. Read this before touching path/env resolution, process-spawning, or the install scripts.

## Where platform actually branches, and why

- **`Platform/XdgPaths.cs`** — config and cache directories branch cleanly: Windows uses `%APPDATA%`/`%LOCALAPPDATA%` (falling back to the matching `SpecialFolder` if the env var is unset), Unix uses `$XDG_CONFIG_HOME`/`$XDG_CACHE_HOME` with `~/.config`/`~/.cache` fallback. **`GetDataDirectory()` has no Windows branch at all** — it always returns `~/.local/share/gitprompt`, which on Windows resolves under the Windows user profile (e.g. `C:\Users\name\.local\share\gitprompt`) via `SpecialFolder.UserProfile`. This looks like a missing `%APPDATA%` branch at first glance; it isn't an oversight to "fix" by copying the config/cache pattern without checking why aliases/completions data specifically doesn't follow it.
- **`Platform/PlatformProvider.cs`** — `Host` resolves `$MSYSTEM` (set by Git Bash/MSYS2, e.g. `MINGW64`) in preference to `Environment.MachineName`, so the hostname segment of the prompt shows the MSYS environment name on Windows, not the actual computer name.
- **`Git/Utilities.cs`** — `FileSystemPathComparer`/`FileSystemPathComparison` switch to `OrdinalIgnoreCase` on Windows (NTFS is case-insensitive by default). Used anywhere a path is compared for equality or containment — e.g. `GitRepositoryLocator`'s worktree-containment check, `UninstallCommand`'s cwd guard. Get this wrong and a feature silently misbehaves only on Windows.
- **`UpdateCommand.cs`** (`Run` and `RunUpdateAliases`) and **`install.sh`** both add `curl --ssl-no-revoke` on Windows — works around curl/Schannel OCSP revocation-check failures that are common behind corporate proxies/firewalls on Windows. Removing it doesn't affect Linux/macOS but breaks `gitprompt update` for Windows users on such networks.
- **`UninstallCommand.cs`** — deleting the running binary differs by platform because Windows locks a running `.exe`. On Unix, `File.Delete` on the running binary just works (the inode stays valid until the process exits). On Windows, it renames the file first (renaming a running executable *does* succeed) then spawns a hidden `cmd.exe /c timeout ... & del` to delete the renamed file once the process exits. Don't collapse this into one code path.

The shell side has its own Windows branch too — see [shell-contract.md](shell-contract.md)'s "Git Bash / MSYS" section for `bash-init.sh`'s `$OSTYPE` detection.

## Build matrix — amd64 only, three OSes

`.github/workflows/publish.yml`'s matrix publishes exactly three combinations, all `amd64`: `linux-x64` (`ubuntu-latest`), `osx-x64` (`macos-latest`), `win-x64` (`windows-latest`). There is no arm64 build for any OS — no Apple Silicon–native binary, no ARM Linux, no ARM Windows. `install.sh`'s own architecture check (`x86_64|amd64` else `die`) matches this and will refuse to install on anything else. Don't assume this runs natively on Apple Silicon or ARM Linux.

## Versioning: `--version`'s "commit hash"

There's no semver — `gitprompt --version` prints a git commit hash. That's `AssemblyInformationalVersion` bound to MSBuild's `$(SourceRevisionId)` property in `GitPrompt.csproj`, which is empty unless the caller passes it explicitly. Both `dev-install-local.sh` and `.github/scripts/publish-artifact.sh` independently pass `-p:SourceRevisionId="$(git rev-parse --short HEAD)"` at publish time. If you add a third build entry point, it needs the same flag or `--version` prints nothing meaningful. There's no changelog or semantic versioning — `latest` is a floating GitHub release, deleted and recreated on every successful build to `master`; see [ci-cd.md](ci-cd.md) for the full pipeline.
