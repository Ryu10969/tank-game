# ARCHITECTURE

Status: Draft for owner review
Last updated: 2026-09-22

## 1. Baseline

| Item | Decision |
|---|---|
| Engine | Unity 6000.3.11f1 |
| Language | C# |
| Render pipeline | URP |
| Camera | Orthographic 3D |
| Phase 0 target | Web |
| Input | Unity Input System |
| Automated tests | Unity Test Framework |
| Online SDK | Phase 0では導入しない |
| Backend | Phase 0では作らない |

Unity 6000.3.11f1は、公式リリースページでmacOS、macOS ARM64およびWebGL build supportが提供されていることを確認した固定版である。最新版へ自動追随せず、変更時は検証ブランチでWebビルドとテストを再実行する。

プロジェクトはUnity Hubの`Universal 3D`テンプレートから生成する。URP、Input System、Test Frameworkが`Packages/manifest.json`に存在し、`ProjectSettings/ProjectVersion.txt`が固定Editor版と一致した場合のみbootstrap完了とする。

## 2. Dependency direction

```text
Presentation -> Gameplay -> Core
Input --------> Gameplay
Stage --------> Gameplay -> Core
Audio --------> Gameplay events
```

- `Core` は可能な限り `UnityEngine` に依存させない。
- `Gameplay` はUnity上の実行状態とCoreルールを接続する。
- `Presentation` はモデル、VFX、UI、カメラを扱う。
- `Input` は人間、BOT、将来のNetwork入力を共通の命令へ変換する。

## 3. Planned assemblies

```text
TankGame.Core
TankGame.Gameplay
TankGame.Input
TankGame.Presentation
TankGame.Tests.EditMode
TankGame.Tests.PlayMode
```

Unity公式文書が示すAssembly DefinitionとTest Assemblyの構成に従い、プロダクションコードとテストを分離する。

## 4. Controller boundary

操作主体は次の契約へ統一する。

```csharp
public interface ITankController
{
    TankCommand ReadCommand(in TankObservation observation);
}
```

Phase 0では次を実装する。

- `HumanTankController`
- `BotTankController`

将来の `NetworkTankController` はPhase 0では作らない。

`HumanTankController`はUnity Input Systemから入力を読み取るが、Gameplay層へ渡す値は移動ベクトル、照準点、単発の射撃要求に限定する。キーコードやマウスAPIをGameplay層へ漏らさない。

## 5. Projectile architecture

跳弾はRigidbodyの反発結果に依存させず、次の順に処理する。

1. 現在位置と当該フレームの移動距離を求める。
2. RaycastまたはSphereCastで最初の衝突を求める。
3. 衝突対象を分類する。
4. 壁なら法線から反射方向を計算する。
5. 残り距離を同一フレーム内で処理する。
6. 戦車ならHit eventを発行する。
7. 寿命、または許容反射回数を使い切った後の次の壁衝突でDespawnする。

ProjectileはCollider callbackではなく同じSphereCast経路でProjectile、Destructible Wall、Tank、
Normal Wallを分類する。全候補の絶対最小TOIを先に固定し、そのTOIからepsilon以内の候補だけを
Projectile、Destructible Wall、Tank、Normal Wallの順で比較する。比較集合は途中の選択候補に
依存して広げない。`IsAlive`をterminal guardとして使用し、Projectile clashは双方の既存Despawnを
呼び、発射枠の二重解放を防ぐ。

反射ベクトル計算、残距離計算、発射枠管理はEditModeテスト対象とする。

## 6. Stage architecture

- Stage定義はScriptableObjectまたはJSONのどちらか一方に統一する。
- 選択はStage 1の実装前にADRで確定する。
- RuntimeのScene hierarchyを唯一のStage情報源にしない。
- ValidatorをEditor上およびCIから実行可能にする。

## 7. State flow

```text
Boot -> LoadStage -> Playing -> StageClear -> LoadStage
                            \-> PlayerDefeated -> LoadStage(1)
```

