# ADR-0002: Stage data and one-stage milestone

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
