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
                if (stage.botSettings == null || string.IsNullOrWhiteSpace(stage.themeId)) errors.Add("Missing required Stage reference.");
                if (stage.enemySpawns == null || stage.enemySpawns.Length == 0 || stage.patrolPoints == null || stage.patrolPoints.Length == 0 || stage.walls == null)
                { errors.Add("Missing Stage arrays."); continue; }
                foreach (var wall in stage.walls)
                    if (wall.size.x <= 0 || wall.size.y <= 0) errors.Add("Invalid wall size.");
                var spawns = new List<Vector2> { stage.playerSpawn };
                spawns.AddRange(stage.enemySpawns);
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
                foreach (var patrol in stage.patrolPoints)
                    if (!Walkable(stage, patrol, tankRadius) || !visited.Contains(Vector2Int.RoundToInt(patrol))) errors.Add("Patrol is unreachable.");
            }
            return errors;
        }
        static bool Walkable(StageDefinition stage, Vector2 point, float radius)
        {
            if (Mathf.Abs(point.x) + radius >= stage.size.x / 2 || Mathf.Abs(point.y) + radius >= stage.size.y / 2) return false;
            foreach (var wall in stage.walls)
                if (Mathf.Abs(point.x - wall.center.x) <= wall.size.x / 2 + radius && Mathf.Abs(point.y - wall.center.y) <= wall.size.y / 2 + radius) return false;
            return true;
        }
    }
}
