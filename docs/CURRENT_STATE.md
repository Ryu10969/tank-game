# CURRENT_STATE

Last updated: 2026-09-21

## Overall status

`UNITY BOOTSTRAP VERIFIED — READY FOR CORE VERTICAL SLICE`

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

## Bootstrap verification

- Unity 6000.3.11f1 batchmode import/compile: exit 0、コンパイルエラーなし。
- `scripts/verify-unity-bootstrap.sh`: PASS。
- URP 17.3.0 / Input System 1.19.0 / Test Framework 1.6.0（既存lock file）。
- Company: Ryu10969 / Product: Tank Game。
- SampleSceneとmetaを削除。Mainのみを有効な先頭ビルドシーンに登録。
- 既存の `.slnx` 除外変更を保持。
- sandbox内ではUPM socketが拒否されたため、許可されたsandbox外実行で検証。
- import終了時にCurl error 42（callback aborted）が1件。コンパイルエラーではない。

## Next action

初期構成コミット直後に `feat/core-vertical-slice` を作成し、1ステージの中間Vertical Sliceを実装する。