遷移要求は1回だけ受理し、同一フレームの重複イベントで二重ロードしない。

## 8. Future boundaries

Photon Fusion、Unity Authentication、Cloud Save、Cloud Code、Remote Config、IAPは別Phaseで追加する。Phase 0の型やフォルダへSDK依存を先行導入しない。

WebGLでオンライン対応するPhaseでは、Photon公式がWebGLにShared Modeを強く推奨していることを前提に再評価する。ただし競技性と不正耐性の要件確定後にAuthority modelをADRで決める。

## 9. Quality gates

各機能は次を満たすまで完了としない。

- 仕様の受入条件が存在する。
- 該当するEditModeまたはPlayModeテストが成功する。
- Unity Consoleに新規Errorがない。
- Web対象コードで未対応APIを使用していない。
- `CURRENT_STATE.md` が実態と一致する。

## Core slice milestone

ADR-0002とGAMEPLAY_SPEC §9に従い、Stage 1のVictory/Defeat→Restartのみを実装する。
Stage定義はScriptableObject。EditorコードはTankGame.Editorへ分離する。

## Two-stage foundation

GAMEPLAY_SPEC §10に従い、Main上のSliceBootstrapが順序付きStageDefinition一覧を持つ。
Stage IDと一覧indexの対応はStageValidatorが検証する。Stage切替はシーンを追加ロードせず、
現在のRuntime rootを破棄して次のStageDefinitionから共通Factoryで再生成する。
Stage 3以降はStageDefinitionを追加して一覧へID順に登録する。

Stage進行のindex管理はCoreのStageProgressionへ分離する。PresentationはGameSessionの
終端状態を監視し、次StageがあるVictoryだけを1回受理する。最終StageのVictoryとDefeatは
停止し、RestartはStage 1へ戻す。Player/BOTの砲弾は同じGameplay実装を共有する。

## Combat interaction extension

StageDefinitionはStageEnemy配列を持ち、Enemy単位でSpawn、巡回点、BotSettings、Mobile/Sentryを指定する。
GameSessionのMatchRulesはEnemy総数を受け取り、最後のEnemy撃破だけをStage clearにする。
Destructible WallとMineはGameplay actor、形状とAmmo IndicatorはPresentationが担当する。
これらはすべてGameSession以下へ生成し、既存Runtime rootの破棄だけでcleanupする。
Mine判定はGameSessionのTank更新直後に行い、Projectile collision maskには含めない。
Tankのtick開始位置と移動後位置をXZ平面の線分としてMine trigger円と比較し、開始時重複、
移動中の横断、終了時重複を同じone-shot damage経路で処理する。

Projectileのproduction tickは全alive Projectileの位置、方向、速度、半径、残り移動時間を
同じ時点で扱う。Projectile同士は相対運動へ変換したswept sphere同士のtime of impactを求め、
各ProjectileのTank、Normal Wall、Destructible WallへのSphereCast hitを局所選択せず、actual TOIを
持つ個別interactionとしてProjectile clashと同じglobal候補集合へ追加する。全interactionの絶対最小TOIを
固定してepsilon集合を作り、その集合内はGAMEPLAY_SPEC §11の
Projectile、Destructible Wall、Tank、Normal Wallの順とruntime identityによる安定キーで決定する。
local minimumと別hitのpriorityを組み合わせない。反射後は候補を再収集して残り時間へ同じ判定を繰り返すため、Projectile登録順や
Physics query順へ結果を依存させない。発射直後のmuzzle区間はquery直前にTransformをPhysicsへ同期し、
同じpriority comparatorでProjectileとstatic対象を分類する。

swept sphereの判別はdouble中間値を使い、discriminantの2項の大きさに比例した小さい許容差を
設定する。許容範囲の負値だけを0へ丸め、範囲外のnear missは接触として扱わない。

EnemyBehaviorはStageValidatorで定義済みenum値だけを許可する。runtimeのcontroller生成も
Mobile/Sentryを明示的に分岐し、未知値はMobileへfallbackせず例外にする。
