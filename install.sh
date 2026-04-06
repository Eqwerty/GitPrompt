#!/usr/bin/env sh
set -eu

# Install prompt from the latest GitHub release.
# Usage:
#   ./install.sh
# Optional:
#   INSTALL_DIR=/custom/bin ./install.sh

BIN_BASENAME="${BIN_BASENAME:-gitprompt}"

OS="$(uname -s | tr '[:upper:]' '[:lower:]')"
ARCH="$(uname -m)"

case "$OS" in
  linux) GOOS="linux" ;;
  darwin) GOOS="darwin" ;;
  msys*|mingw*|cygwin*) GOOS="windows" ;;
  *)
    echo "Unsupported OS: $OS"
    echo "This installer currently supports Linux, macOS, and Windows (Git Bash)."
    exit 1
    ;;
esac

case "$ARCH" in
  x86_64|amd64) GOARCH="amd64" ;;
  *)
    echo "Unsupported architecture: $ARCH"
    echo "This installer currently supports amd64 only."
    exit 1
    ;;
esac

if [ -z "${INSTALL_DIR:-}" ]; then
  if [ "$GOOS" = "windows" ]; then
    INSTALL_DIR="$HOME/promptgo"
  else
    INSTALL_DIR="$HOME/.local/bin"
  fi
fi

if [ "$GOOS" = "windows" ]; then
  ASSET="prompt_${GOOS}_${GOARCH}.zip"
  EXTRACTED_NAME="prompt.exe"
  INSTALL_NAME="${INSTALL_NAME:-${BIN_BASENAME}.exe}"
else
  ASSET="prompt_${GOOS}_${GOARCH}.tar.gz"
  EXTRACTED_NAME="prompt"
  INSTALL_NAME="${INSTALL_NAME:-${BIN_BASENAME}}"
fi

URL="https://github.com/Eqwerty/Prompt/releases/latest/download/${ASSET}"

TMP_DIR="$(mktemp -d)"
trap 'rm -rf "$TMP_DIR"' EXIT INT TERM

echo "Downloading ${URL}"

if command -v curl >/dev/null 2>&1; then
  curl -fsSL "$URL" -o "$TMP_DIR/$ASSET"
elif command -v wget >/dev/null 2>&1; then
  wget -qO "$TMP_DIR/$ASSET" "$URL"
else
  echo "Neither curl nor wget is installed."
  exit 1
fi

mkdir -p "$INSTALL_DIR"

if [ "$GOOS" = "windows" ]; then
  if command -v unzip >/dev/null 2>&1; then
    unzip -q "$TMP_DIR/$ASSET" -d "$TMP_DIR"
  elif command -v powershell.exe >/dev/null 2>&1; then
    powershell.exe -NoProfile -Command "Expand-Archive -Path '$TMP_DIR/$ASSET' -DestinationPath '$TMP_DIR' -Force" >/dev/null
  else
    echo "Need unzip or powershell.exe to extract zip files."
    exit 1
  fi
else
  tar -xzf "$TMP_DIR/$ASSET" -C "$TMP_DIR"
fi

cp "$TMP_DIR/$EXTRACTED_NAME" "$INSTALL_DIR/$INSTALL_NAME"
chmod +x "$INSTALL_DIR/$INSTALL_NAME" 2>/dev/null || true

echo "Installed to $INSTALL_DIR/$INSTALL_NAME"
echo "Make sure $INSTALL_DIR is in your PATH."