#!/usr/bin/env sh

# Fast dev installer: copies git_aliases.sh locally without recompiling the binary.
# Usage: sh ./dev-install-aliases.sh

set -eu

SCRIPT_DIRECTORY="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
REPOSITORY_ROOT="$SCRIPT_DIRECTORY"

_INSTALL_SOURCED=1
. "$SCRIPT_DIRECTORY/install.sh"

run_step() {
  step_message="$1"
  log_file="$2"
  shift 2

  if _run_animated_step "$step_message" "$log_file" "$@"; then
    printf "\r${GREEN}✓${R} %s...\n" "$step_message"
  else
    step_status=$?
    printf '\n'
    printf "${RED}error:${R} " >&2
    cat "$log_file" >&2
    exit "$step_status"
  fi
}

TEMPORARY_DIRECTORY="$(mktemp -d)"
trap '_stop_spinner; rm -rf "$TEMPORARY_DIRECTORY"' EXIT
trap '_stop_spinner; printf "${SHOW_CURSOR}\n${RED}error:${R} Cancelled.\n" >&2; exit 130' INT TERM

mkdir -p "$ALIASES_DIR"

run_step "Installing git aliases" "$TEMPORARY_DIRECTORY/aliases.log" \
  cp "$REPOSITORY_ROOT/git_aliases.sh" "$ALIASES_FILE_PATH"

printf '\nReload your shell (or open a new terminal) to apply the changes.\n'
