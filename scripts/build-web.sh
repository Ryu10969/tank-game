#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
unity_editor="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.3.11f1/Unity.app/Contents/MacOS/Unity}"
mkdir -p "$repo_root/Logs/Verification"
"$unity_editor" -batchmode -nographics -quit -projectPath "$repo_root" -buildTarget WebGL -executeMethod TankGame.Editor.SliceProjectBuilder.BuildWeb -logFile "$repo_root/Logs/Verification/WebGL.log"
