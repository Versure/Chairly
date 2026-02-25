#!/bin/bash
# Ralph WSL Environment Setup
# Run this script in WSL Ubuntu to install all prerequisites for Ralph.
# Usage: bash scripts/ralph/setup-wsl.sh

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

info() { echo -e "${GREEN}[OK]${NC} $1"; }
warn() { echo -e "${YELLOW}[WARN]${NC} $1"; }
fail() { echo -e "${RED}[MISSING]${NC} $1"; }

ERRORS=0

echo ""
echo "=========================================="
echo "  Ralph WSL Environment Setup — Chairly"
echo "=========================================="
echo ""

# -------------------------------------------
# 1. Git
# -------------------------------------------
if command -v git &>/dev/null; then
  info "Git $(git --version | cut -d' ' -f3)"
else
  warn "Git not found — installing..."
  sudo apt update && sudo apt install -y git
  info "Git installed: $(git --version | cut -d' ' -f3)"
fi

# -------------------------------------------
# 2. Node.js (via nvm — recommended for Claude Code)
# -------------------------------------------
if command -v node &>/dev/null; then
  NODE_VERSION=$(node --version)
  NODE_MAJOR=$(echo "$NODE_VERSION" | cut -d'.' -f1 | tr -d 'v')
  if [ "$NODE_MAJOR" -ge 20 ]; then
    info "Node.js $NODE_VERSION"
  else
    warn "Node.js $NODE_VERSION is too old — Claude Code requires Node.js >= 20"
    warn "Run: nvm install 22 && nvm use 22"
    ERRORS=$((ERRORS + 1))
  fi
else
  warn "Node.js not found — installing via nvm..."
  if ! command -v nvm &>/dev/null && [ ! -s "$HOME/.nvm/nvm.sh" ]; then
    curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.40.1/install.sh | bash
    export NVM_DIR="$HOME/.nvm"
    # shellcheck source=/dev/null
    [ -s "$NVM_DIR/nvm.sh" ] && . "$NVM_DIR/nvm.sh"
  else
    export NVM_DIR="$HOME/.nvm"
    # shellcheck source=/dev/null
    [ -s "$NVM_DIR/nvm.sh" ] && . "$NVM_DIR/nvm.sh"
  fi
  nvm install 22
  nvm use 22
  info "Node.js $(node --version) installed via nvm"
fi

# -------------------------------------------
# 3. Claude Code CLI
# -------------------------------------------
if command -v claude &>/dev/null; then
  info "Claude Code CLI found at $(which claude)"
else
  warn "Claude Code CLI not found — installing..."
  npm install -g @anthropic-ai/claude-code
  if command -v claude &>/dev/null; then
    info "Claude Code CLI installed"
  else
    fail "Claude Code CLI installation failed — try manually: npm install -g @anthropic-ai/claude-code"
    ERRORS=$((ERRORS + 1))
  fi
fi

# -------------------------------------------
# 4. GitHub CLI (gh)
# -------------------------------------------
if command -v gh &>/dev/null; then
  info "GitHub CLI $(gh --version | head -1 | cut -d' ' -f3)"
  if gh auth status &>/dev/null 2>&1; then
    info "GitHub CLI authenticated"
  else
    warn "GitHub CLI installed but not authenticated — run: gh auth login"
    ERRORS=$((ERRORS + 1))
  fi
else
  warn "GitHub CLI not found — installing..."
  (type -p wget >/dev/null || (sudo apt update && sudo apt install -y wget)) \
    && sudo mkdir -p -m 755 /etc/apt/keyrings \
    && out=$(mktemp) && wget -nv -O"$out" https://cli.github.com/packages/githubcli-archive-keyring.gpg \
    && cat "$out" | sudo tee /etc/apt/keyrings/githubcli-archive-keyring.gpg > /dev/null \
    && sudo chmod go+r /etc/apt/keyrings/githubcli-archive-keyring.gpg \
    && echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | sudo tee /etc/apt/sources.list.d/github-cli.list > /dev/null \
    && sudo apt update \
    && sudo apt install -y gh
  info "GitHub CLI installed — run 'gh auth login' to authenticate"
  ERRORS=$((ERRORS + 1))
fi

# -------------------------------------------
# 5. jq
# -------------------------------------------
if command -v jq &>/dev/null; then
  info "jq $(jq --version)"
else
  warn "jq not found — installing..."
  sudo apt update && sudo apt install -y jq
  info "jq installed"
fi

# -------------------------------------------
# 6. .NET SDK (for backend quality checks)
# -------------------------------------------
if command -v dotnet &>/dev/null; then
  DOTNET_VERSION=$(dotnet --version)
  DOTNET_MAJOR=$(echo "$DOTNET_VERSION" | cut -d'.' -f1)
  if [ "$DOTNET_MAJOR" -ge 10 ]; then
    info ".NET SDK $DOTNET_VERSION"
  else
    warn ".NET SDK $DOTNET_VERSION found but .NET 10 is required"
    warn "See: https://dotnet.microsoft.com/download/dotnet/10.0"
    ERRORS=$((ERRORS + 1))
  fi
else
  warn ".NET SDK not found — Ralph needs it for backend quality checks"
  warn "Install from: https://dotnet.microsoft.com/download/dotnet/10.0"
  warn "Or run: sudo apt install -y dotnet-sdk-10.0"
  ERRORS=$((ERRORS + 1))
fi

# -------------------------------------------
# 7. SSH key for GitHub
# -------------------------------------------
if [ -f "$HOME/.ssh/id_ed25519" ] || [ -f "$HOME/.ssh/id_rsa" ]; then
  info "SSH key found"
  if ssh -T git@github.com 2>&1 | grep -q "successfully authenticated"; then
    info "SSH key authenticated with GitHub"
  else
    warn "SSH key exists but may not be added to GitHub — verify with: ssh -T git@github.com"
  fi
else
  fail "No SSH key found — generate one with: ssh-keygen -t ed25519 -C \"your-email@example.com\""
  ERRORS=$((ERRORS + 1))
fi

# -------------------------------------------
# 8. Project clone
# -------------------------------------------
echo ""
RALPH_PROJECT_DIR="$HOME/projects/Chairly"
if [ -d "$RALPH_PROJECT_DIR/.git" ]; then
  info "Project cloned at $RALPH_PROJECT_DIR"
else
  warn "Project not cloned at $RALPH_PROJECT_DIR"
  echo "  Run: mkdir -p ~/projects && git clone git@github.com:Versure/Chairly.git ~/projects/Chairly"
  ERRORS=$((ERRORS + 1))
fi

# -------------------------------------------
# Summary
# -------------------------------------------
echo ""
echo "=========================================="
if [ $ERRORS -eq 0 ]; then
  echo -e "  ${GREEN}All prerequisites met!${NC}"
  echo "  Ralph is ready to run."
  echo ""
  echo "  Next steps:"
  echo "    cd ~/projects/Chairly"
  echo "    git pull"
  echo "    ./scripts/ralph/ralph.sh --tool claude 3"
else
  echo -e "  ${YELLOW}$ERRORS issue(s) need attention${NC}"
  echo "  Fix the issues above, then re-run this script."
fi
echo "=========================================="
echo ""
