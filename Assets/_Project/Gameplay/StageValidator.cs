using System.Collections.Generic;
using UnityEngine;
namespace TankGame.Gameplay
{
    public static class StageValidator
    {
        // The prototype uses axis-aligned walls, integer spawns and a 1-unit validation grid.
        public static List<string> Validate(IReadOnlyList<StageDefinition> stages, GameplaySettings settings)
        {
            if (settings == null) return new List<string> { "Missing GameplaySettings." };
            var errors = Validate(stages, settings.tankRadius);
            if (settings.defaultDurability != 1 || settings.heavyDurability != 2)
                errors.Add("Tank durability settings must be Standard 1 and Heavy 2.");
            if (settings.heavyMoveSpeed <= 0 || settings.heavyProjectileSpeed <= 0 ||
                settings.heavyProjectileSpeed >= settings.projectileSpeed)
                errors.Add("Heavy speed settings must be positive and Heavy projectile speed must be slower than Standard.");
            if (settings.burstShotCount != 3 || settings.burstShotCount > settings.shotCapacity ||
                settings.burstSpacingSeconds <= 0 || settings.burstReloadSeconds <= settings.burstSpacingSeconds * (settings.burstShotCount - 1))
                errors.Add("Invalid Burst settings.");
            if (settings.mineCapacity != 2 || !Mathf.Approximately(settings.mineFuseSeconds, 1) || settings.mineBlastRadius <= 0)
                errors.Add("Invalid Mine settings.");
            if (!Mathf.Approximately(settings.stageTitleSeconds, 0.5f) || !Mathf.Approximately(settings.stageGoSeconds, 0.5f) ||
                settings.stageClearSeconds <= 0)
                errors.Add("Invalid Stage presentation settings.");
            if (stages != null)
                foreach (var stage in stages)
                    if (stage != null && stage.enemies != null)
                        foreach (var enemy in stage.enemies)
                            if (enemy.archetype == EnemyArchetype.Heavy && enemy.botSettings != null &&
                                settings.heavyMoveSpeed >= enemy.botSettings.moveSpeed)
                                errors.Add("Heavy movement speed must be slower than Mobile.");
            return errors;
        }

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
                if (stage.enemies == null || stage.enemies.Length == 0 || stage.walls == null || stage.destructibleWalls == null)
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
                    if (!System.Enum.IsDefined(typeof(EnemyArchetype), enemy.archetype)) errors.Add("Undefined Enemy Archetype.");
                    if (enemy.behavior == EnemyBehavior.Mobile && (enemy.patrolPoints == null || enemy.patrolPoints.Length == 0)) errors.Add("Mobile Enemy requires patrol points.");
                    if ((enemy.archetype == EnemyArchetype.Heavy || enemy.archetype == EnemyArchetype.Burst) && enemy.behavior != EnemyBehavior.Mobile)
                        errors.Add("Heavy and Burst require Mobile behavior.");
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
                if (stage.stageId == 3)
                {
                    int heavy = 0, burst = 0;
                    foreach (var enemy in stage.enemies)
                    {
                        if (enemy.archetype == EnemyArchetype.Heavy) heavy++;
                        if (enemy.archetype == EnemyArchetype.Burst) burst++;
                    }
                    if (heavy != 1 || burst != 1) errors.Add("Stage 3 requires exactly one Heavy and one Burst.");
                    if (stage.walls.Length == 0 || stage.destructibleWalls.Length == 0)
                        errors.Add("Stage 3 requires Normal and Destructible Walls.");
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
