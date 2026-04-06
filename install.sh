#!/usr/bin/env sh
set -eu

# Install prompt from the latest GitHub release.
# Usage:
#   REPO_OWNER=owner REPO_NAME=repo ./install.sh
# Optional:
#   INSTALL_DIR=/custom/bin ./install.sh

REPO_OWNER="${REPO_OWNER:-EduardoQuintana}"
REPO_NAME="${REPO_NAME:-Prompt}"
INSTALL_DIR="${INSTALL_DIR:-$HOME/.local/bin}"

OS="$(uname -s | tr '[:upper:]' '[:lower:]')"
ARCH="$(uname -m)"

case "$OS" in
  linux) GOOS="linux" ;;
  darwin) GOOS="darwin" ;;
  *)
    echo "Unsupported OS: $OS"
    echo "This installer currently supports Linux and macOS."
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

ASSET="prompt_${GOOS}_${GOARCH}.tar.gz"
URL="https://github.com/${REPO_OWNER}/${REPO_NAME}/releases/latest/download/${ASSET}"

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
tar -xzf "$TMP_DIR/$ASSET" -C "$TMP_DIR"
install "$TMP_DIR/prompt" "$INSTALL_DIR/prompt"

echo "Installed prompt to $INSTALL_DIR/prompt"
echo "Make sure $INSTALL_DIR is in your PATH."