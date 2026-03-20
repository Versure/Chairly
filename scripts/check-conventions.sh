#!/usr/bin/env bash
# Convention checker for Chairly
# Scans for common convention violations in the codebase.
# Run: ./scripts/check-conventions.sh
# Returns exit code 1 if violations found, 0 if clean.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
VIOLATIONS=0

red()    { printf '\033[0;31m%s\033[0m\n' "$1"; }
yellow() { printf '\033[0;33m%s\033[0m\n' "$1"; }
green()  { printf '\033[0;32m%s\033[0m\n' "$1"; }

check() {
    local label="$1"
    shift
    local result
    result=$("$@" 2>/dev/null || true)
    if [ -n "$result" ]; then
        red "FAIL: $label"
        echo "$result" | head -20
        echo ""
        VIOLATIONS=$((VIOLATIONS + 1))
    else
        green "PASS: $label"
    fi
}

echo "=== Chairly Convention Checker ==="
echo ""

# --- TypeScript checks ---

check "No 'any' type in TypeScript" \
    grep -rn --include="*.ts" -E ':\s*any\b|<any>|as any' \
    "$REPO_ROOT/src/frontend/chairly/libs/" \
    --exclude-dir=node_modules

check "No console statements in TypeScript" \
    grep -rn --include="*.ts" -E 'console\.(log|warn|error|debug|info)' \
    "$REPO_ROOT/src/frontend/chairly/libs/" \
    --exclude-dir=node_modules \
    --exclude="*.spec.ts"

check "No @Input() decorator (use input() signal)" \
    grep -rn --include="*.ts" '@Input(' \
    "$REPO_ROOT/src/frontend/chairly/libs/"

check "No @Output() decorator (use OutputEmitterRef)" \
    grep -rn --include="*.ts" '@Output(' \
    "$REPO_ROOT/src/frontend/chairly/libs/"

check "No @ViewChild() decorator (use viewChild())" \
    grep -rn --include="*.ts" '@ViewChild(' \
    "$REPO_ROOT/src/frontend/chairly/libs/"

check "No inline template: in components (use templateUrl:)" \
    grep -rn --include="*.ts" "template:" \
    "$REPO_ROOT/src/frontend/chairly/libs/" \
    | grep -v "templateUrl:" \
    | grep -v "*.spec.ts" \
    | grep -v "node_modules" \
    | grep -v ".routes.ts" \
    | grep -v "// template" \
    || true

check "No empty imports array (omit imports when empty)" \
    grep -rn --include="*.ts" 'imports: \[\]' \
    "$REPO_ROOT/src/frontend/chairly/libs/"

check "No Subject+ngOnDestroy pattern (use takeUntilDestroyed)" \
    grep -rn --include="*.ts" 'ngOnDestroy' \
    "$REPO_ROOT/src/frontend/chairly/libs/" \
    --exclude="*.spec.ts"

# --- Backend checks ---

check "No bare migrationBuilder.CreateTable() (must use IF NOT EXISTS)" \
    grep -rn 'migrationBuilder\.CreateTable(' \
    "$REPO_ROOT/src/backend/Chairly.Infrastructure/Migrations/" \
    --include="*.cs" \
    | grep -v "Designer.cs" \
    | grep -v "ModelSnapshot.cs"

check "No bare migrationBuilder.CreateIndex() (must use IF NOT EXISTS)" \
    grep -rn 'migrationBuilder\.CreateIndex(' \
    "$REPO_ROOT/src/backend/Chairly.Infrastructure/Migrations/" \
    --include="*.cs" \
    | grep -v "Designer.cs" \
    | grep -v "ModelSnapshot.cs"

check "No bare migrationBuilder.AddColumn() (must use IF NOT EXISTS)" \
    grep -rn 'migrationBuilder\.AddColumn(' \
    "$REPO_ROOT/src/backend/Chairly.Infrastructure/Migrations/" \
    --include="*.cs" \
    | grep -v "Designer.cs" \
    | grep -v "ModelSnapshot.cs"

# --- Summary ---

echo ""
if [ "$VIOLATIONS" -gt 0 ]; then
    red "=== $VIOLATIONS convention violation(s) found ==="
    exit 1
else
    green "=== All convention checks passed ==="
    exit 0
fi
