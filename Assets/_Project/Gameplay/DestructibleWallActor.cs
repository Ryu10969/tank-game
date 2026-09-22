using UnityEngine;
namespace TankGame.Gameplay
{
    public sealed class DestructibleWallActor : MonoBehaviour
    {
        public bool IsAlive { get; private set; } = true;
        public void Hit()
        {
            if (!IsAlive) return;
            IsAlive = false;
            var collider = GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
