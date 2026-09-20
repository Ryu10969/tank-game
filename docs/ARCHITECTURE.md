# ARCHITECTURE

Status: Draft for owner review
Last updated: 2026-09-20

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

## 5. Projectile architecture

跳弾はRigidbodyの反発結果に依存させず、次の順に処理する。

1. 現在位置と当該フレームの移動距離を求める。
2. RaycastまたはSphereCastで最初の衝突を求める。
3. 衝突対象を分類する。
4. 壁なら法線から反射方向を計算する。
5. 残り距離を同一フレーム内で処理する。
6. 戦車ならHit eventを発行する。
7. 寿命または最大反射回数に達したらDespawnする。

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
