using UnityEngine;
namespace TankGame.Presentation
{
    public sealed class MineExplosionVfx : MonoBehaviour
    {
        public bool IsAlive { get; private set; }
        public float Lifetime { get; private set; }
        public float TargetDiameter { get; private set; }
        float age;

        public void Initialize(float radius, float lifetime)
        {
            TargetDiameter = Mathf.Max(0, radius * 2);
            Lifetime = Mathf.Max(0.0001f, lifetime);
            age = 0;
            IsAlive = true;
            transform.localScale = Vector3.one * 0.05f;
        }

        void Update() { Tick(Time.deltaTime); }

        public void Tick(float deltaTime)
        {
            if (!IsAlive) return;
            age = Mathf.Min(Lifetime, age + Mathf.Max(0, deltaTime));
            float diameter = Mathf.Lerp(0.05f, TargetDiameter, age / Lifetime);
            transform.localScale = Vector3.one * diameter;
            if (age < Lifetime) return;
            IsAlive = false;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
