# GAMEPLAY_SPEC

Status: Draft for owner review
Last updated: 2026-09-22

## 1. Coordinate model

- 表示は3Dモデルを使う。
- ゲームプレイは水平な1平面に限定する。
- カメラはOrthographicとする。
- 高低差、ジャンプ、弾道落下はPhase 0に含めない。

カメラ角度、Orthographic Size、論理グリッド寸法はUnity上の視認性検証後に固定する。固定前にStage 1〜10を量産しない。

## 2. Tank rules

- 戦車は車体と砲塔を分離する。
- 車体の移動方向と砲塔の照準方向は独立して扱う。
- プレイヤーとBOTは有効な弾丸が1発命中すると撃破される。
- 撃破中の戦車は移動、照準、射撃を行えない。

Phase 0のPC操作は次で固定する。

- 移動: `W/A/S/D`による画面基準の平面移動
- `W`: 画面上方向
- `S`: 画面下方向
- `A`: 画面左方向
- `D`: 画面右方向
- 斜め入力は正規化し、縦横入力より速くならない。
- 車体は現在の移動ベクトルへ向かって旋回する。
- 移動は車体の旋回完了を待たない。
- 照準: マウスカーソルからゲーム平面へRaycastし、その交点へ砲塔を向ける。
- 射撃: マウス左ボタンの押下1回につき1発を要求する。
- 左ボタンを押し続けても連射しない。
- ゲーム画面外またはゲーム平面との交点を取得できない場合、直前の有効照準方向を維持する。

タッチ操作はPhase 0のPC操作感が成立した後に設計する。

次の値は最初の操作プロトタイプで固定する。
- 移動速度
- 旋回速度
- 砲塔旋回速度
- 戦車の当たり判定寸法

## 3. Projectile rules

- 1台の戦車が同時に存在させられる自弾は最大3発。
- 発射済みの弾が消滅した時点で、その発射枠が1つ回復する。
- 弾丸は水平面を一定速度で移動する。
- 重力は適用しない。
- 壁との衝突はRaycastまたはSphereCastで検出する。
- 反射方向は次式で求める。

\[
R = D - 2(D \cdot N)N
\]

`D` は衝突直前の正規化された進行方向、`N` は衝突面の正規化された法線、`R` は正規化前の反射方向である。

- UnityのPhysics Materialによる反発をゲームルールとして使用しない。
- 1フレーム内に残り移動距離がある場合、衝突地点から微小距離だけ離して残りの軌道計算を継続する。
- 同一点への連続衝突を防止する。

次の値は未決定であり、弾道プロトタイプで固定する。

- 弾速
- 弾半径
- 最大寿命
- 最大反射回数
- 発射直後に射手へ当たらないための猶予方式
- 自弾で自分を撃破できるか
- 弾丸同士が衝突するか

## 4. Damage and victory

- 敵戦車が全滅した時点でステージクリア。
- クリア後は次のステージへ進む。
- Stage 10クリア後はPhase 0完了画面を表示する。
- プレイヤーが撃破された場合、Stage 1へ戻る。
- リセット時には全戦車、全弾丸、ステージ一時状態を破棄する。

## 5. BOT v0

BOT v0は次の状態機械で構成する。

1. `Observe`: プレイヤー位置と障害物を確認する。
2. `Move`: 設定された目的地点へ移動する。
3. `Aim`: 砲塔を目標方向へ向ける。
4. `Fire`: 射撃可能かつクールダウン終了時に発射する。
5. `Recover`: 次の判断まで待機する。

Phase 0のBOT v0は機械学習を使用しない。難易度は設定値で制御する。

- `reactionTimeSeconds`
- `aimErrorDegrees`
- `fireCooldownSeconds`
- `moveSpeed`
- `ricochetAwareness`

具体値はStage設計時に固定し、各Stageデータに保存する。

## 6. Stage data

Stageはシーンへ直接ハードコードせず、データとして表現する。

必須フィールドは次のとおり。

- Stage ID
- 論理サイズ
- プレイヤーSpawn
- 敵Spawn一覧
- 壁・障害物一覧
- BOT設定参照
- テーマID

Stage validatorは最低限、次を拒否する。

- Spawnが壁と重なる。
- Spawnがステージ外にある。
- プレイヤーまたは敵が移動不能領域へ閉じ込められる。
- Stage IDが重複する。
- 必須参照が存在しない。

## 7. Audio feedback

- 発射時に発射音を1回鳴らす。
- 壁へ反射するたびに反射音を1回鳴らす。
- 撃破時に撃破音を1回鳴らす。
- 同一反射音のピッチ変化範囲はAudio prototypeで固定する。
- BGM、SFX、Masterの音量を別々に制御できる構造にする。

## 8. Acceptance tests

### Projectile

- 法線 `(1, 0)` の垂直壁へ方向 `(-1, 0)` で入射した弾は `(1, 0)` へ反射する。
- 法線 `(0, 1)` の水平壁へ方向 `(0, -1)` で入射した弾は `(0, 1)` へ反射する。
- 45度入射の反射角は入射角と一致する。
- 弾丸が消滅すると発射可能数が1回復する。
- 有効弾3発の間は4発目を発射できない。

