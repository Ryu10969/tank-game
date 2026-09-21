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
