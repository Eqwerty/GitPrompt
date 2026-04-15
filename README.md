# Prompt

A fast cross-platform shell prompt binary for Git repositories.

`gitprompt` prints a two-line prompt:

1. `user host path [git-status]`
2. prompt symbol (`$`, `#`, or `>`)

This repository contains the source code for that binary.

## Quick Install

Install latest release:

```sh
curl -fsSL https://raw.githubusercontent.com/Eqwerty/Prompt/master/install.sh | sh
```

Default install location:

- Linux/macOS: `$HOME/.local/bin/gitprompt`
- Windows Git Bash: `$HOME/prompt/gitprompt.exe`

Update is the same command.

## Uninstall

```sh
curl -fsSL https://raw.githubusercontent.com/Eqwerty/Prompt/master/uninstall.sh | sh
```

This removes the binary and its install directory. Your shell config (e.g. `~/.bashrc`) is not
modified — remove the `PS1` block manually after uninstalling.

## Bash Setup

Add to your shell config:

Linux/macOS:

```sh
PS1='$([ -x "$HOME/.local/bin/gitprompt" ] && "$HOME/.local/bin/gitprompt" || printf "\w \$ ")'
```

Windows Git Bash:

```sh
PS1='$([ -x "$HOME/prompt/gitprompt.exe" ] && "$HOME/prompt/gitprompt.exe" || printf "\w > ")'
```

The `&&`/`||` guard runs on every prompt render — if `gitprompt` is removed, the prompt falls back to the current directory and prompt symbol (e.g. `~/repos$ `). Bash expands `\w` and `\$` before the command substitution runs.

## Prompt Format Reference

### Overall Shape

Line 1:

`<user> <host> <path> [git-status]`

Line 2:

`$` on Unix, `#` for Unix root, `>` on Windows.

If you are outside a Git repo, the git-status segment is omitted.

### Context Segment

- `<user>`: current user (fallback `?`)
- `<host>`: machine name (fallback `?`)
- `<path>`: current working directory
- Home path is shortened to `~`
- If the working directory no longer exists but a shell fallback path is available, `<path>` is rendered in red and suffixed with `[missing]`

### Git Status Segment

General shape:

`(branch) ↑A ↓B +x ~y ...`

Render order:

1. Branch label
2. Ahead (`↑N`) if `N > 0`
3. Behind (`↓N`) if `N > 0`
4. Staged counts (`+ ~ → -`, non-zero only)
5. Unstaged counts (`+ ~ → -`, non-zero only)
6. Untracked (`?N`)
7. Conflicts (`!N`)
8. Stash (`@N`)

### Branch Labels

- Tracked branch: `(main)`
- No upstream: `*(feature)`
  - `*` means no upstream tracking branch.
- Detached HEAD commit: `(abc1234...)`
- Detached HEAD with one matching remote ref: `(origin/main abc1234...)`

### Operation Markers

If Git has an in-progress operation, it appears inside the branch label:

- `(main|MERGE)`
- `*(feature|CHERRY-PICK)`
- `(feature|REBASE)`

Supported markers: `REBASE`, `MERGE`, `CHERRY-PICK`, `REVERT`, `BISECT`.

### Icons

- `↑` ahead commits
- `↓` behind commits
- `+` added
- `~` modified
- `→` renamed/copied
- `-` deleted
- `?` untracked
- `!` conflicts
- `@` stash entries

Staged and unstaged share the same file-state icons (`+ ~ → -`) and are distinguished by color:

- staged: green
- unstaged: red

Example:

```text
(main) ↑2 ↓1 +1 ~2 +3 -1 ?4 !1 @1
```

In that example, `+1 ~2` is staged, and `+3 -1` is unstaged.

## Local Development

Run the local dev install script:

```sh
sh ./dev-install-local.sh
```

Useful flags:

```sh
sh ./dev-install-local.sh --verbose
sh ./dev-install-local.sh --skip-tests
```
