# UNITY_SETUP

Status: Required before gameplay implementation
Target host: macOS
Target Editor: Unity 6000.3.11f1

## 1. Install

Unity Hubから次を導入する。

- Unity Editor `6000.3.11f1`
- Web Build Support

Unity公式では、HubがEditor版、module、project、templateを管理する。WebビルドにはWeb moduleが必要である。参照先は`REFERENCES.md`に記録している。

## 2. Generate a clean URP project

Unity Hubで次を指定する。

- Template: `Universal 3D`
- Project name: `tank-game-unity-bootstrap`
- Location: `/Users/masanoryuudai/Documents`
- Unity version: `6000.3.11f1`

生成が完了してEditorのConsole Errorが0件であることを確認し、Unity Editorを終了する。

既存のGitリポジトリへ直接新規プロジェクトを作成しない。初期文書と`.git`を保持するため、一時プロジェクトからUnity管理対象だけをコピーする。

## 3. Copy Unity-managed directories

ターミナルで実行する。

```bash
cd /Users/masanoryuudai/Documents/tank-game

cp -R ../tank-game-unity-bootstrap/Assets/. Assets/
cp -R ../tank-game-unity-bootstrap/Packages/. Packages/
cp -R ../tank-game-unity-bootstrap/ProjectSettings/. ProjectSettings/
```

この手順は初回bootstrap専用である。ゲーム実装開始後に再実行しない。

## 4. Open the real repository

Unity Hubの`Add project from disk`から次を選択する。

```text
/Users/masanoryuudai/Documents/tank-game
```

Editorが開いたらPackage Managerで次を確認する。

- Universal RP
- Input System
- Test Framework

不足しているpackageのみUnity Registryから追加する。package versionはEditorが解決した`Packages/packages-lock.json`を正とし、推測で手入力しない。

Input System導入時に入力バックエンド切替を求められた場合は`Input System Package (New)`を選択し、Editor再起動を許可する。

## 5. Verify

Unity Editorを終了し、ターミナルで実行する。

```bash
cd /Users/masanoryuudai/Documents/tank-game
bash scripts/verify-unity-bootstrap.sh
git status --short
```

検証スクリプトが`Unity bootstrap verification: PASS`を出力すること。Unity Console Errorが0件であること。両方を満たすまでコミットしない。

## 6. Commit

検証成功後に実行する。

```bash
git add Assets Packages ProjectSettings docs scripts README.md
git commit -m "build: bootstrap Unity URP project"
git push
```

`Library`、`Temp`、`Logs`、`UserSettings`は生成物でありコミットしない。`.gitignore`で除外済みである。
