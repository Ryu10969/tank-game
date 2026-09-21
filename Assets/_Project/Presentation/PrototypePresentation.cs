using UnityEngine;
namespace TankGame.Presentation
{
    [CreateAssetMenu(menuName = "Tank Game/Prototype Presentation")]
    public sealed class PrototypePresentation : ScriptableObject
    {
        public Material floor, wall, player, enemy, trim, projectile;
        public float wallHeight = 1.5f;
        public float floorThickness = 0.3f;
        public float cameraSize = 10.5f;
        public Vector3 cameraPosition = new Vector3(0, 18, -10.3923f);
        public Vector3 cameraAngles = new Vector3(60, 0, 0);
        public Vector3 bodyScale = new Vector3(0.85f, 0.35f, 1.1f);
        public Vector3 treadScale = new Vector3(0.18f, 0.27f, 1.15f);
        public float treadOffset = 0.46f;
        public Vector3 turretPosition = new Vector3(0, 0.25f, 0);
        public Vector3 turretScale = new Vector3(0.55f, 0.22f, 0.55f);
        public Vector3 barrelPosition = new Vector3(0, 0, 0.5f);
        public Vector3 barrelScale = new Vector3(0.16f, 0.16f, 0.8f);
    }
}
