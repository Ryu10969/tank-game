using TankGame.Gameplay;
using UnityEngine;
namespace TankGame.Presentation
{
    public static class PrimitiveFactory
    {
        public static GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material, bool collidable = false)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collidable)
            {
                go.GetComponent<Collider>().enabled = false;
                Object.Destroy(go.GetComponent<Collider>());
            }
            else go.layer = CollisionQueries.WallLayer;
            return go;
        }
        public static TankActor Tank(GameSession session, Vector2 spawn, bool player, PrototypePresentation art, float moveSpeed)
        {
            var go = new GameObject(player ? "Player" : "Bot"); go.layer = CollisionQueries.TankLayer;
            go.transform.SetParent(session.transform); go.transform.position = StageDefinition.World(spawn, session.Settings.planeHeight);
            var collider = go.AddComponent<SphereCollider>(); collider.radius = session.Settings.tankRadius;
            var body = new GameObject("Body").transform; body.SetParent(go.transform, false);
            Material material = player ? art.player : art.enemy;
            Shape("Chassis", PrimitiveType.Cube, body, Vector3.zero, art.bodyScale, material);
            Shape("Left runner", PrimitiveType.Cube, body, Vector3.left * art.treadOffset, art.treadScale, art.trim);
            Shape("Right runner", PrimitiveType.Cube, body, Vector3.right * art.treadOffset, art.treadScale, art.trim);
            var turret = new GameObject("Turret").transform; turret.SetParent(go.transform, false); turret.localPosition = art.turretPosition;
            Shape("Turret cap", player ? PrimitiveType.Cylinder : PrimitiveType.Cube, turret, Vector3.zero, art.turretScale, material);
            Shape("Barrel", PrimitiveType.Cube, turret, art.barrelPosition, art.barrelScale, art.trim);
            var actor = go.AddComponent<TankActor>(); actor.Initialize(session, player, body, turret, moveSpeed); session.Register(actor);
            return actor;
        }
        public static void Arena(Transform parent, StageDefinition stage, PrototypePresentation art)
        {
            Shape("Wood floor", PrimitiveType.Cube, parent, Vector3.down * (art.floorThickness / 2), new Vector3(stage.size.x, art.floorThickness, stage.size.y), art.floor);
            foreach (var wall in stage.walls) Wall(parent, wall.center, wall.size, art);
            float t = stage.boundaryThickness;
            Wall(parent, new Vector2(-stage.size.x / 2 - t / 2, 0), new Vector2(t, stage.size.y + 2 * t), art);
            Wall(parent, new Vector2(stage.size.x / 2 + t / 2, 0), new Vector2(t, stage.size.y + 2 * t), art);
            Wall(parent, new Vector2(0, -stage.size.y / 2 - t / 2), new Vector2(stage.size.x, t), art);
            Wall(parent, new Vector2(0, stage.size.y / 2 + t / 2), new Vector2(stage.size.x, t), art);
        }
        public static DestructibleWallActor DestructibleWall(GameSession session, StageWall wall, PrototypePresentation art)
        {
            var go = Shape("Destructible wall", PrimitiveType.Cube, session.transform,
                StageDefinition.World(wall.center, art.wallHeight / 2), new Vector3(wall.size.x, art.wallHeight, wall.size.y), art.destructibleWall, true);
            var actor = go.AddComponent<DestructibleWallActor>(); session.Register(actor); return actor;
        }
        public static MineActor Mine(GameSession session, Vector2 position, PrototypePresentation art)
        {
            const float radius = 0.55f;
            var go = Shape("Mine", PrimitiveType.Cylinder, session.transform,
                StageDefinition.World(position, 0.08f), new Vector3(0.8f, 0.08f, 0.8f), art.mine);
            var actor = go.AddComponent<MineActor>(); actor.Initialize(session.Settings.tankRadius + radius); session.Register(actor); return actor;
        }
        static void Wall(Transform parent, Vector2 center, Vector2 size, PrototypePresentation art)
        { Shape("Wood wall", PrimitiveType.Cube, parent, StageDefinition.World(center, art.wallHeight / 2), new Vector3(size.x, art.wallHeight, size.y), art.wall, true); }
    }
}
