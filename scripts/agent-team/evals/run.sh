#!/usr/bin/env bash
# scripts/agent-team/evals/run.sh
# Lightweight validation for agent workflow config, docs, and task specs.

set -euo pipefail

ROOT="$(git rev-parse --show-toplevel)"
cd "$ROOT"

errors=0
warnings=0

fail() {
  echo "ERROR: $*" >&2
  errors=$((errors + 1))
}

warn() {
  echo "WARN: $*" >&2
  warnings=$((warnings + 1))
}

require_file() {
  local file="$1"
  if [[ ! -f "$file" ]]; then
    fail "Missing file: $file"
  fi
}

require_exec() {
  local file="$1"
  if [[ ! -x "$file" ]]; then
    fail "Not executable: $file"
  fi
}

if ! command -v jq >/dev/null 2>&1; then
  fail "jq is required for evals but is not installed."
  exit 1
fi

echo "Running agent workflow evals..."

# ---------------------------------------------------------------------------
# Required scripts
# ---------------------------------------------------------------------------
require_exec "scripts/agent-team/start.sh"
require_exec "scripts/agent-team/rework.sh"
require_exec "scripts/agent-team/hooks/task-completed.sh"

# ---------------------------------------------------------------------------
# Settings hook check
# ---------------------------------------------------------------------------
if ! jq -e '.hooks.TaskCompleted[0].hooks[]? | select(.command == "scripts/agent-team/hooks/task-completed.sh")' \
  .claude/settings.json >/dev/null 2>&1; then
  fail "TaskCompleted hook missing or does not reference scripts/agent-team/hooks/task-completed.sh"
fi