### Flow

- 最後の敵を撃破すると次Stageへ1回だけ遷移する。
- プレイヤー撃破時はStage 1へ1回だけ遷移する。
- Stage 10クリア後に存在しないStage 11を読み込まない。

## 9. Core Vertical Slice milestone (2026-09-21, superseded)

ユーザー指定の今回の中間成果はStage 1だけとする。上記Phase 0全体の
Stage 1〜10進行、音声、最終アートの完成を意味しない。
Stage 1で敵全滅をVictoryとして停止し、プレイヤー被弾をDefeatとして停止する。
DefeatのRestartボタンで全実行状態を破棄しStage 1を再生成する。
Victoryからも同じボタンで再試行できる。次Stageは今回読み込まない。
UIは残り発射枠、Victory、Defeat、Restartのみ。操作説明はREADMEへ記載する。

### 今回の確定ルールと暫定設定

- 同時自弾上限3。リロードタイマーはなく、消滅ごとに1枠回復。
- 壁で2回まで反射し、3回目の壁衝突で消滅する。2回目直後には消さない。
- 発射後、最初の反射までは射手を衝突対象から除外。反射後は自爆可能。
- 弾丸同士は衝突しない。各戦車の弾は陣営を問わず有効。
- 入力は画面外/非フォーカス/平面との交点なしでは直前の照準方向を保持。
  画面外クリックと非フォーカス中の射撃・移動は受け付けない。
- ゲーム停止時は移動・照準・射撃・弾道・BOT判断を停止。
- Restart操作のクリックは射撃へ流さず、ボタン解放後の新しい押下から射撃を受け付ける。
- 移動衝突はSphereCastで移動距離を制限。壁と他戦車を通過できない。
- 発射は砲塔の現在方向。銃口までの区間も衝突検査する。
- BOTはObserve→Move→Aim→Fire→Recover。Stageデータの巡回地点へ移動し、
  直射が壁で遮られる場合は撃たずに次巡回へ進む。跳弾予測は今回無効。

| 暫定値（Unity unit / second / degree） | 値 |
|---|---|
| 移動速度 / 車体旋回 / 砲塔旋回 | 4 / 540 / 720 |
| 戦車衝突半径 / ゲーム平面Y | 0.5 / 0.55 |
| 弾速 / 弾半径 / 寿命 | 10 / 0.10 / 8 |
| 衝突面分離距離 / 銃口距離 | 0.002 / 0.9 |
| BOT反応 / 射撃間隔 / 移動速度 | 0.7 / 2.0 / 1.5 |
| BOT移動時間 / 到着距離 / 照準誤差 / 照準許容角 / 跳弾予測 | 1.2 / 0.15 / 0 / 1 / false |
| Stage ID / サイズ / テーマ | 1 / 20×14 / wood-prototype |
| プレイヤーSpawn / 敵Spawn (X,Z) | (-6,-3) / (6,3) |
| BOT巡回 (X,Z) | (6,-3), (6,3) |
| 中央障害物 (X,Z; 幅,奥行) | (0,0; 2,4) |
| 外壁厚 / 壁高さ | 1 / 1.5 |
| カメラ俯角 / Orthographic Size / 位置 | 60 / 10.5 / (0,18,-10.3923) |
| Stage検証格子間隔 | 1 |

数値はGameplaySettings、BotSettings、StageDefinition、Presentation設定へ集約。
カメラと形状・単色マテリアルは仮表現で、ART_DIRECTIONの最終品質承認とは別。
Stage 2以降は作成しない。AI生成/外部アセット/画像/音声は使用しない。

### 中間成果の受入条件

1. 有効自弾3発の間4発目を拒否し、消滅すると発射枠が回復する。
2. 法線ベースで2回反射し、3回目の壁接触で弾が消える。
3. 1フレームに複数の壁へ当たっても残距離を処理する。
4. 有効弾の1発で戦車が破壊され、重複被弾は無視する。
5. 敵全滅でVictory、プレイヤー破壊でDefeatになり以後状態を変更しない。
6. RestartでStage 1と発射枠3を再生成し、古い戦車・弾を残さない。
7. Mainだけをロードして上記一連の操作ができる。
8. WASDは画面基準、斜め速度は一定。左クリック保持で連射しない。
9. Stage validatorが壁内・範囲外・孤立Spawn、重複ID、必須参照不足を拒否する。

この節の「Stage 1のみ」「最大2回反射」「Victoryで停止」は、次節の2 Stage
マイルストーンで置き換える。その他の操作・衝突・一撃撃破・弾数ルールは維持する。

## 10. Two-stage foundation milestone (2026-09-22)

Mainシーンは順序付きのStageDefinition一覧を参照する。Stage固有のSpawn、壁、
巡回地点、BOT設定、テーマは各StageDefinitionへ保存し、共通の戦車、弾、壁生成、
入力、勝敗ルールはStage間で共有する。Stage IDは1から連続し、一覧順と一致させる。

