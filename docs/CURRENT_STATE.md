# CURRENT_STATE

Last updated: 2026-09-23

## Overall status

`OWNER IMMUNITY / MINE EXPLOSION VFX VERIFIED — WEB BUILD READY`

今回の成果はStage 1〜3の拡張基盤。Phase 0全体の完成ではない。

## Implemented

- Unity 6000.3.11f1 / URP 17.3.0 / Input System 1.19.0 / Test Framework 1.6.0。Mainのみビルド対象。
- 画面基準WASD、マウス照準、単発クリック、同時自弾3発、Tank直下の3-slot Ammo Indicator。
- 両陣営共通のProjectileActor、1回反射、global TOI、Projectile clash、Destructible Wall、明示的な面法線反射。Projectileは反射後もowner Tankのroot / child Colliderを常に無視する。
- Standard / Sentry / Burstは耐久値1。Heavyは緑、耐久値2、移動1.2 unit/s、弾速7 unit/sで既存Mobile AIを再利用。
- Burstは青、0.18秒間隔の3連射、2.0秒reload。各pending shot前にLOSを再確認し、喪失時は残弾をキャンセルしてreloadへ移行する。共通`TryFire()` / `ShotSlots`、3発上限、terminal/death/transition停止を維持。
- QのPlayer Mine。各Stage 2回、Player位置へ設置、1秒後に半径1.05で1 damage。接触非起爆、同一Tankへ1回、HUD残数表示。爆発時は別lifecycleの球状graybox VFXが0.30秒で直径2.10まで拡大する。
- Stage事前配置Mine、関連Stage data、contact trigger経路は削除。MineはStage終了・遷移・Restartでcleanupし、残数はStageごと2へreset。
- Stage 1→2→3→Final Victory。RestartはStage 1。Stage 3はHeavy 1体、Burst 1体、Normal Wall、Destructible Wallを持つ。
- 各Stageの`STAGE N` 0.5秒 → `GO!` 0.5秒、全滅後の`STAGE CLEAR` 0.75秒 → 次Stage / Final Victory。演出中はGameSession gateで全gameplayを停止。
- `EnemyBehavior`と`EnemyArchetype`のvalidator/runtime fail-fast、新settings、Stage 3内訳、presentation material参照のbuild validation。
- Core / Gameplay / Input / Presentation / Editor / EditMode / PlayModeのassembly分離、ScriptableObject Stage data、Runtime root cleanup。

## Verification

- `scripts/verify-unity-bootstrap.sh`: **PASS**。
- Unity batchmode: EditMode **25/25 PASS**、PlayMode **118/118 PASS**、計143件。failed / skipped / inconclusiveはすべて0。
- Heavyの2 hit、1 hit目のEnemy残数維持、速度差、Burstの3連射・間隔・reload・3発上限・停止条件に加え、途中LOS喪失の残弾キャンセルとreload後の新burstをproduction経路で検証。
- Mineの2回上限、1秒ヒューズ、接触非起爆、Player/Standard/Heavy damage、1 explosion 1 hit、cleanup/reset、presentation/terminal lockを検証。
- Mine VFXの1回生成、半径表示、独立寿命、Stage切替 / Restart cleanup、およびPlayer / Mobile / Sentry / Heavy / Burstの直射・反射後owner immunityと複数Collider、owner後方候補処理をproduction経路で検証。
- Stage 3 validation、Heavy×1 / Burst×1、初期LOS遮蔽、Stage 1→2→3→Victory、Restart→Stage 1を検証。
- Stage Intro / GO / Clearの表示順、時間、gameplay lock、Clear後transitionを検証。既存Projectile clash / global TOI / reflection / Ammo / Mobile / Sentry / Destructible Wall回帰もPASS。
- WebGL build: **PASS**（59,698,536 bytes、非圧縮）。C# compiler error / warningは0 / 0。
- ローカルWebGLで`STAGE 1`、Ammo slots、`MINES 2 → 1`、Q設置、約1秒後の自爆Defeat、Restart後のMine残数2 resetを手動確認。browser console warning/error 0件。
- 狭幅browserでMine HUD先頭が切れる問題を手動確認中に発見し、上部中央配置へ修正後に再テスト・再ビルド・再確認済み。
- Stage Clear、Stage 3の緑/2 hit Heavy、青/3連射Burst、Stage 3 Clear→Final VictoryのWebGL手動操作は未実施（自動テスはPASS）。
- 今回追加したMine Explosion VFXと反射後owner immunityのWebGL手動確認は未実施（自動テストはPASS）。
- Unity起動/終了時のlicense update、debugger port、thread cleanupのメッセージ、Web build中のUnity.Collections型解決メッセージ3件は既存のまま。詳細はIMPLEMENTATION_REPORT.md。
- ログ/XML: `Logs/Verification`（Git対象外）。

## Prototype decisions

現在の受入条件と数値は `GAMEPLAY_SPEC.md` §§12、13。StageデータとRuntime rootの方針は
`adr/ADR-0002-stage-data-and-core-slice.md`。仮モデルの寸法・色は
`Assets/_Project/Data/PrototypePresentation.asset` と各material。外部・AI生成メディアなし。

## Not implemented / remaining work

- Stage 4〜10とPhase 0完了画面。
- 新武器、Boss、VFX/SFX polish、BGM、音量UI、本番アート、最終アート承認。
- PvP/協力、モバイル固有操作、ガチャ/課金/セーブ。
- 公開ホスティング、第三者プレイテスト、成功指標の目標値確定。
- BOTの跳弾予測、任意角度壁/任意寸法通路の一般的な経路検証。

## Git delivery

- 開始点: `main` / `fb12ddae386b6f08a2190fd188808f91cfdf6b4b`。
- 作業ブランチ: `feat/enemy-variety-stage3-presentation`。
- commit / push / PR / mergeは実行しない。
