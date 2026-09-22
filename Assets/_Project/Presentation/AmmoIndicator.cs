using System.Text;
using TankGame.Gameplay;
using UnityEngine;
namespace TankGame.Presentation
{
    public sealed class AmmoIndicator : MonoBehaviour
    {
        TankActor tank;
        Camera gameCamera;
        static Texture2D filledTexture;
        static Texture2D outlineTexture;
        public int SlotCount => tank == null ? 0 : tank.Slots.Capacity;
        public int AvailableSlots => tank == null ? 0 : tank.Slots.Available;
        public string SlotGlyphs
        {
            get
            {
                var text = new StringBuilder();
                for (int i = 0; i < SlotCount; i++) text.Append(i < tank.Slots.Active ? '○' : '●');
                return text.ToString();
            }
        }
        public void Initialize(TankActor owner, Camera camera) { tank = owner; gameCamera = camera; }
        void OnGUI()
        {
            if (tank == null || gameCamera == null || !tank.Life.IsAlive) return;
            Vector3 screen = gameCamera.WorldToScreenPoint(tank.transform.position + Vector3.back * 1.2f);
            if (screen.z <= 0) return;
            float scale = Mathf.Max(1, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            EnsureTextures();
            float x = screen.x / scale - 29;
            float y = (Screen.height - screen.y) / scale;
            for (int i = 0; i < SlotCount; i++)
                GUI.DrawTexture(new Rect(x + i * 22, y, 14, 14), i < tank.Slots.Active ? outlineTexture : filledTexture);
        }
        static void EnsureTextures()
        {
            if (filledTexture != null) return;
            filledTexture = CircleTexture(false); outlineTexture = CircleTexture(true);
        }
        static Texture2D CircleTexture(bool outline)
        {
            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - 7.5f, dy = y - 7.5f, distance = Mathf.Sqrt(dx * dx + dy * dy);
                    bool visible = outline ? distance >= 5 && distance <= 7 : distance <= 7;
                    pixels[y * size + x] = visible ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                }
            texture.SetPixels32(pixels); texture.Apply(); return texture;
        }
    }
}