- Stage 1クリア時は同じMain内でStage 2を1回だけ生成する。
- Stage 2クリア時はVictoryで停止する。存在しないStage 3は読み込まない。
- プレイヤー撃破時はDefeatで停止し、RestartでStage 1へ戻る。
- Stage切替とRestartでは、旧Stageの戦車、弾、壁、一時状態を破棄する。
- UIはAMMO、最終Victory、Defeat、Restartのみを維持する。
- プレイヤー弾とBOT弾は同じProjectileActorとProjectileRulesを使う。
- 全砲弾は壁で最大1回だけ反射し、次の反射対象壁との衝突で消滅する。
- 同時自弾3発、一撃撃破、射手猶予、自爆、弾同士非衝突は維持する。

BOT v0の状態名とController境界は維持する。行動は次のように調整する。

- 射線がなければRecoverで待たず、次の巡回地点へ移動して射線を探す。
- 射線があればAimとFireへ進む。
- 射撃後は次の巡回地点へ移動し、射撃間隔中も位置を変える。
- 移動時間切れ時は次の巡回地点へ切り替え、その場でAimを繰り返さない。
- 高度なPathfinding、NavMesh、跳弾射撃予測は導入しない。

### 暫定設定

| 設定 | Stage 1 / Stage 2 |
|---|---|
| 最大反射回数 | 1（プレイヤー・BOT共通） |
| BOT反応 / 射撃間隔 / 移動速度 | 0.35秒 / 1.5秒 / 1.8 unit/s |
| BOT移動時間 / 最短再配置 / 到着距離 / 照準誤差 / 許容角 | 2.5秒 / 0.6秒 / 0.15 / 0度 / 1度 |
| Stage 1 Spawn (Player / Enemy) | (-6,-3) / (6,3) |
| Stage 1壁 | 中央 (0,0), 2×4 |
| Stage 1巡回点 | (6,-3), (6,3) |
| Stage 2 Spawn (Player / Enemy) | (6,-4) / (-6,4) |
| Stage 2壁 | 中央 (0,0), 6×2 |
| Stage 2巡回点 | (-6,-4), (-4,0), (-6,4) |

### 受入条件

1. プレイヤー弾とBOT弾は初回壁衝突で反射し、2回目の壁衝突で消滅する。
2. 両陣営の有効弾3発の間は4発目を拒否し、消滅すると枠が回復する。
3. Stage 1とStage 2の定義が同じvalidatorを通り、ID 1, 2の順で登録される。
4. Stage 1最後の敵を撃破するとStage 2へ1回だけ遷移する。
5. Stage 2最後の敵を撃破するとVictoryで停止する。
6. どちらのStageで敗北してもRestartでStage 1と弾数3を再生成する。
7. BOTは射線がない間も巡回し、射線を得るとAim・Fireし、射撃後も再配置する。
8. Stage 1の移動、照準、射撃、障害物、撃破、UIを反射回数以外は維持する。

## 11. Combat interactions and Stage gimmicks milestone (2026-09-22)

- Projectile同士は射手・陣営・反射済みかを問わず接触時に双方消滅する。双方の発射枠は既存のDespawn経路で1回だけ回復する。
- 同距離の複数候補はProjectile、Destructible Wall、Tank、Normal Wallの順で分類し、terminal後のProjectileは追加処理しない。
- 上部の数値AMMO表示を廃止し、Player Tank直下へ既存ShotSlotsを読む3-slot表示を置く。`●`は利用可能、`○`はfield上の自弾。Enemyには表示しない。
- StageDefinitionはEnemyごとにSpawn、巡回点、BotSettings、Behaviorを保持する。全Enemy撃破時だけStage clearになる。
- 既存Mobileに加えSentryを1種類追加する。Sentryは移動せず、直射LOSがある場合に既存反応時間・照準・射撃間隔・3発上限で射撃する。
- Destructible WallはTankとLOSを遮り、Projectile 1 hitで壁とProjectileが消滅する。反射回数は増えない。破壊後はTank、Projectile、LOSが通過できる。
- MineはStage配置型の中立hazard。Tankが範囲へ入ると既存`TankActor.Hit()`を1回だけ呼んで消滅する。Projectileでは起爆しない。
- Enemy、Projectile、Ammo Indicator、Destructible Wall、MineはStage Runtime rootとともにStage遷移・Restartで破棄する。

### Stage 2追加配置

| 要素 | 設定 (X,Z) |
|---|---|
| Player | (6,-4) |
| Mobile Enemy | Spawn (-6,4)、巡回 (-6,-4), (-4,0), (-6,4) |
| Sentry Enemy | Spawn (6,4) |
| Normal Wall | 中央 (0,0)、6×2 |
| Destructible Wall | 中央 (4,0)、1×2 |
| Mine | (2,-4)、1個 |

Stage 1の配置、Enemy数、Behavior、通常壁は変更しない。Stage 3以降、新武器、Projectile起爆Mine、HP 2以上の壁は対象外。
