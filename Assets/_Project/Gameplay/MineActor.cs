using UnityEngine;
namespace TankGame.Gameplay
{
    public sealed class MineActor : MonoBehaviour
    {
        public bool IsArmed { get; private set; } = true;
        public float FuseRemaining { get; private set; }
        public float BlastRadius { get; private set; }
        GameSession session;

        public void Initialize(GameSession owner, float fuseSeconds, float blastRadius)
        {
            session = owner; FuseRemaining = fuseSeconds; BlastRadius = blastRadius;
        }

        public void Tick(float deltaTime)
        {
            if (!IsArmed) return;
            FuseRemaining = Mathf.Max(0, FuseRemaining - Mathf.Max(0, deltaTime));
            if (FuseRemaining <= 0) Explode();
        }

        public void Explode()
        {
            if (!IsArmed) return;
            Vector3 center = transform.position;
            IsArmed = false;
            gameObject.SetActive(false);
            session.ApplyMineExplosion(center, BlastRadius);
            session.SpawnMineExplosionVfx(center, BlastRadius);
            Destroy(gameObject);
        }

        public void Cleanup()
        {
            if (!IsArmed) return;
            IsArmed = false;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
