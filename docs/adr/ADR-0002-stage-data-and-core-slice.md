# ADR-0002: Stage data and incremental milestones

- Status: Accepted for core slice
- Date: 2026-09-21

StageはScriptableObjectに統一する。StageDefinitionはID、論理サイズ、Spawn、
障害物、BOT設定参照、テーマ、巡回地点を保存する。実行時にプリミティブを生成するが、
Main上のStageDefinition参照を唯一のStageデータ源とする。
EditorとCIは同じvalidatorで格子経路の到達性、Spawn、必須参照と重複IDを確認する。
今回の整数座標・軸平行壁に対して、半径分膨張した障害物を1unit格子で検査する。
任意角度の壁や細い通路の一般的なNavMesh検証は対象外。

Phase 0全体仕様は維持し、今回のユーザー指定によりその手前のStage 1中間成果を作る。
Victory/Defeatで停止しRestartでStage 1を作り直す。次Stageへの進行は後工程。
最大反射回数2は「反射を2回許可、次の壁接触で消滅」と定義する。
射手は初回反射まで除外、弾同士の衝突なし。これらは今回固定する試作ルール。

CoreはUnity参照なし。Gameplayが物理検出とCore状態を結び、Inputが人間/BOT命令を供給、
Presentationが構成・モデル・UIを担当する。Editor assemblyがStage検証・シーン保存・Web buildを担当。

## Amendment: two-stage foundation (2026-09-22)

上記のStage 1限定、最大反射2回、Victory停止は2026-09-21時点の中間成果を記録したもの。
今回の要求で現在のマイルストーンをStage 1〜2へ拡張し、最大反射を両陣営共通で1回へ変更する。

MainのSliceBootstrapはStageDefinitionの順序付き配列を参照する。Stage IDは1始まりで
配列順と一致し、validatorが欠番、逆順、重複を拒否する。Stageクリア時は同じMain内で
次の定義からRuntime rootを再生成する。最終StageのみVictoryで停止し、Defeat/Restartは
Stage 1へ戻る。この方式はScene名やStage番号の分岐を増やさず、StageDefinitionの追加で
Stage 3以降を拡張できる。

BOTは既存のObserve/Move/Aim/Fire/Recoverと巡回点を維持する。射線なしと射撃後は
Moveへ戻し、射撃間隔中も巡回して射線を作る。NavMeshや別AI frameworkは導入しない。

## Amendment: combat interactions and Stage gimmicks (2026-09-22)

StageDefinitionのEnemy Spawn一覧をStageEnemy一覧へ置き換え、Enemyごとの巡回点、BotSettings、
Mobile/Sentry Behaviorをデータ化する。GameSessionは一覧数をMatchRulesへ渡すため、全Enemy撃破の
既存Coreルールを維持する。Destructible WallとMineもStageDefinitionの配置データとし、Runtime rootへ生成する。

Projectile clashはUnity collision callbackへ依存せず既存SphereCastの衝突分類へ統合する。
Projectileの`IsAlive`をterminal guard、既存Despawnを発射枠返却の唯一経路として維持する。
