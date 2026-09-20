#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project_version_file="$repo_root/ProjectSettings/ProjectVersion.txt"
manifest_file="$repo_root/Packages/manifest.json"
expected_editor="6000.3.11f1"

fail() {
  printf 'Unity bootstrap verification: FAIL — %s\n' "$1" >&2
  exit 1
}

[[ -f "$project_version_file" ]] || fail "ProjectSettings/ProjectVersion.txt is missing"
[[ -f "$manifest_file" ]] || fail "Packages/manifest.json is missing"

grep -Fq "m_EditorVersion: $expected_editor" "$project_version_file" || \
  fail "Editor version is not $expected_editor"

grep -Fq '"com.unity.render-pipelines.universal"' "$manifest_file" || \
  fail "Universal RP package is missing"

grep -Fq '"com.unity.inputsystem"' "$manifest_file" || \
  fail "Input System package is missing"

grep -Fq '"com.unity.test-framework"' "$manifest_file" || \
  fail "Test Framework package is missing"

printf 'Unity bootstrap verification: PASS\n'
