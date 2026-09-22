using System.Collections.Generic;
using UnityEngine;
namespace TankGame.Gameplay
{
    public static class StageValidator
    {
        // The prototype uses axis-aligned walls, integer spawns and a 1-unit validation grid.
        public static List<string> Validate(IReadOnlyList<StageDefinition> stages, float tankRadius)
        {
            var errors = new List<string>();
            if (stages == null || stages.Count == 0)
            {
                errors.Add("At least one Stage is required.");
                return errors;
            }
            var ids = new HashSet<int>();
            for (int stageIndex = 0; stageIndex < stages.Count; stageIndex++)
            {
                var stage = stages[stageIndex];
                if (stage == null) { errors.Add("Missing Stage reference."); continue; }
                if (!ids.Add(stage.stageId)) errors.Add("Duplicate Stage ID.");
                if (stage.stageId != stageIndex + 1) errors.Add("Stage IDs must be contiguous and match list order.");
                if (stage.stageId < 1 || stage.size.x < 2 || stage.size.y < 2 || stage.boundaryThickness <= 0)
                    errors.Add("Invalid Stage dimensions or ID.");
                if (string.IsNullOrWhiteSpace(stage.themeId)) errors.Add("Missing required Stage reference.");
                if (stage.enemies == null || stage.enemies.Length == 0 || stage.walls == null || stage.destructibleWalls == null || stage.mines == null)
                { errors.Add("Missing Stage arrays."); continue; }
                foreach (var wall in stage.walls)
                    if (wall.size.x <= 0 || wall.size.y <= 0) errors.Add("Invalid wall size.");
                foreach (var wall in stage.destructibleWalls)
                    if (wall.size.x <= 0 || wall.size.y <= 0) errors.Add("Invalid destructible wall size.");
                var spawns = new List<Vector2> { stage.playerSpawn };
                foreach (var enemy in stage.enemies)
                {
                    spawns.Add(enemy.spawn);
                    if (enemy.botSettings == null) errors.Add("Missing Enemy BotSettings.");
                    if (!System.Enum.IsDefined(typeof(EnemyBehavior), enemy.behavior)) errors.Add("Undefined Enemy Behavior.");
                    if (enemy.behavior == EnemyBehavior.Mobile && (enemy.patrolPoints == null || enemy.patrolPoints.Length == 0)) errors.Add("Mobile Enemy requires patrol points.");
                }
                foreach (var spawn in spawns)
                    if (!Walkable(stage, spawn, tankRadius)) errors.Add("Spawn outside Stage or overlapping wall.");
                for (int i = 0; i < spawns.Count; i++)
                    for (int j = i + 1; j < spawns.Count; j++)
                        if (Vector2.Distance(spawns[i], spawns[j]) < tankRadius * 2) errors.Add("Spawns overlap.");
                var start = Vector2Int.RoundToInt(stage.playerSpawn);
                var visited = new HashSet<Vector2Int>();
                var queue = new Queue<Vector2Int>();
                if (Walkable(stage, start, tankRadius)) { visited.Add(start); queue.Enqueue(start); }
                var steps = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    foreach (var step in steps)
                    {
                        var next = current + step;
                        if (!visited.Contains(next) && Walkable(stage, next, tankRadius) && Walkable(stage, (Vector2)current + (Vector2)step * 0.5f, tankRadius))
                        { visited.Add(next); queue.Enqueue(next); }
                    }
                }
                foreach (var spawn in spawns)
                    if (visited.Count < 2 || !visited.Contains(Vector2Int.RoundToInt(spawn))) errors.Add("Spawn is isolated or unreachable.");
                foreach (var enemy in stage.enemies)
                    if (enemy.patrolPoints != null)
                        foreach (var patrol in enemy.patrolPoints)
                            if (!Walkable(stage, patrol, tankRadius) || !visited.Contains(Vector2Int.RoundToInt(patrol))) errors.Add("Patrol is unreachable.");
                foreach (var mine in stage.mines)
                {
                    if (!Inside(stage, mine, 0.55f) || InsideWall(stage.walls, mine, 0.55f) || InsideWall(stage.destructibleWalls, mine, 0.55f))
                        errors.Add("Mine outside Stage or overlapping wall.");
                    foreach (var spawn in spawns)
                        if (Vector2.Distance(mine, spawn) < tankRadius + 0.55f) errors.Add("Mine overlaps Spawn.");
                }
            }
            return errors;
        }
        static bool Walkable(StageDefinition stage, Vector2 point, float radius)
        {
            return Inside(stage, point, radius) && !InsideWall(stage.walls, point, radius) && !InsideWall(stage.destructibleWalls, point, radius);
        }
        static bool Inside(StageDefinition stage, Vector2 point, float radius)
        { return Mathf.Abs(point.x) + radius < stage.size.x / 2 && Mathf.Abs(point.y) + radius < stage.size.y / 2; }
        static bool InsideWall(IReadOnlyList<StageWall> walls, Vector2 point, float radius)
        {
            foreach (var wall in walls)
                if (Mathf.Abs(point.x - wall.center.x) <= wall.size.x / 2 + radius && Mathf.Abs(point.y - wall.center.y) <= wall.size.y / 2 + radius) return true;
            return false;
        }
    }
}