# ---------------------------------------------------------------------------
# Agent front matter checks
# ---------------------------------------------------------------------------
shopt -s nullglob
agent_files=(.claude/agents/*.md)
if [[ ${#agent_files[@]} -eq 0 ]]; then
  fail "No agent files found under .claude/agents/"
else
  for agent in "${agent_files[@]}"; do
    grep -q "^name:" "$agent" || fail "$agent: missing 'name' in front matter"
    grep -q "^description:" "$agent" || fail "$agent: missing 'description' in front matter"
    grep -q "^model:" "$agent" || fail "$agent: missing 'model' in front matter"
    grep -q "^tools:" "$agent" || fail "$agent: missing 'tools' in front matter"
  done
fi

# ---------------------------------------------------------------------------
# Skill front matter checks
# ---------------------------------------------------------------------------
skill_files=(.claude/skills/*/SKILL.md)
if [[ ${#skill_files[@]} -eq 0 ]]; then
  fail "No skills found under .claude/skills/"
else
  for skill in "${skill_files[@]}"; do
    grep -q "^name:" "$skill" || fail "$skill: missing 'name' in front matter"
    grep -q "^description:" "$skill" || fail "$skill: missing 'description' in front matter"
    grep -q "^user-invocable:" "$skill" || fail "$skill: missing 'user-invocable' in front matter"
  done
fi

# ---------------------------------------------------------------------------
# Phase file presence
# ---------------------------------------------------------------------------
require_file ".claude/skills/feature-team/SKILL.md"
require_file ".claude/skills/feature-team/phase-0-spec.md"
require_file ".claude/skills/feature-team/phase-1-backend.md"
require_file ".claude/skills/feature-team/phase-2-frontend.md"
require_file ".claude/skills/feature-team/phase-3-backend-review.md"
require_file ".claude/skills/feature-team/phase-3-frontend-review.md"
require_file ".claude/skills/feature-team/phase-5-merge.md"
require_file ".claude/skills/rework-team/SKILL.md"
require_file ".claude/skills/rework-team/phase-rework-backend.md"
require_file ".claude/skills/rework-team/phase-rework-frontend.md"

# ---------------------------------------------------------------------------
# Docs alignment checks
# ---------------------------------------------------------------------------
if grep -q "docs/specs" docs/agent-teams-workflow.md; then
  fail "docs/agent-teams-workflow.md still references docs/specs; update to .claude/tasks/"
fi

if ! grep -q "\.claude/tasks/" docs/agent-teams-workflow.md; then
  fail "docs/agent-teams-workflow.md should reference .claude/tasks/ paths"
fi

if grep -q "docs/specs" .claude/skills/chairly-spec-format/SKILL.md; then
  fail ".claude/skills/chairly-spec-format/SKILL.md still references docs/specs"
fi

if ! grep -q "\.claude/tasks/" .claude/skills/chairly-spec-format/SKILL.md; then
  fail ".claude/skills/chairly-spec-format/SKILL.md should reference .claude/tasks/"
fi

# ---------------------------------------------------------------------------
# tasks.json schema validation
# ---------------------------------------------------------------------------
task_files=(.claude/tasks/*/tasks.json)
if [[ ${#task_files[@]} -eq 0 ]]; then
  warn "No tasks.json files found under .claude/tasks/"
else
  for tasks_file in "${task_files[@]}"; do
    feature_dir="$(basename "$(dirname "$tasks_file")")"
    feature="$(jq -r '.feature // empty' "$tasks_file")"
    if [[ -z "$feature" || "$feature" == "null" ]]; then
      fail "$tasks_file: missing feature field"
      continue
    fi
    if [[ "$feature" != "$feature_dir" ]]; then
      fail "$tasks_file: feature '$feature' does not match directory '$feature_dir'"
    fi

    spec_path="$(jq -r '.specPath // empty' "$tasks_file")"
    if [[ -z "$spec_path" || "$spec_path" == "null" ]]; then
      fail "$tasks_file: missing specPath field"
      continue
    fi
    expected_spec=".claude/tasks/${feature_dir}/spec.md"
    if [[ "$spec_path" != "$expected_spec" ]]; then
      fail "$tasks_file: specPath '$spec_path' should be '$expected_spec'"
    fi
    if [[ ! -f "$spec_path" ]]; then
      fail "$tasks_file: specPath does not exist: $spec_path"
    fi

    tasks_len="$(jq '.tasks | length' "$tasks_file" 2>/dev/null || echo "")"
    if [[ -z "$tasks_len" || "$tasks_len" -eq 0 ]]; then
      fail "$tasks_file: tasks list is empty"
      continue
    fi

    mapfile -t task_ids < <(jq -r '.tasks[].id' "$tasks_file")
    if [[ ${#task_ids[@]} -eq 0 ]]; then
      fail "$tasks_file: no task IDs found"
      continue
    fi

    duplicates="$(printf '%s\n' "${task_ids[@]}" | sort | uniq -d || true)"
    if [[ -n "$duplicates" ]]; then
      fail "$tasks_file: duplicate task IDs found: $duplicates"
    fi

    while IFS=$'\t' read -r id layer title status; do
      if [[ -z "$id" || -z "$layer" || -z "$title" || -z "$status" ]]; then
        fail "$tasks_file: task entries must include id, layer, title, status"
        continue
      fi
      if ! [[ "$id" =~ ^[BF][0-9]+$ ]]; then
        fail "$tasks_file: invalid task id '$id' (expected B1, F2, ...)"
      fi
      if [[ "$layer" != "backend" && "$layer" != "frontend" ]]; then
        fail "$tasks_file: task '$id' has invalid layer '$layer'"
      fi
      case "$status" in
        pending|in_progress|completed|blocked) ;;
        *) fail "$tasks_file: task '$id' has invalid status '$status'" ;;
      esac

      if [[ -f "$spec_path" ]]; then
        spec_line="$(grep -n "^### ${id}[[:space:]]" "$spec_path" | head -n 1 || true)"
        if [[ -z "$spec_line" ]]; then
          fail "$tasks_file: spec heading missing for task '$id' in $spec_path"
        else
          spec_title="$(printf '%s' "$spec_line" | cut -d: -f2-)"
          spec_title="${spec_title#*### ${id}}"
          spec_title="$(printf '%s' "$spec_title" | sed -E 's/^[[:space:]]+//')"
          spec_title="$(printf '%s' "$spec_title" | sed -E 's/^[^A-Za-z0-9]+[[:space:]]*//')"
          if [[ "$spec_title" != "$title" ]]; then
            fail "$tasks_file: task '$id' title mismatch (spec: '$spec_title', tasks.json: '$title')"
          fi
        fi
      fi
    done < <(jq -r '.tasks[] | [.id, .layer, .title, .status] | @tsv' "$tasks_file")

    while IFS= read -r dep; do
      if [[ -n "$dep" ]] && ! printf '%s\n' "${task_ids[@]}" | grep -Fxq "$dep"; then
        fail "$tasks_file: dependsOn references unknown task id '$dep'"
      fi
    done < <(jq -r '.tasks[].dependsOn[]?' "$tasks_file")
  done
fi

if [[ "$errors" -gt 0 ]]; then
  echo "Agent workflow evals failed: $errors error(s)." >&2
  exit 1
fi

if [[ "$warnings" -gt 0 ]]; then
  echo "Agent workflow evals completed with $warnings warning(s)." >&2
fi

echo "Agent workflow evals passed."
