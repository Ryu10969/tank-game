# Core Vertical Slice implementation history

## Review follow-up: Burst LOS cancellation and obsolete Mine API

Date: 2026-09-23 (JST)
Branch: `feat/enemy-variety-stage3-presentation`

- Burstのpending shotがLOSを再確認していなかったため、各pending shot前に`TankObservation.ClearShot`を検査するよう修正。喪失時は残弾を破棄して通常reloadへ移行し、LOS回復時に旧burstを再開しない。
- 実Colliderで`1発目 → LOS遮断 → 残り2発キャンセル → reload中は停止 → LOS回復・reload完了後に新規3連射`をproduction pathで検証。
- repository全体で参照がない旧Stage配置Mine由来の`GameSession.Register(MineActor)`を削除。`TryPlaceMine`のlist追加、lifecycle、cleanup、resetは変更なし。
- 最終検証はbootstrap PASS、EditMode 25/25、PlayMode 118/118、計143件、WebGL 59,698,536 bytes、failed / skipped / inconclusive 0、C# compiler error / warning 0 / 0、`git diff --check` PASS。

## Current update: Mine explosion VFX and Projectile owner immunity

Date: 2026-09-23 (JST)
Branch: `feat/enemy-variety-stage3-presentation`

- Mine爆発はdamageとPresentation生成を分離し、既存Sphere primitiveとMine materialで半径1.05を0.30秒で示すgraybox VFXを1回生成する。Mine破棄後も寿命まで残り、Runtime root破棄でcleanupされる。
- ProjectileActorは発射時Tankを`Owner`として保持し、反射後もownerのroot / child ColliderをSphereCast候補から除外する。global TOI comparator、Projectile clash、他Tankへのfriendly-fireは変更していない。
- Player / Mobile / Sentry / Heavy / Burstの直射owner immunity、Playerの反射後immunity、複数Collider、owner後方のWall / Tank、VFXの1回生成・寿命・Stage切替 / Restart cleanupをPlayModeに追加した。
- 最終検証はbootstrap PASS、EditMode 25/25、PlayMode 117/117、計142件、WebGL 59,698,525 bytes、failed / skipped / inconclusive 0、C# compiler error / warning 0 / 0、`git diff --check` PASS。
- 今回のMine Explosion VFXとowner immunityのWebGL手動操作は未実施。自動テストとbuildはPASS。

## Current update: enemy variety, Stage 3, and presentation

Date: 2026-09-23 (JST)
Branch: `feat/enemy-variety-stage3-presentation`

- `TankLife`を耐久値指定可能に最小一般化し、緑のHeavyだけを2 hit、移動1.2、弾速7にした。
- 青のBurstは既存Mobile controllerを包むschedulerで、0.18秒間隔の3連射、2.0秒reloadを実装。各弾は共通`TryFire` / `ShotSlots`を通る。
- 旧Stage配置・接触起爆MineとStage dataを削除。QでPlayer位置へ各Stage 2回設置、1秒後に半径1.05で1 damageを各1回適用する。
- Stage 3にHeavy 1体、Burst 1体、Normal Wall、Destructible Wallを配置。初期LOSは壁で遮蔽する。
- `STAGE N` 0.5秒 → `GO!` 0.5秒 → Playing、全滅後`STAGE CLEAR` 0.75秒 → 次Stage / Final Victoryを実装。演出中はGameSession gateで全gameplayを停止する。
- `EnemyBehavior`に加え`EnemyArchetype`もvalidator/runtimeの両方でfail-fast。Heavy/Burst/Mine/進行/演出と既存回帰を自動テスト化した。
- 外部・AI生成assetなし。Heavy/Burstの識別色materialは既存Editor builderでローカル生成したgraybox。
- 最終検証はbootstrap PASS、EditMode 25/25、PlayMode 107/107、WebGL 59,693,526 bytes、
  failed / skipped / inconclusive 0、C# compiler error / warning 0 / 0、`git diff --check` PASS。
- ローカルWebGLでStage 1 intro、Ammo slots、Q Mine、`MINES 2 → 1`、約1秒後の自爆Defeat、
  Restart後の2 reset、browser console warning/error 0件を確認。狭幅時のMine HUD切れを発見・修正し、再確認した。
- Stage Clear、Stage 3のHeavy/Burst、Stage 3 Clear→Final VictoryのWebGL手動操作は未実施。該当PlayModeテスはPASS。

