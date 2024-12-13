using System.Reflection;
using TheArchive.Core.Models;
using TheArchive.Core.ModulesAPI;
using UnityEngine;

namespace Hikaria.ItemMarker.Managers
{
    public static class ItemMarkerManager
    {
        public static CustomSettings<Dictionary<uint, ResourceMarkerDescription>> ItemMarkerDescriptions = new("ItemMarkerDesciptions", new());

        public class ResourceMarkerDescription
        {
            public uint ItemID { get; set; } = 0U;
            public string DataBlockName { get; set; } = string.Empty;
            public string PublicName { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public SColor Color { get; set; } = UnityEngine.Color.white;
            public VisibleUpdateModeType VisibleUpdateMode { get; set; } = VisibleUpdateModeType.World;
            public float VisibleWorldDistance { get; set; } = 30f;
            public int VisibleCourseNodeDistance { get; set; } = 1;
            public float Alpha { get; set; } = 1f;
            public float AlphaADS { get; set; } = 0.4f;
            public float IconScale { get; set; } = 0.5f;
            public float PingFadeOutTime { get; set; } = 12f;
            public bool AlwaysVisible { get; set; } = false;
            public bool AlwaysShowTitle { get; set; } = false;
            public bool UseCustomIcon { get; set; } = false;
            public string CustomIconPath { get; set; } = string.Empty;
        }

        public enum VisibleUpdateModeType
        {
            World,
            CourseNode,
            Zone,
            Manual
        }

        public static bool TryGetCustomIcon(uint id, out Sprite sprite)
        {
            return _customIconSprites.TryGetValue(id, out sprite);
        }

        public static void Init()
        {
            string iconDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Assets/icons");
            if (!Directory.Exists(iconDir))
                Directory.CreateDirectory(iconDir);
            foreach (var pair in ItemMarkerDescriptions.Value)
            {
                var id = pair.Key;
                var desc = pair.Value;
                if (!desc.UseCustomIcon)
                    continue;

                var file = Path.Combine(iconDir, desc.CustomIconPath);
                if (!File.Exists(file))
                    continue;

                byte[] array = File.ReadAllBytes(file);
                var texture2D = new Texture2D(2, 2);
                if (texture2D.LoadImage(array))
                {
                    texture2D.name = Path.GetFileNameWithoutExtension(file);
                    texture2D.hideFlags = HideFlags.HideAndDontSave;
                    _customIconTextures[id] = texture2D;
                    var sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 64f);
                    sprite.name = Path.GetFileNameWithoutExtension(file);
                    sprite.hideFlags = HideFlags.HideAndDontSave;
                    _customIconSprites[id] = sprite;
                }
            }
        }

        private static Dictionary<uint, Sprite> _customIconSprites = new();
        private static Dictionary<uint, Texture2D> _customIconTextures = new();
    }
}
