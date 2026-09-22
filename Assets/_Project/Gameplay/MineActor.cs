using UnityEngine;
namespace TankGame.Gameplay
{
    public sealed class MineActor : MonoBehaviour
    {
        public bool IsArmed { get; private set; } = true;
        public float TriggerRadius { get; private set; }
        public void Initialize(float triggerRadius) { TriggerRadius = triggerRadius; }
        public bool TryTrigger(TankActor tank)
        { return TryTrigger(tank, tank == null ? Vector3.zero : tank.transform.position, tank == null ? Vector3.zero : tank.transform.position); }
        public bool TryTrigger(TankActor tank, Vector3 start, Vector3 end)
        {
            if (!IsArmed || tank == null || !tank.Life.IsAlive) return false;
            Vector2 segmentStart = new Vector2(start.x, start.z);
            Vector2 segment = new Vector2(end.x - start.x, end.z - start.z);
            Vector2 toMine = new Vector2(transform.position.x - start.x, transform.position.z - start.z);
            float t = segment.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(toMine, segment) / segment.sqrMagnitude) : 0;
            Vector2 closest = segmentStart + segment * t;
            Vector2 mine = new Vector2(transform.position.x, transform.position.z);
            if ((closest - mine).sqrMagnitude > TriggerRadius * TriggerRadius) return false;
            IsArmed = false;
            gameObject.SetActive(false);
            tank.Hit();
            Destroy(gameObject);
            return true;
        }
    }
}