以下のStage配置Mine、Stage 2最終Victory、過去のテスト件数は履歴記録であり、
現行仕様はGAMEPLAY_SPEC §§12、13とCURRENT_STATEを優先する。

## Current update: combat interactions and Stage gimmicks

Date: 2026-09-22 (JST)

- Projectile clash、Player追従3-slot Ammo Indicator、複数Enemy、Sentry、Destructible Wall、Mineを既存Runtime rootへ統合。
- Stage 1配置は維持。Stage 2はMobile (-6,4)、Sentry (6,4)、破壊壁 (4,0; 1×2)、Mine (2,-4)。
- Projectile/Tank/Wallの判定はcallbackではなく、静的対象へのSphereCast TOIとProjectile相対運動のcontinuous TOIを比較し、terminal guard付きで発生順に処理する。
- production tickは全Projectileの全actual static hitとProjectile clashを1つのglobal候補集合へ入れ、絶対最小TOIからepsilon集合を固定する。その集合内をProjectile、Destructible Wall、Tank、Normal Wallのpriorityとruntime identityの安定キーで決定し、local winnerの時刻とpriorityは混在させない。muzzle query直前にもTransformをPhysicsへ同期する。
- swept-sphere discriminantはdouble中間値と係数scale比例の許容差を使い、極小負値だけを0へ丸める。
- MineはTankのtick開始位置から移動後位置までの線分とtrigger円を判定し、高deltaでも横断を検出する。未知EnemyBehaviorはvalidatorとruntime controller生成の双方で拒否する。
- EditMode 21/21、PlayMode 100/100（epsilon chain全6列挙順、global production順序、境界値を含む）、bootstrap、WebGL build（59,679,667 bytes、非圧縮）はすべて成功。
- ローカルWebGLでStage 1、Player直下の3-slot、射撃時のfilled→outline、上部数値AMMO非表示を確認し、
  ブラウザconsole warning/errorは0件。Stage 2複合interactionはPlayModeで自動検証した。

## Current update: two-stage foundation

Date: 2026-09-22 (JST)

- Stage 1〜2をMain内の順序付きStageDefinition一覧で管理する。
- Stage 1クリアでStage 2へ一度だけ遷移し、Stage 2クリアで最終Victoryになる。
- 両陣営のProjectileActorは同じmaximumReflections=1を使い、2回目の壁接触で消滅する。
- BOTは射線なしと射撃後に巡回へ戻り、射撃間隔中も位置取りを続ける。
- Stage 2はPlayer (6,-4)、Enemy (-6,4)、中央壁6×2、巡回点
  (-6,-4), (-4,0), (-6,4) のGraybox。新ギミック・新アートなし。
- EditMode 20/20、PlayMode 26/26、bootstrap検証、WebGL build（59,538,081 bytes、非圧縮）はすべて成功。
- Build前検証はMainの`SliceBootstrap.stages`を並べ替えず検証する。誤順序、反射0回境界、
  Update経由遷移、Stage 2敗北後Restart、実Collider接続のBOT位置取りを追加テストで確認した。
- ローカルHTTPの最新WebGLでStage 1起動、WASD移動、敵の移動・射撃、Defeat、Restartを確認し、
  ブラウザconsoleのwarning/errorが0件であることを確認した。
  Stage 1→2、Stage 2最終Victory、両陣営の2回目壁接触による消滅はPlayModeで自動検証したが、
  今回のブラウザ手動操作では再現確認していない。

以下は2026-09-21の1 Stageベースライン実装記録であり、反射回数、Stage数、テスト件数は
上記更新およびCURRENT_STATE、GAMEPLAY_SPEC §10が現在値となる。

Date: 2026-09-21 (JST)
Branch: `feat/core-vertical-slice`

## Delivered

Stage 1の2.5DゲームループをMainだけで実行する。画面基準WASD、独立砲塔のマウス照準、
クリック単発射撃、同時自弾3発、最大2回の法線反射、次の壁接触で消滅、1発撃破、
BOT 1体、Victory/Defeat、Stage 1再開始を実装した。
Unity Physics Materialの反発を使用しない。CoreはUnityEngine参照なし。
入力、物理連携、表示、Editor処理、テストをassembly単位で分離した。
外部メディアのダウンロード、追加SDK、pushは行っていない。

## Validation

