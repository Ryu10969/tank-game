#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
unity_editor="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.3.11f1/Unity.app/Contents/MacOS/Unity}"
mkdir -p "$repo_root/Logs/Verification"
for platform in EditMode PlayMode; do
  "$unity_editor" -batchmode -nographics -projectPath "$repo_root" -runTests -testPlatform "$platform" -testResults "$repo_root/Logs/Verification/$platform.xml" -logFile "$repo_root/Logs/Verification/$platform.log"
done
