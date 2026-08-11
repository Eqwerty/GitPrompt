# Internal Commands & Flags

`docs/commands.md` is the human-facing command reference and intentionally omits commands a user should never type by hand. This file documents the full surface, including `IsHidden: true` entries in `src/GitPrompt/Commands/CommandRegistry.cs`, for anyone (human or AI) working on the command dispatch, shell integration, or cache code.

| Command | Hidden? | Called by | Purpose |
|---|---|---|---|
| `gitprompt --invalidate-status-cache` | Yes | `bash-init.sh`'s `_gitprompt_update_ps1` (the `PROMPT_COMMAND` hook), once per prompt render that follows a real command — flagged by the DEBUG trap | Writes a new random token to `status-invalidation.token`, forcing `GitStatusSharedCache` to treat every cached entry as stale on next read regardless of TTL/fingerprint. See [architecture.md](architecture.md) and [shell-contract.md](shell-contract.md). Harmless to run manually — worst case is one extra `git status` call. |
| `gitprompt --migrate-config` | Yes | `InitCommand.Run`, unconditionally, on every `gitprompt init bash` (i.e. every new shell session) | Upgrades `config.jsonc` to the latest schema while preserving existing values (`ConfigInitializer.InitializeDefaultConfig`). Exposed as a flag mainly for manual recovery/testing — not meant for interactive use. |

All other commands in `CommandRegistry.Commands` are visible (`IsHidden` unset/false) and are documented in `docs/commands.md`; keep that file in sync when adding or renaming a visible command's verb/usage/description.

## Adding a new command

- Visible commands need a `Group` (shown in `gitprompt --help`) and should be added to `docs/commands.md`.
- Hidden commands should have a clear reason to stay hidden (called only by generated shell script, or a pure internal/testing tool) — note it in this file when you add one.
- If the command needs to mutate the calling shell's state (env vars, functions, `PS1`) rather than just print output, it can't do that as a subprocess — see the "Aliases are loaded independently" section in [shell-contract.md](shell-contract.md) for the pattern (print shell code, have `bash-init.sh`'s wrapper `eval` it).
