# CURRENT_STATE

Last updated: 2026-09-21

## Overall status

`CORE VERTICAL SLICE VERIFIED — ONE-STAGE WEB BUILD READY`

今回の成果はStage 1だけの中間Vertical Slice。Phase 0全体の完成ではない。

## Implemented

- Unity 6000.3.11f1 / URP 17.3.0 / Input System 1.19.0 / Test Framework 1.6.0。
- Company `Ryu10969` / Product `Tank Game`。Mainのみビルド対象。SampleSceneとmeta削除。
- 画面基準WASD、正規化した斜め移動、独立した車体・砲塔、マウス照準、単発クリック。
- 同時自弾3発、消滅時の枠回復、2回反射、3回目壁接触で消滅。
- SphereCastと明示的な法線反射。残距離の同フレーム処理。物理マテリアル反発なし。
- 1発撃破、BOT 1種類1体、Victory/Defeat停止、RestartによるStage 1全再生成。
- プリミティブと単色材質、装弾数・勝敗・RestartのみのUI。
- Core / Gameplay / Input / Presentation / Editor / EditMode / PlayModeのassembly分離。
- ScriptableObjectにStage・ゲーム数値・BOT・仮表示を集約。
- Stage validator、テストスクリプト、Webビルドスクリプト、出典記録。

## Verification

- Unity 6000.3.11f1 batchmode: EditMode **16/16 PASS**、PlayMode **17/17 PASS**、計33件、失敗/skipなし。
- `scripts/verify-unity-bootstrap.sh`: PASS。Main単独ビルド登録・Company/Product・Input System・生成物除外を確認。
- WebGL build: **PASS**、59,538,249 bytes。出力 `Builds/WebGL`（Git対象外）。
- 同じMainをブラウザで起動し、W移動、照準、射撃、跳弾撃破→Victory、Defeat→Restartを操作確認。
- 最終版でRestart直後のAMMO 3/3（誤射なし）と、その後の跳弾によるVictoryを確認。
- 最終ブラウザ実行のconsole error/warning: 0。C# compile error/warning: 0。
- Unity EditorのGUIでPlayボタンを押す手動確認は未実施。Mainロードはbatchmode PlayModeで検証。
- Unity起動/終了時のライセンス更新、debugger port、thread cleanupのメッセージ、およびWebビルド中のUnity.Collections型解決メッセージ3件は残る。詳細はIMPLEMENTATION_REPORT.md。
- ログ・XML: `Logs/Verification`（Git対象外）。
- 初回Web実行のCapsuleCollider strippingをlink.xmlで修正。Restartクリックの射撃漏れも修正し、再テスト/再ビルド/ブラウザ再確認済み。

## Prototype decisions

詳細な数値と中間受入条件は `GAMEPLAY_SPEC.md` §9。
Stageデータ方式とPhase 0全体に対する位置づけは `adr/ADR-0002-stage-data-and-core-slice.md`。
仮モデルの寸法・色は `Assets/_Project/Data/PrototypePresentation.asset` と各material。
外部・AI生成メディアなし。`ASSET_PROVENANCE.md`参照。

## Not implemented / remaining work

- Stage 2〜10、次ステージ進行、Phase 0完了画面。
- 本番アート、木目画像、VFX、BGM/SFX、音量UI、最終アート承認。
- 地雷、強化、複数敵タイプ、PvP/協力、モバイル固有操作、ガチャ/課金/セーブ。
- 公開ホスティング、第三者プレイテスト、成功指標の目標値確定。
- BOTの跳弾予測（設定はfalse）、任意角度壁/任意寸法通路の一般的な経路検証。

## Git delivery

- main上の初期構成: `d4b94cf` — `chore: initialize Unity 6 URP project`。
- その直後に `git switch -c feat/core-vertical-slice`。ゲーム実装はこのブランチのみ。
- 既存 `.gitignore` の `.slnx` 追加を保持し初期構成へ含めた。
- pushは実行しない。
