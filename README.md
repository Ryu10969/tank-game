# tank-game

反射角を読む、木製玩具調の2.5Dトップダウン戦車ゲームです。
Unity 6000.3.11f1 / URP / 新Input Systemを使用しています。
現在の成果はPhase 0の途中段階である、Stage 1だけのCore Vertical Sliceです。

## 遊び方

`Assets/Scenes/Main.unity`を開きPlayを押します。シーン内の設定参照から床・壁・戦車を実行時に生成します。

- WASD: 画面基準の移動。マウス: 砲塔の照準。
- 左クリック: 押下ごとに1発。長押しでは連射しません。
- 自弾は同時3発まで。消滅すると枠が回復します。
- 壁で2回まで反射し、3回目の壁接触で消滅します。反射後の自弾にも被弾します。
- 敵全滅でVICTORY、被弾でDEFEAT。RESTARTでStage 1を最初から遊べます。

## 検証・Webビルド

Unity Editorを閉じて実行します。`UNITY_EDITOR`で実行ファイルを指定できます。

```bash
bash scripts/verify-unity-bootstrap.sh
bash scripts/test-unity.sh
bash scripts/build-web.sh
python3 -m http.server 8000 --directory Builds/WebGL --bind 127.0.0.1
```

ブラウザで `http://127.0.0.1:8000` を開きます。Webビルドは無圧縮で、ローカルHTTPサーバーで確認できます。
ビルド成果物は`Builds/WebGL`、テストXML/ログは`Logs/Verification`に出力し、Git対象外です。
Mainだけがビルド対象です。Stage検証はEditorの`Tank Game > Validate Stages`または
batchmodeの`-executeMethod TankGame.Editor.SliceProjectBuilder.Validate`で実行できます。
`Prepare Core Slice`は既存アセットを再利用してMainの参照を設定する開発用コマンドで、通常のPlay前には不要です。

## 文書

- [Product Charter](docs/PRODUCT_CHARTER.md)
- [Gameplay Specification / 今回の受入条件と暫定数値](docs/GAMEPLAY_SPEC.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Art Direction](docs/ART_DIRECTION.md)
- [IP Guidelines](docs/IP_GUIDELINES.md)
- [Current State](docs/CURRENT_STATE.md)
- [Implementation report](docs/IMPLEMENTATION_REPORT.md)
- [Unity Setup](docs/UNITY_SETUP.md)
- [References](docs/REFERENCES.md)
- [ADR-0001: PC Controls](docs/adr/ADR-0001-pc-controls.md)
- [ADR-0002: Stage data and core slice](docs/adr/ADR-0002-stage-data-and-core-slice.md)
- [Asset provenance](docs/ASSET_PROVENANCE.md)

Phase 0全体のStage 1〜10、音声、最終品質アート、第三者プレイテストは未完了です。
オンライン、課金、セーブ、地雷、強化、複数敵タイプ、モバイル固有操作は今回の対象外です。
