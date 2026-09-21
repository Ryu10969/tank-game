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

settings_file="$repo_root/ProjectSettings/ProjectSettings.asset"
build_settings="$repo_root/ProjectSettings/EditorBuildSettings.asset"
grep -Fq 'companyName: Ryu10969' "$settings_file" || fail "Company Name is not Ryu10969"
grep -Fq 'productName: Tank Game' "$settings_file" || fail "Product Name is not Tank Game"
grep -Fq 'activeInputHandler: 1' "$settings_file" || fail "New Input System is not active"
[[ -f "$repo_root/Assets/Scenes/Main.unity" ]] || fail "Main scene is missing"
[[ ! -e "$repo_root/Assets/Scenes/SampleScene.unity" && ! -e "$repo_root/Assets/Scenes/SampleScene.unity.meta" ]] || fail "SampleScene remains"
[[ "$(grep -c '    path:' "$build_settings")" == 1 ]] || fail "Build must contain exactly one scene"
grep -Fq '  - enabled: 1' "$build_settings" || fail "Main is not enabled"
grep -Fq '    path: Assets/Scenes/Main.unity' "$build_settings" || fail "Main is not the first build scene"
main_guid="$(awk '/^guid:/ {print $2}' "$repo_root/Assets/Scenes/Main.unity.meta")"
grep -Fq "    guid: $main_guid" "$build_settings" || fail "Main scene GUID mismatch"
for ignored_path in Library/probe Temp/probe Logs/probe UserSettings/probe probe.slnx; do
  git -C "$repo_root" check-ignore -q "$ignored_path" || fail "$ignored_path is not ignored"
done
printf 'Unity bootstrap verification: PASS\n'
