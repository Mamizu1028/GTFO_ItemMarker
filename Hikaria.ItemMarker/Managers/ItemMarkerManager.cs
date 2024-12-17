using AIGraph;
using Hikaria.ItemMarker.Handlers;
using Hikaria.ItemMarker.Handlers.Markers;
using Il2CppInterop.Runtime;
using LevelGeneration;
using TheArchive.Loader;
using UnityEngine;

namespace Hikaria.ItemMarker.Managers
{
    public static class ItemMarkerManager
    {
        public static bool DevMode { get; set; } = false;

        internal static void Init()
        {
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<ItemMarkerTag>();
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<ItemScanner>();
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<ItemMarkerBase>();
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<ItemInLevelMarker>();
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<LG_ComputerTerminalMarker>();
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<LG_PowerGeneratorMarker>();
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<LG_HSUMarker>();
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<LG_HSUActivatorMarker>();
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<LG_BulkheadDoorControllerMarker>();
        }

        internal static void InspectGameObject(GameObject go)
        {
            foreach (var type in _typesToInspect)
            {
                var target = go.GetComponentInChildren(type);
                if (target == null)
                    continue;
                if (target.GetComponent<ItemMarkerBase>() == null)
                    target.gameObject.AddComponent(_itemMarkers[type]).Cast<ItemMarkerBase>().SetupNavMarker(target);
            }
        }

        internal static void SearchGameObject()
        {
            foreach (var type in _typesToInspect)
            {
                foreach (var obj in UnityEngine.Object.FindObjectsOfType(type))
                {
                    var target = obj.Cast<Component>();
                    if (target.GetComponent<ItemMarkerBase>() == null)
                    {
                        var itemMarker = target.gameObject.AddComponent(_itemMarkers[type]).Cast<ItemMarkerBase>();
                        itemMarker.SetupNavMarker(target);
                    }
                }
            }

        }

        public static void RegisterItemMarker<C, T>() where C : Component where T : ItemMarkerBase
        {
            var il2CppType = Il2CppType.Of<C>(true);
            _typesToInspect.Add(il2CppType);
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<T>();
            _itemMarkers.Add(il2CppType, Il2CppType.Of<T>(true));
        }

        internal static void RegisterTerminalItemMarker(int instanceId, ItemMarkerBase marker)
        {
            if (!_terminalItemMarkers.TryGetValue(instanceId, out var markers))
            {
                markers = new();
                _terminalItemMarkers[instanceId] = markers;
            }
            markers.Add(marker);
        }

        internal static void OnTerminalPing(int instanceId)
        {
            if (_terminalItemMarkers.TryGetValue(instanceId, out var markers))
            {
                foreach (var marker in markers)
                    marker.OnTerminalPing();
            }
        }

        internal static void SetTerminalItemKey(int instanceId, string key)
        {
            if (_terminalItemMarkers.TryGetValue(instanceId, out var setters))
            {
                foreach (var setter in setters)
                {
                    setter.SetTerminalItemKey(key);
                }
            }
        }

        internal static void RegisterItemMarkerAutoUpdate(ItemMarkerBase itemMarker)
        {
            if (ItemMarkerAutoUpdateModeLookup.TryGetValue(itemMarker.VisibleUpdateMode, out var markers))
            {
                markers.Add(itemMarker);
            }
        }

        internal static void UnregisterItemMarkerAutoUpdate(ItemMarkerBase itemMarker)
        {
            if (ItemMarkerAutoUpdateModeLookup.TryGetValue(itemMarker.VisibleUpdateMode, out var markers))
            {
                markers.Remove(itemMarker);
            }
        }

        internal static void OnPlayerCourseNodeChanged(AIG_CourseNode newNode)
        {
            foreach (var marker in ItemMarkerAutoUpdateModeLookup[ItemMarkerVisibleUpdateModeType.CourseNode])
            {
                marker.OnPlayerCourseNodeChanged(newNode);
            }
        }

        internal static void OnPlayerZoneChanged(LG_Zone newZone)
        {
            foreach (var marker in ItemMarkerAutoUpdateModeLookup[ItemMarkerVisibleUpdateModeType.Zone])
            {
                marker.OnPlayerZoneChanged(newZone);
            }
        }

        internal static void OnPlayerDimensionChanged(Dimension newDim)
        {
            foreach (var marker in ItemMarkerAutoUpdateModeLookup[ItemMarkerVisibleUpdateModeType.Dimension])
            {
                marker.OnPlayerDimensionChanged(newDim);
            }
        }

        private static readonly Dictionary<Il2CppSystem.Type, Il2CppSystem.Type> _itemMarkers = new();
        private static readonly HashSet<Il2CppSystem.Type> _typesToInspect = new();
        private static readonly Dictionary<int, HashSet<ItemMarkerBase>> _terminalItemMarkers = new();
        private static readonly Dictionary<ItemMarkerVisibleUpdateModeType, HashSet<ItemMarkerBase>> ItemMarkerAutoUpdateModeLookup = new()
        {
            { ItemMarkerVisibleUpdateModeType.World, new() },
            { ItemMarkerVisibleUpdateModeType.CourseNode, new() },
            { ItemMarkerVisibleUpdateModeType.Zone, new() },
            { ItemMarkerVisibleUpdateModeType.Dimension, new() },
            { ItemMarkerVisibleUpdateModeType.Manual, new() },
        };
    }
}
