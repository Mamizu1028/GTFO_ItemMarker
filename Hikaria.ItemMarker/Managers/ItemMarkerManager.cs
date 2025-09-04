using AIGraph;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hikaria.ItemMarker.Handlers;
using Hikaria.ItemMarker.Handlers.Markers;
using Il2CppInterop.Runtime;
using LevelGeneration;
using System.Collections;
using TheArchive.Loader;
using UnityEngine;

namespace Hikaria.ItemMarker.Managers
{
    public static class ItemMarkerManager
    {
        public static bool DevMode
        {
            get => _devMode;
            set
            {
                if (_devMode != value)
                {
                    _devMode = value;
                    if (_devMode)
                    {
                        foreach (var marker in _allItemMarkers)
                        {
                            marker.DoEnterDevMode();
                            if (!marker.enabled)
                                CoroutineManager.StartCoroutine(UpdateDevMode(marker).WrapToIl2Cpp());
                        }
                    }
                    else
                    {
                        foreach (var marker in _allItemMarkers)
                        {
                            marker.DoExitDevMode();
                        }
                    }
                }
            }
        }
        private static bool _devMode;

        private static IEnumerator UpdateDevMode(ItemMarkerBase marker)
        {
            var yielder = new WaitForSecondsRealtime(0.2f);
            while (DevMode)
            {
                try
                {
                    marker.DoDevModeUpdate();
                }
                catch
                {

                }
                yield return yielder;
            }
        }

        internal static void Init()
        {
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<ItemMarkerTag>();
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<ItemScanner>();
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<ItemMarkerBase>();
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<ItemInLevel_Marker>();
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<LG_ComputerTerminal_Marker>();
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<LG_PowerGenerator_Marker>();
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<LG_HSU_Marker>();
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<LG_HSUActivator_Marker>();
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<LG_BulkheadDoorController_Marker>();
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<LG_SecurityDoor_Locks_Marker>();
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<LG_DisinfectionStation_Marker>();
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
                        target.gameObject.AddComponent(_itemMarkers[type]).Cast<ItemMarkerBase>().SetupNavMarker(target);
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

        internal static void RegisterItemMarker(ItemMarkerBase marker)
        {
            _allItemMarkers.Add(marker);

            if (DevMode)
                marker.DoEnterDevMode();

            if (!marker.enabled)
                CoroutineManager.StartCoroutine(UpdateDevMode(marker).WrapToIl2Cpp());
        }

        internal static void UnregisterItemMarker(ItemMarkerBase marker)
        {
            _allItemMarkers.Remove(marker);
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

        internal static void UnregisterTerminalItemMarker(int instanceId, ItemMarkerBase marker)
        {
            if (!_terminalItemMarkers.TryGetValue(instanceId, out var markers))
            {
                markers = new();
                _terminalItemMarkers[instanceId] = markers;
            }
            markers.Remove(marker);
        }


        internal static void OnTerminalPing(int instanceId)
        {
            if (_terminalItemMarkers.TryGetValue(instanceId, out var markers))
            {
                foreach (var marker in markers)
                    marker.OnTerminalPing();
            }
        }

        internal static void OnTerminalItemKeyUpdate(int instanceId, string key)
        {
            if (_terminalItemMarkers.TryGetValue(instanceId, out var markers))
            {
                foreach (var marker in markers)
                {
                    marker.OnTerminalItemKeyUpdate(key);
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
            if (DevMode)
            {
                foreach (var marker in _allItemMarkers)
                {
                    marker.DoDevModeUpdate();
                }
                return;
            }
            foreach (var marker in ItemMarkerAutoUpdateModeLookup[ItemMarkerVisibleUpdateModeType.CourseNode])
            {
                marker.OnPlayerCourseNodeChanged(newNode);
            }
        }

        internal static void OnPlayerZoneChanged(LG_Zone newZone)
        {
            if (DevMode)
            {
                foreach (var marker in _allItemMarkers)
                {
                    marker.DoDevModeUpdate();
                }
                return;
            }
            foreach (var marker in ItemMarkerAutoUpdateModeLookup[ItemMarkerVisibleUpdateModeType.Zone])
            {
                marker.OnPlayerZoneChanged(newZone);
            }
        }

        internal static void OnPlayerDimensionChanged(Dimension newDim)
        {
            if (DevMode)
            {
                foreach (var marker in _allItemMarkers)
                {
                    marker.DoDevModeUpdate();
                }
                return;
            }

            foreach (var marker in ItemMarkerAutoUpdateModeLookup[ItemMarkerVisibleUpdateModeType.Dimension])
            {
                marker.OnPlayerDimensionChanged(newDim);
            }
        }

        private static readonly Dictionary<Il2CppSystem.Type, Il2CppSystem.Type> _itemMarkers = new();
        private static readonly HashSet<Il2CppSystem.Type> _typesToInspect = new();
        private static readonly Dictionary<int, HashSet<ItemMarkerBase>> _terminalItemMarkers = new();
        private static readonly HashSet<ItemMarkerBase> _allItemMarkers = new();

        private static readonly Dictionary<ItemMarkerVisibleUpdateModeType, HashSet<ItemMarkerBase>> ItemMarkerAutoUpdateModeLookup = new()
        {
            { ItemMarkerVisibleUpdateModeType.World, new() },
            { ItemMarkerVisibleUpdateModeType.CourseNode, new() },
            { ItemMarkerVisibleUpdateModeType.Zone, new() },
            { ItemMarkerVisibleUpdateModeType.Dimension, new() },
            { ItemMarkerVisibleUpdateModeType.Manual, new() },
            { ItemMarkerVisibleUpdateModeType.Custom, new() },
        };
    }
}
