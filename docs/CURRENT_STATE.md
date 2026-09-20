# CURRENT_STATE

Last updated: 2026-09-20

## Overall status

`UNITY BOOTSTRAP REQUIRED — NOT READY FOR GAMEPLAY IMPLEMENTATION`

## Completed

- リポジトリ名を `tank-game` に決定
- Phase 0をWeb Vertical Sliceに限定
- 技術基盤をUnity 6 / C# / URPに決定
- Product Charter初稿
- Gameplay Specification初稿
- Art Direction初稿
- IP Guidelines初稿
- Architecture初稿
- Git除外設定
- Codex向け作業規則
- PC操作方式を確定
- ADR-0001を作成
- Unity bootstrap手順を作成
- Unity bootstrap検証スクリプトを作成

## Not completed

- GAMEPLAY_SPECに残る移動速度・弾丸数値の確定
- 成功指標の目標値確定
- Unityプロジェクト生成
- Unity package versionの確定
- EditMode / PlayModeテスト基盤
- ゲームコード
- アートアセット
- Webビルド
- プレイテスト

## Confirmed controls

- 移動: `W/A/S/D`、画面基準
- 照準: マウスカーソル
- 射撃: 左クリック、押下1回につき1発
- タッチ操作: Phase 0 PC操作確立後

## Current blocker

Unity Editorを使用していないため、`Assets`、`Packages`、`ProjectSettings`の正式な生成とコンパイル検証が未完了。

## Next action

MacのUnity HubでUnity 6000.3.11f1とWeb Build Supportを導入し、`docs/UNITY_SETUP.md`に従って`Universal 3D`プロジェクトを生成する。`scripts/verify-unity-bootstrap.sh`成功後に移動・照準の実装へ進む。
