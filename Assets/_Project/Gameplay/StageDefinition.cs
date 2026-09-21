using System;
using UnityEngine;
namespace TankGame.Gameplay
{
    [Serializable]
    public struct StageWall
    {
        public Vector2 center;
        public Vector2 size;
        public StageWall(Vector2 center, Vector2 size) { this.center = center; this.size = size; }
    }

    [CreateAssetMenu(menuName = "Tank Game/Stage Definition")]
    public sealed class StageDefinition : ScriptableObject
    {
        public int stageId = 1;
        public Vector2 size = new Vector2(20, 14);
        public Vector2 playerSpawn = new Vector2(-6, -3);
        public Vector2[] enemySpawns = { new Vector2(6, 3) };
        public StageWall[] walls = { new StageWall(Vector2.zero, new Vector2(2, 4)) };
        public BotSettings botSettings;
        public string themeId = "wood-prototype";
        public Vector2[] patrolPoints = { new Vector2(6, -3), new Vector2(6, 3) };
        public float boundaryThickness = 1;
        public static Vector3 World(Vector2 point, float height) => new Vector3(point.x, height, point.y);
    }
}
