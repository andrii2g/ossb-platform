#!/usr/bin/env sh

set -eu

# Bootstrap script for ensuring the initial repository layout exists.
# The script is intentionally small and safe so it can be used as a
# lightweight sanity check during early repository setup.

ROOT_DIR="$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)"

echo "Checking OSBB Platform repository structure in: $ROOT_DIR"

ensure_dir() {
  dir_path="$1"

  if [ -d "$ROOT_DIR/$dir_path" ]; then
    echo "ok  - $dir_path"
  else
    echo "create - $dir_path"
    mkdir -p "$ROOT_DIR/$dir_path"
  fi
}

ensure_dir "docs"
ensure_dir "services"
ensure_dir "tools"
ensure_dir "libs"
ensure_dir "infra"
ensure_dir "scripts/bootstrap"
ensure_dir ".github/workflows"

ensure_dir "services/billing"
ensure_dir "services/payments"
ensure_dir "services/residents"
ensure_dir "services/notifications"
ensure_dir "services/maintenance"

ensure_dir "tools/invoice-generator"
ensure_dir "libs/core"
ensure_dir "libs/contracts"
ensure_dir "infra/docker"
ensure_dir "infra/env"

echo "Repository bootstrap check complete."
