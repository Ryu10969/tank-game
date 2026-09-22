# CURRENT_STATE

Last updated: 2026-09-22

## Overall status

`COMBAT INTERACTIONS VERIFIED — WEB BUILD READY`

今回の成果はStage 1〜2の拡張基盤。Phase 0全体の完成ではない。

## Implemented

- Unity 6000.3.11f1 / URP 17.3.0 / Input System 1.19.0 / Test Framework 1.6.0。
- Company `Ryu10969` / Product `Tank Game`。Mainのみビルド対象。SampleSceneとmeta削除。
- 画面基準WASD、正規化した斜め移動、独立した車体・砲塔、マウス照準、単発クリック。
- 同時自弾3発、消滅時の枠回復。両陣営共通で1回反射し、2回目の壁接触で消滅。
- SphereCastと明示的な法線反射。残距離の同フレーム処理。物理マテリアル反発なし。
- Projectile同士は陣営を問わず、同一tickの相対運動によるcontinuous TOIで相殺し、双方の発射枠を既存Despawn経路で回復。
- Player直下の3-slot Ammo Indicator。上部数値AMMO表示とEnemy Indicatorはなし。
- 1発撃破。Stage 1はMobile 1体、Stage 2はMobile＋Sentryの2体で、全Enemy撃破時だけclear。
- Stage 2にProjectile 1 hitのDestructible Wall 1枚と、Tankの移動線分との接触で1回発動する中立Mine 1個。
- Defeat停止とRestartによるStage 1全再生成。
- プリミティブと単色材質、Tank追従Ammo slot・勝敗・RestartのみのUI。
- Core / Gameplay / Input / Presentation / Editor / EditMode / PlayModeのassembly分離。
- ScriptableObjectにStage・ゲーム数値・BOT・仮表示を集約。Stage 1とStage 2をID順に登録。
- StageProgressionが連続Stageのindexを管理し、共通FactoryがStageごとのRuntime rootを再生成。
- BOTは射線なし・射撃後・移動時間切れに巡回点を切り替え、射撃間隔中も位置取りを継続。
- Stage validator、テストスクリプト、Webビルドスクリプト、出典記録。

## Verification

- Unity 6000.3.11f1 batchmode: EditMode **21/21 PASS**、PlayMode **100/100 PASS**、計121件、失敗/skip/inconclusiveなし。
- `scripts/verify-unity-bootstrap.sh`: PASS。Main単独ビルド登録・Company/Product・Input System・生成物除外を確認。
- Build前検証はMainの`SliceBootstrap.stages`実配列を順序変更せず検証し、誤順序をEditModeで検証。
- Stage 1/2ロード、Update経由Stage 1→2、最終Victory、Stage 2 Defeat→Stage 1 Restart、両陣営1反射、実Collider接続のBOT位置取りをPlayModeで検証。
- WebGL build: **PASS**（59,679,667 bytes、非圧縮）。前回のローカルHTTP確認ではStage 1、Tank直下3-slot、射撃時の`●→○`、上部数値AMMO非表示、ブラウザconsole warning/error 0件を確認済み。
- moving-vs-moving Projectile clash、全actual static/clash候補のglobal TOI順序、絶対最小TOI基準のepsilon集合と仕様priority、全6列挙順・global境界値、muzzle区間の最新Physics pose、接線許容差、反射後clash、登録/Collider生成順非依存、TankのMine横断、未知EnemyBehavior拒否をPlayMode/EditModeで自動検証。Stage 2複数Enemy/Sentry、破壊壁、全Enemy clear、cleanupの既存検証もPASS。今回ブラウザ手動確認は再実施していない。
- Stage 1→2とStage 2最終VictoryはPlayModeで自動検証。ブラウザ手動操作ではStage 2遷移と壁接触2回目の消滅を再現確認していない。
- Unity起動/終了時のライセンス更新、debugger port、thread cleanupのメッセージ、およびWebビルド中のUnity.Collections型解決メッセージ3件は残る。詳細はIMPLEMENTATION_REPORT.md。
- ログ・XML: `Logs/Verification`（Git対象外）。
- 初回Web実行のCapsuleCollider strippingをlink.xmlで修正。Restartクリックの射撃漏れも修正し、再テスト/再ビルド/ブラウザ再確認済み。

## Prototype decisions

詳細な数値と現在の受入条件は `GAMEPLAY_SPEC.md` §10。
Stageデータ方式とPhase 0全体に対する位置づけは `adr/ADR-0002-stage-data-and-core-slice.md`。
仮モデルの寸法・色は `Assets/_Project/Data/PrototypePresentation.asset` と各material。
外部・AI生成メディアなし。`ASSET_PROVENANCE.md`参照。

## Not implemented / remaining work

- Stage 3〜10とPhase 0完了画面。
- 本番アート、木目画像、VFX、BGM/SFX、音量UI、最終アート承認。
- 強化、追加Enemyタイプ、PvP/協力、モバイル固有操作、ガチャ/課金/セーブ。
- 公開ホスティング、第三者プレイテスト、成功指標の目標値確定。
- BOTの跳弾予測（設定はfalse）、任意角度壁/任意寸法通路の一般的な経路検証。

## Git delivery

- main上の初期構成: `d4b94cf` — `chore: initialize Unity 6 URP project`。
- その直後に `git switch -c feat/core-vertical-slice`。ゲーム実装はこのブランチのみ。
- 既存 `.gitignore` の `.slnx` 追加を保持し初期構成へ含めた。
- pushは実行しない。
