# CURRENT_STATE

Last updated: 2026-09-20

## Overall status

`PLANNING — NOT READY FOR GAMEPLAY IMPLEMENTATION`

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

## Not completed

- プロダクトオーナーによる仕様初稿の承認
- GAMEPLAY_SPECに残る操作・弾丸数値の確定
- 成功指標の目標値確定
- Unityプロジェクト生成
- Unity package versionの確定
- EditMode / PlayModeテスト基盤
- ゲームコード
- アートアセット
- Webビルド
- プレイテスト

## Blocking decisions

次の工程へ入る前に、まず操作方式だけを確定する。

1. PCでの移動方式
2. PCでの照準方式
3. PCでの射撃入力

タッチ操作はPC版の操作感が成立した後に決める。

## Next action

プロダクトオーナーへPC操作方式の選択肢を提示し、決定後に `GAMEPLAY_SPEC.md` を更新する。その後、Unity 6000.3.11f1でURPプロジェクトを生成する。
