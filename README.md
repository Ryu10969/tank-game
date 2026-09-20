# tank-game

反射角を読むことを中心にした、木製玩具調の見下ろし型タンクゲームです。

## 現在の段階

`Phase 0: Web Vertical Slice` の仕様確定段階です。Unityプロジェクト本体は、仕様レビュー完了後に Unity 6000.3.11f1 で生成します。

Phase 0の完成条件は次のとおりです。

- 1つの木製ステージでプレイヤー戦車を操作できる。
- 同時に存在できる自弾は3発まで。
- 弾丸が壁面法線に基づいて反射する。
- プレイヤーと敵は弾丸1発で撃破される。
- BOTを全滅させると次のステージへ進む。
- プレイヤーが撃破されるとStage 1へ戻る。
- Stage 1からStage 10まで通して遊べる。
- Webビルドを第三者がブラウザでプレイできる。

## 文書

- [Product Charter](docs/PRODUCT_CHARTER.md)
- [Gameplay Specification](docs/GAMEPLAY_SPEC.md)
- [Art Direction](docs/ART_DIRECTION.md)
- [IP Guidelines](docs/IP_GUIDELINES.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Current State](docs/CURRENT_STATE.md)
- [Unity Setup](docs/UNITY_SETUP.md)
- [References](docs/REFERENCES.md)
- [ADR-0001: PC Controls](docs/adr/ADR-0001-pc-controls.md)

## 実装順

1. Unityプロジェクト生成とテスト基盤
2. Unity bootstrap検証
3. 移動・照準
4. 射撃・3発制限
5. 決定論的な反射計算
6. 被弾・撃破・再開
7. BOT 1種類
8. データ駆動のStage 1〜10
9. 木製玩具調の最終品質アセット1セット
10. Webビルドと第三者プレイテスト

オンライン対戦、アカウント、ガチャ、課金、30ステージ化はPhase 0の対象外です。
