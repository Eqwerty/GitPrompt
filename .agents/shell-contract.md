# Shell ↔ Binary Contract

`gitprompt` splits its logic across a shell script (`src/GitPrompt/Resources/bash-init.sh`, embedded as a resource and printed by `gitprompt init bash`) and the C# binary. Neither side is fully self-explanatory without the other — this doc is the bridge. Read this before changing `InitCommand.cs`, `bash-init.sh`, or the cache-invalidation flags in `CommandRegistry.cs`.

## Why there's a shell side at all

A subprocess can't change its parent shell's prompt, environment, or loaded functions. Everything that must persist in *the interactive shell itself* — `PS1`, aliases/functions, tab completion — has to be shell script that gets `eval`'d or sourced into that shell. The binary's job is: compute a value, print it, exit. `bash-init.sh` is the glue that repeatedly invokes the binary and wires its output into the shell.

## Per-prompt cycle

Each render is driven by two bash hooks set up once at init time:

1. **`trap '__gitprompt_debug_trap' DEBUG`** fires before every command bash is about to run. It's guarded two ways so it only fires for a *real* user command, not the prompt-rendering machinery itself: `__gitprompt_running` is `1` for the whole duration of `_gitprompt_update_ps1` (hook #2), and there's a belt-and-suspenders check that `$BASH_COMMAND` isn't literally `_gitprompt_update_ps1`. When it does fire, it records a start timestamp (`__gitprompt_cmd_start_us`, via `EPOCHREALTIME`) and sets `__gitprompt_preexec_flag=1`.
2. **`PROMPT_COMMAND=_gitprompt_update_ps1...`** runs right before bash draws the next prompt. It:
   - computes elapsed ms since the recorded start time and passes it to the binary via the `GITPROMPT_LAST_CMD_MS` env var — read by `PlatformProvider.LastCommandDurationMs` and rendered by `CommandDurationSegmentBuilder`,
   - if `__gitprompt_preexec_flag` was set (i.e. a real command just ran), calls `gitprompt --invalidate-status-cache` *before* rendering — this is what forces `GitStatusSharedCache` to treat its cached segment as stale even if the TTL window hasn't elapsed and even if the mtime-based fingerprint didn't change (see [architecture.md](architecture.md)),
   - invokes the binary with no positional args (just the env var above) to get the actual prompt string, and assigns it to `PS1` verbatim (the binary emits the fully-rendered, ANSI-colored string — bash does no further interpretation of it),
   - falls back to `$_GITPROMPT_ORIGINAL_PS1` (the user's real original prompt, captured once at init) if that invocation fails or returns empty output — the safety net for a binary that's present but broken at runtime. If the binary isn't executable *at init time*, these hooks are never installed in the first place (guarded by `[ -x "$_GITPROMPT_BIN" ]`), so `PS1` is simply left as bash's own default.

So: **every command invalidates the status cache; every prompt render re-reads (or rebuilds) it.** The TTL on `GitStatusSharedCache` mostly matters for *rapid* re-renders without an intervening command (e.g. resizing the terminal, `PROMPT_COMMAND` re-firing), not for the normal command→prompt cycle.

## Aliases are loaded independently, not via the binary

`bash-init.sh` sources `git_aliases.sh` directly at init time (`. "$_gitprompt_aliases"`), setting `_GITPROMPT_ALIASES_ENABLED=1`. This has to happen via `source`, not by asking the binary to do it, for the same "subprocess can't mutate parent shell" reason above.

`gitprompt aliases enable`, `gitprompt aliases disable`, and `gitprompt update aliases` are special-cased inside the `gitprompt()` wrapper function: instead of running the binary directly, it captures the binary's **stdout as shell code** and `eval`s it. The binary can only *print* the shell code needed to load/unload aliases in the current shell — it cannot make that change take effect itself. Any new command that needs to mutate the calling shell's state (not just print something) must follow this same eval-the-stdout pattern, and must be added to the `case` statement in `gitprompt()` in `bash-init.sh`.

## Tab completion is static, generated at init time

`{{GITPROMPT_COMMANDS}}` in `bash-init.sh` is replaced by `InitCommand.GenerateBashInit` with a space-joined list of `CommandRegistry.VisibleCommands` verbs, further filtered to verbs with no space in them. That filter is doing two things at once:

- Hidden commands (`--invalidate-status-cache`, `--migrate-config`) are already excluded upstream by `VisibleCommands` — they're implementation details, not something a user should tab-complete into.
- Multi-word sub-command verbs (`config reset`, `aliases enable`, `aliases disable`, `update aliases`) are deliberately dropped from this top-level list, because `_gitprompt_complete` in `bash-init.sh` completes those one level down via separate hardcoded `case` arms keyed on the *previous* word (`config` → `reset`, `update` → `aliases`, `aliases` → `enable disable`). `{{GITPROMPT_COMMANDS}}` only needs to supply the first-word verbs (`init`, `config`, `aliases`, `update`, `uninstall`, `debug`, `paths`, `--help`, `--version`).

This means:

- Adding a new visible top-level command in `CommandRegistry.cs` does **not** automatically show up in a user's existing shell — they need to re-run `gitprompt init bash` (typically via `gitprompt update`, which re-triggers shell config, or a fresh shell) to regenerate the script.
- Adding a new *sub-command* (a verb containing a space) also needs a new hardcoded `case` arm added to `_gitprompt_complete` by hand — it won't pick it up from `CommandRegistry` automatically the way top-level verbs do.
- Adding a new hidden flag needs no completion work at all, by design.

## Git Bash / MSYS on Windows

`bash-init.sh` detects Windows at the very top via `[[ "$OSTYPE" == msys || "$OSTYPE" == cygwin ]]` (lines 4–5) and switches `_GITPROMPT_BIN` to `gitprompt.exe` (and the fallback `PS1` to a backslash-escaped one). The same check is repeated near the bottom (line 99) to also register tab completion for `gitprompt.exe`, not just `gitprompt`. Both checks need to move together — see [platform.md](platform.md) for the rest of the Windows-specific branching on the C# side (env var resolution, path comparisons, the running-`.exe` delete workaround).

## `--migrate-config` runs on every session start

`InitCommand.Run` calls `ConfigInitializer.InitializeDefaultConfig()` unconditionally before printing the init script — i.e. on every new shell that does `eval "$(gitprompt init bash)"`. This silently upgrades an old `config.jsonc` to the latest schema while preserving existing values. `--migrate-config` as a standalone flag exists mainly for manual recovery/testing, not for interactive use.