| Check | Result |
|---|---|
| Unity version | 6000.3.11f1 (3000ef702840) |
| EditMode batchmode | 16 passed / 0 failed / 0 skipped |
| PlayMode batchmode | 17 passed / 0 failed / 0 skipped |
| Bootstrap script | PASS |
| C# compiler errors / warnings | 0 / 0 |
| WebGL build | PASS, 59,538,249 bytes, uncompressed |
| Final browser console errors / warnings | 0 / 0 |
| Main build scene | Only enabled scene, index 0 |
| Git generated-file exclusions | Library, Temp, Logs, UserSettings, .slnx ignored |

EditModeは発射枠、2反射上限、面法線反射、残距離、1発撃破、勝敗の終端性、Stage validatorを検証。
PlayModeはMainの参照、実際のSphereCast壁反射、複数接触、壁越し銃口、被弾勝敗、再開始破棄、
寿命、反射後自爆、移動制限・斜め速度・フレーム非依存、BOT、照準保持、クリック解放待ちを検証。
寿命テストは衝突対象がない場所で期限前の生存と期限後の消滅を確認する。

ブラウザではWによる上方向移動、砲塔照準、射撃、弾数表示、反射弾による撃破・VICTORY、
敵弾によるDEFEAT、Restartを確認した。最終ビルドではRestart直後に3/3を保持し、
その後の射撃で減少、外壁を使った跳弾でVICTORYになることを再確認した。
左右/下方向、クリック長押し、全解像度・複数ブラウザの網羅的な手動操作は未実施。
Unity Editor GUIのPlayボタンによる手動確認も未実施。MainはPlayModeで自動ロードしている。

## Reproduce

Unity Editorを閉じてリポジトリ直下で実行する。

```bash
bash scripts/verify-unity-bootstrap.sh
bash scripts/test-unity.sh
bash scripts/build-web.sh
python3 -m http.server 8000 --directory Builds/WebGL --bind 127.0.0.1
```

Browser: <http://127.0.0.1:8000/>。公開デプロイではなくローカルHTTPでの確認。
ログ/XMLは`Logs/Verification/{EditMode,PlayMode}.{log,xml}`、Webログは`Logs/Verification/WebGL.log`。
実際のUnityコマンドは上記scripts内に保存。sandbox内のUPM socket制限があったため、
Unity batchmodeとローカルサーバーは承認されたsandbox外実行を使用した。

## Remaining messages and fixed issues

C#コンパイルエラー/警告と最終ブラウザの実行エラー/警告は0。
ただしUnityログが完全に無警告という意味ではない。

- 起動時: `Access token is unavailable; failed to update`。既存ライセンスで起動し、各処理はexit 0。
- 終了時: `debugger-agent: Unable to listen on 43`、`Thread ... may have been prematurely finalized`。
- Web build: Unity.CollectionsのNativeBitArrayDispose、NativeRingQueueDispose、NativeTextDisposeについて、
  AtomicSafetyHandle型の解決メッセージ3件。パッケージ内部であり今回変更していない。
  ビルド成功と実プレイは確認したが、メッセージ自体は未解消。
- 初期bootstrapの終了時にはCurl error 42も1件あった。最終テスト/ビルドログでは発生していない。
- 初回Web実行で実行時Cylinder生成に必要なCapsuleColliderがstrippingされていた。
  `Presentation/link.xml`で標準プリミティブ依存型を保持し、最終ブラウザで解消を確認。
- Restartクリックが発射へ流れる問題はFirePressGateで解放待ちを追加し、テストとブラウザで解消を確認。

## Temporary values

GAMEPLAY_SPEC §9の表が数値の記録。主要値は以下。

- プレイヤー速度4、車体旋回540°/s、砲塔旋回720°/s、戦車半径0.5、平面Y 0.55。
- 弾速10、半径0.1、寿命8秒、同時3発、反射2回、分離距離0.002、銃口距離0.9。
- BOT反応0.7秒、射撃間隔2秒、速度1.5、移動判断1.2秒、到着距離0.15、誤差0°、許容角1°、跳弾予測false。
- Stage 20×14、外壁厚1、高さ1.5、中央壁2×4、Spawn (-6,-3)/(6,3)、巡回(6,-3)/(6,3)。
- カメラ俯角60°、Orthographic Size 10.5（狭い画面は全幅が入るよう起動時調整）。
- 仮モデル寸法はPrototypePresentation.asset、6色はData内materialへ保存。Smoothness 0.05。
- 自弾は初回反射まで射手を無視し、その後は自爆可能。弾同士の衝突なし。

## Not delivered

