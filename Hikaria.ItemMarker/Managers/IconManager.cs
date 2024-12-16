using System.Reflection;
using UnityEngine;

namespace Hikaria.ItemMarker.Managers
{
    public static class IconManager
    {
        public static bool TryGetCustomIcon(string fileName, out Sprite sprite)
        {
            return _customItemIconSprites.TryGetValue(fileName, out sprite);
        }

        internal static void Init()
        {
            string iconDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Assets/icons");
            if (!Directory.Exists(iconDir))
                Directory.CreateDirectory(iconDir);
            foreach (var file in Directory.GetFiles(iconDir, "*.png"))
            {
                var fileFullName = Path.GetFileName(file);
                var fileName = Path.GetFileNameWithoutExtension(file);
                byte[] array = File.ReadAllBytes(file);
                var texture2D = new Texture2D(2, 2);
                if (texture2D.LoadImage(array))
                {
                    texture2D.name = fileName;
                    texture2D.hideFlags = HideFlags.HideAndDontSave;
                    _customItemIconTextures[fileFullName] = texture2D;
                    var sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 64f);
                    sprite.name = Path.GetFileNameWithoutExtension(file);
                    sprite.hideFlags = HideFlags.HideAndDontSave;
                    _customItemIconSprites[fileFullName] = sprite;
                }
            }
        }

        private static Dictionary<string, Sprite> _customItemIconSprites = new();
        private static Dictionary<string, Texture2D> _customItemIconTextures = new();
    }
}
