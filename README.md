# Prompt

This repository builds a single C#/.NET binary from `src/Prompt/Program.cs` and auto-publishes GitHub Releases.

## What Happens On Push

The workflow in `.github/workflows/release.yml` runs only when both are true:

- You push to `master`
- The push includes changes to one of:
  - `src/Prompt/**`
  - `.github/workflows/release.yml`
  - `install.sh`
  - `README.md`

When it runs, it:

- Builds cross-platform self-contained binaries with `dotnet publish`
- Packages release artifacts
- Replaces a fixed `latest` release tag with new artifacts

Stable asset URL pattern:

- `https://github.com/Eqwerty/Prompt/releases/download/latest/<asset-name>`

Current build targets:

- Linux amd64: `prompt_linux_amd64.tar.gz`
- macOS amd64: `prompt_darwin_amd64.tar.gz`
- Windows amd64: `prompt_windows_amd64.zip`

## Install (Linux, macOS, and Windows Git Bash)

The installer downloads the latest GitHub release asset for your OS and installs the binary with this default layout:

- Linux/macOS: `$HOME/.local/bin/gitprompt`
- Windows Git Bash: `$HOME/promptgo/gitprompt.exe`

Install:

    curl -fsSL https://raw.githubusercontent.com/Eqwerty/Prompt/master/install.sh | sh

Notes:

- Installer supports Linux, macOS, and Windows Git Bash on amd64.
- Release binaries are self-contained and do not require a preinstalled .NET runtime.

## Update

Run the same install command again to update to the newest release:

    curl -fsSL https://raw.githubusercontent.com/Eqwerty/Prompt/master/install.sh | sh

Optional alias (Windows Git Bash, with schannel workaround):

    alias updateprompt='curl -fsSL --ssl-no-revoke https://raw.githubusercontent.com/Eqwerty/Prompt/master/install.sh | sh'

## Bash Prompt Setup

After install, set `PS1` and you are done.

Linux/macOS:

    PS1='$($HOME/.local/bin/gitprompt)'

Windows Git Bash:

    PS1='$(~/promptgo/gitprompt.exe)'