Stage 2〜10と次Stage進行、地雷、性能強化、複数敵タイプ、PvP/協力、モバイル固有操作、
ガチャ/課金/セーブ、本番画像・音楽・効果音、VFX、最終アート、公開ホスティング、第三者プレイテスト。
これらは今回の対象外。Phase 0全体の完了は宣言しない。

## Commits

- `d4b94cf` — `chore: initialize Unity 6 URP project` (main)。
- 直後に`git switch -c feat/core-vertical-slice`を実行し、ブランチ名を確認してゲーム実装を開始。
- 本レポートを含むコミット: `feat: implement core tank gameplay vertical slice`。
  ハッシュは最終応答と`git log -1 --oneline`で確認できる（コミット自身のハッシュを本文へ埋め込まない）。
- pushなし。既存の`.slnx`除外変更は初期構成コミットへ保持。

## Files

初期構成コミットは既存のAssets/Settings、InputSystem_Actions、Packages、ProjectSettingsとmetaを保存し、
Company/Product、Main単独登録、SampleScene削除、CURRENT_STATEを整理した。
以下は実装コミットで作成・変更したファイル（対応するmetaもすべてコミット）。
UnityのWebビルドが更新したURPのshader prefilter/runtime設定とWebGLビルド設定も保持する。

- `Assets/Scenes/Main.unity`
- `Assets/Settings/DefaultVolumeProfile.asset`
- `Assets/Settings/Mobile_RPAsset.asset`
- `Assets/Settings/PC_RPAsset.asset`
- `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`
- `Assets/_Project/Core/CombatRules.cs`
- `Assets/_Project/Core/TankGame.Core.asmdef`
- `Assets/_Project/Data/BotSettings.asset`
- `Assets/_Project/Data/Enemy.mat`
- `Assets/_Project/Data/Floor.mat`
- `Assets/_Project/Data/GameplaySettings.asset`
- `Assets/_Project/Data/Player.mat`
- `Assets/_Project/Data/Projectile.mat`
- `Assets/_Project/Data/PrototypePresentation.asset`
- `Assets/_Project/Data/Stage1.asset`
- `Assets/_Project/Data/Trim.mat`
- `Assets/_Project/Data/Wall.mat`
- `Assets/_Project/Editor/SliceProjectBuilder.cs`
- `Assets/_Project/Editor/TankGame.Editor.asmdef`
- `Assets/_Project/Gameplay/BotSettings.cs`
- `Assets/_Project/Gameplay/CollisionQueries.cs`
- `Assets/_Project/Gameplay/GameSession.cs`
- `Assets/_Project/Gameplay/GameplaySettings.cs`
- `Assets/_Project/Gameplay/ProjectileActor.cs`
- `Assets/_Project/Gameplay/StageDefinition.cs`
- `Assets/_Project/Gameplay/StageValidator.cs`
- `Assets/_Project/Gameplay/TankActor.cs`
- `Assets/_Project/Gameplay/TankCommand.cs`
- `Assets/_Project/Gameplay/TankGame.Gameplay.asmdef`
- `Assets/_Project/Input/AimMemory.cs`
- `Assets/_Project/Input/BotTankController.cs`
- `Assets/_Project/Input/FirePressGate.cs`
- `Assets/_Project/Input/HumanTankController.cs`
- `Assets/_Project/Input/TankGame.Input.asmdef`
- `Assets/_Project/Presentation/PrimitiveFactory.cs`
- `Assets/_Project/Presentation/PrototypePresentation.cs`
- `Assets/_Project/Presentation/SliceBootstrap.cs`
- `Assets/_Project/Presentation/TankGame.Presentation.asmdef`
- `Assets/_Project/Presentation/link.xml`
- `Assets/_Project/Tests/EditMode/CoreRulesTests.cs`
- `Assets/_Project/Tests/EditMode/StageValidatorTests.cs`
- `Assets/_Project/Tests/EditMode/TankGame.Tests.EditMode.asmdef`
- `Assets/_Project/Tests/PlayMode/SlicePlayTests.cs`
- `Assets/_Project/Tests/PlayMode/TankGame.Tests.PlayMode.asmdef`
- `ProjectSettings/ProjectSettings.asset`
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/ASSET_PROVENANCE.md`
- `docs/CURRENT_STATE.md`
- `docs/GAMEPLAY_SPEC.md`
- `docs/IMPLEMENTATION_REPORT.md`
- `docs/adr/ADR-0002-stage-data-and-core-slice.md`
- `scripts/build-web.sh`
- `scripts/test-unity.sh`
- `scripts/verify-unity-bootstrap.sh`
