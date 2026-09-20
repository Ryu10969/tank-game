# AGENTS.md

## Source of truth

実装前に次の順で読むこと。

1. `docs/PRODUCT_CHARTER.md`
2. `docs/GAMEPLAY_SPEC.md`
3. `docs/ARCHITECTURE.md`
4. `docs/ART_DIRECTION.md`
5. `docs/IP_GUIDELINES.md`
6. `docs/CURRENT_STATE.md`

文書間に矛盾がある場合は実装せず、矛盾を報告する。仕様に存在しない数値や挙動を推測で追加しない。

## Delivery rules

- Phase 0の範囲外機能を追加しない。
- 1工程につき1つの検証可能なVertical Sliceとして実装する。
- ゲームルールは可能な限りUnity APIから分離し、EditModeでテストする。
- Unityの物理マテリアル任せで跳弾を実装しない。
- 重要な挙動変更では、先に仕様と受入条件を更新する。
- 生成アセットの出典、ライセンス、生成条件を記録する。
- Nintendo作品の名称、画像、音声、ステージ、UI、キャラクターを参照素材としてリポジトリへ入れない。
- 実装完了時に `docs/CURRENT_STATE.md` を更新する。

## Verification

変更内容に応じて以下を実行する。

- EditMode tests
- PlayMode tests
- Web build
- 該当受入条件の手動確認

実行していない検証を成功扱いにしない。
