using AIGraph;
using Gear;
using Hikaria.ItemMarker.Handlers;
using Hikaria.ItemMarker.Handlers.Markers;
using Hikaria.ItemMarker.Managers;
using LevelGeneration;
using Player;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.Localization;
using UnityEngine;

namespace Hikaria.ItemMarker.Features
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    [HideInModSettings]
    [DoNotSaveToConfig]
    public class ItemMarker : Feature
    {
        public override string Name => "物品标记";

        public static new ILocalizationService Localization { get; set; }

        [FeatureConfig]
        public static ItemMarkerSettings Settings { get; set; }

        public class ItemMarkerSettings
        {
            [FSDisplayName("开发者模式")]
            [FSHide]
            public bool DevMode
            {
                get => ItemMarkerManager.DevMode;
                set => ItemMarkerManager.DevMode = value;
            }
        }

        public override void Init()
        {
            IconManager.Init();
            ItemMarkerManager.Init();
        }

        public override void OnGameStateChanged(int state)
        {
            if (state != (int)eGameStateName.InLevel)
                return;

            ItemMarkerManager.SearchGameObject();
        }

        public override void OnGameDataInitialized()
        {
            ItemInLevel_Marker.OnGameDataInitialized();
        }

        [ArchivePatch(typeof(PlayerAgent), nameof(PlayerAgent.CourseNode), null, ArchivePatch.PatchMethodType.Setter)]
        private class PlayerAgent__set_CourseNode__Patch
        {
            private static bool nodeChanged;
            private static bool zoneChanged;
            private static bool dimensionChanged;
            private static AIG_CourseNode preNode;
            private static void Prefix(PlayerAgent __instance, AIG_CourseNode value)
            {
                if (!__instance.IsLocallyOwned)
                    return;
                preNode = __instance.CourseNode;
                nodeChanged = preNode?.NodeID != value?.NodeID;
                zoneChanged = preNode?.m_zone.ID != value?.m_zone.ID;
                dimensionChanged = preNode?.m_dimension.DimensionIndex != value?.m_dimension.DimensionIndex;
            }

            private static void Postfix(AIG_CourseNode value)
            {
                if (nodeChanged)
                {
                    nodeChanged = false;
                    ItemMarkerManager.OnPlayerCourseNodeChanged(value);
                }

                if (zoneChanged)
                {
                    zoneChanged = false;
                    ItemMarkerManager.OnPlayerZoneChanged(value?.m_zone);
                }

                if (dimensionChanged)
                {
                    dimensionChanged = false;
                    ItemMarkerManager.OnPlayerDimensionChanged(value?.m_dimension);
                }
            }
        }

        [ArchivePatch(typeof(PlayerAgent), nameof(PlayerAgent.Setup))]
        private class PlayerAgent__Setup__Patch
        {
            private static void Postfix(PlayerAgent __instance)
            {
                if (__instance.GetComponent<ItemScanner>() == null)
                    __instance.gameObject.AddComponent<ItemScanner>();
            }
        }

        [ArchivePatch(typeof(LG_ResourceContainer_Sync), nameof(LG_ResourceContainer_Sync.OnStateChange))]
        private class LG_ResourceContainer_Sync__OnStateChange__Patch
        {
            private static void Postfix(LG_ResourceContainer_Sync __instance, pResourceContainerItemState newState)
            {
                if (newState.status == eResourceContainerStatus.Open)
                {
                    foreach (var marker in __instance.GetComponentsInChildren<ItemMarkerTag>(true))
                    {
                        marker.ItemMarker.IsDiscovered = true;
                    }
                }
            }
        }

        [ArchivePatch(typeof(SyncedNavMarkerWrapper), nameof(SyncedNavMarkerWrapper.OnStateChange))]
        private class SyncedNavMarkerWrapper__OnStateChange__Patch
        {
            private static bool Prefix(SyncedNavMarkerWrapper __instance, pNavMarkerState oldState, pNavMarkerState newState)
            {
                if (__instance.m_playerIndex == -1)
                    return true;

                if (newState.status != eNavMarkerStatus.Visible)
                    return true;

                bool flag = false;
                if (newState.style != eNavMarkerStyle.PlayerPingResourceLocker && newState.style != eNavMarkerStyle.PlayerPingResourceBox)
                {
                    var colliders = Physics.OverlapSphere(newState.worldPos, 0.001f, LayerManager.MASK_PING_TARGET);
                    foreach (var collider in colliders)
                    {
                        var marker = collider.GetComponent<ItemMarkerTag>();
                        if (marker == null)
                            continue;
                        if (marker.ItemMarker.Style != newState.style)
                            continue;
                        marker.ItemMarker.OnPlayerPing();
                        flag = true;
                    }
                }

                return !flag;
            }
        }

        [ArchivePatch(typeof(LG_GenericTerminalItem), nameof(LG_GenericTerminalItem.PlayPing))]
        private class LG_GenericTerminalItem__PlayPing__Patch
        {
            private static void Postfix(LG_GenericTerminalItem __instance)
            {
                ItemMarkerManager.OnTerminalPing(__instance.GetInstanceID());
            }
        }

        [ArchivePatch(typeof(ItemSpawnManager), nameof(ItemSpawnManager.SpawnItem))]
        private class ItemSpawnManager__SpawnItem__Patch
        {
            private static void Postfix(global::Item __result)
            {
                var itemInLevel = __result?.TryCast<ItemInLevel>();
                if (itemInLevel == null)
                    return;

                if (itemInLevel.GetComponent<ItemInLevel_Marker>() == null)
                    itemInLevel.gameObject.AddComponent<ItemInLevel_Marker>().SetupNavMarker(itemInLevel);

                if (itemInLevel.CourseNode != null)
                    return;

                if (itemInLevel.internalSync == null || itemInLevel.internalSync.GetCurrentState().status != ePickupItemStatus.PlacedInLevel)
                    return;

                if (itemInLevel.Get_pItemData().originCourseNode.TryGet(out var node))
                {
                    itemInLevel.CourseNode = node;
                    return;
                }
                var terminalItem = itemInLevel.GetComponent<iTerminalItem>();
                if (terminalItem?.SpawnNode != null)
                {
                    itemInLevel.CourseNode = terminalItem.SpawnNode;
                    return;
                }
                var pickupItem = itemInLevel.transform.parent?.parent?.GetComponentInChildren<LG_PickupItem>();
                if (pickupItem != null)
                {
                    itemInLevel.CourseNode = pickupItem.SpawnNode;
                    return;
                }
            }
        }

        [ArchivePatch(typeof(LG_ResourceContainer_Storage), nameof(LG_ResourceContainer_Storage.SetSpawnNode))]
        private class LG_ResourceContainer_Storage__SetSpawnNode__Patch
        {
            private static void Postfix(LG_ResourceContainer_Storage __instance, GameObject obj, AIG_CourseNode spawnNode)
            {
                foreach (var item in obj.GetComponentsInChildren<ItemInLevel>())
                {
                    if (item.CourseNode == null && item.internalSync.GetCurrentState().status == ePickupItemStatus.PlacedInLevel)
                    {
                        item.CourseNode = spawnNode;
                    }
                }
            }
        }

        [ArchivePatch(typeof(CarryItemPickup_Core), nameof(CarryItemPickup_Core.OnCustomDataUpdated))]
        private class CarryItemPickup_Core__OnCustomDataUpdated__Patch
        {
            private static void Postfix(CarryItemPickup_Core __instance, pItemData_Custom customDataCopy)
            {
                if (!ItemInLevel_Marker.ItemInLevelMarkerLookup.TryGetValue(__instance.GetInstanceID(), out var marker))
                    return;
                marker.OnItemCustomDataUpdate(customDataCopy);
            }
        }

        [ArchivePatch(typeof(ResourcePackPickup), nameof(ResourcePackPickup.OnCustomDataUpdated))]
        private class ResourcePackPickup__OnCustomDataUpdated__Patch
        {
            private static void Postfix(ResourcePackPickup __instance, pItemData_Custom customDataCopy)
            {
                if (!ItemInLevel_Marker.ItemInLevelMarkerLookup.TryGetValue(__instance.GetInstanceID(), out var marker))
                    return;
                marker.OnItemCustomDataUpdate(customDataCopy);
            }
        }

        [ArchivePatch(typeof(ConsumablePickup_Core), nameof(ConsumablePickup_Core.OnCustomDataUpdated))]
        private class ConsumablePickup_Core__OnCustomDataUpdated__Patch
        {
            private static void Postfix(ConsumablePickup_Core __instance, pItemData_Custom customDataCopy)
            {
                if (!ItemInLevel_Marker.ItemInLevelMarkerLookup.TryGetValue(__instance.GetInstanceID(), out var marker))
                    return;
                marker.OnItemCustomDataUpdate(customDataCopy);
            }
        }

        [ArchivePatch(typeof(LG_ComputerTerminal), nameof(LG_ComputerTerminal.Setup))]
        private class LG_ComputerTerminal__Setup__Patch
        {
            private static void Postfix(LG_ComputerTerminal __instance)
            {
                if (__instance.GetComponent<LG_ComputerTerminal_Marker>() == null)
                    __instance.gameObject.AddComponent<LG_ComputerTerminal_Marker>().SetupNavMarker(__instance);
            }
        }

        [ArchivePatch(typeof(LG_GenericTerminalItem), nameof(LG_GenericTerminalItem.Setup))]
        private class LG_GenericTerminalItem__Setup__Patch
        {
            private static void Postfix(LG_GenericTerminalItem __instance, string key)
            {
                ItemMarkerManager.OnTerminalItemKeyUpdate(__instance.GetInstanceID(), key);
            }
        }

        [ArchivePatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.OnBuildDone))]
        private class LG_WardenObjective_Reactor__OnBuildDone__Patch
        {
            private static void Postfix(LG_WardenObjective_Reactor __instance)
            {
                if (__instance.SpawnNode != null && __instance.m_terminal != null)
                {
                    if (!__instance.SpawnNode.m_zone.TerminalsSpawnedInZone.Contains(__instance.m_terminal))
                        __instance.SpawnNode.m_zone.TerminalsSpawnedInZone.Add(__instance.m_terminal);
                }
            }
        }

        [ArchivePatch(typeof(LG_BulkheadDoorController_Core), nameof(LG_BulkheadDoorController_Core.Setup))]
        private class LG_BulkheadDoorController_Core__Setup__Patch
        {
            private static void Postfix(LG_BulkheadDoorController_Core __instance)
            {
                if (__instance.GetComponent<LG_BulkheadDoorController_Marker>() == null)
                    __instance.gameObject.AddComponent<LG_BulkheadDoorController_Marker>().SetupNavMarker(__instance);
            }
        }

        [ArchivePatch(typeof(LG_PowerGenerator_Core), nameof(LG_PowerGenerator_Core.Setup))]
        private class LG_PowerGenerator_Core__Setup__Patch
        {
            private static void Postfix(LG_PowerGenerator_Core __instance)
            {
                if (__instance.GetComponent<LG_PowerGenerator_Marker>() == null)
                    __instance.gameObject.AddComponent<LG_PowerGenerator_Marker>().SetupNavMarker(__instance);
            }
        }

        [ArchivePatch(typeof(LG_HSU), nameof(LG_HSU.Setup))]
        private class LG_HSU__Setup__Patch
        {
            private static void Postfix(LG_HSU __instance)
            {
                if (__instance.GetComponent<LG_HSU_Marker>() == null)
                    __instance.gameObject.AddComponent<LG_HSU_Marker>().SetupNavMarker(__instance);
            }
        }

        [ArchivePatch(typeof(LG_HSUActivator_Core), nameof(LG_HSUActivator_Core.SetupAsWardenObjective))]
        private class LG_HSUActivator_Core__SetupAsWardenObjective__Patch
        {
            private static void Postfix(LG_HSUActivator_Core __instance)
            {
                if (__instance.GetComponent<LG_HSUActivator_Marker>() == null)
                    __instance.gameObject.AddComponent<LG_HSUActivator_Marker>().SetupNavMarker(__instance);
            }
        }

        [ArchivePatch(typeof(LG_HSUActivator_Core), nameof(LG_HSUActivator_Core.SetupFromCustomGeomorph))]
        private class LG_HSUActivator_Core__SetupFromCustomGeomorph__Patch
        {
            private static void Postfix(LG_HSUActivator_Core __instance)
            {
                if (__instance.GetComponent<LG_HSUActivator_Marker>() == null)
                    __instance.gameObject.AddComponent<LG_HSUActivator_Marker>().SetupNavMarker(__instance);
            }
        }

        [ArchivePatch(typeof(LG_SecurityDoor_Locks), nameof(LG_SecurityDoor_Locks.SetupForBulkheadDC))]
        private class LG_SecurityDoor_Locks__SetupForBulkheadDC__Patch
        {
            private static void Postfix(LG_SecurityDoor_Locks __instance)
            {
                if (__instance.GetComponent<LG_SecurityDoor_Locks_Marker>() == null)
                    __instance.gameObject.AddComponent<LG_SecurityDoor_Locks_Marker>().SetupNavMarker(__instance);
            }
        }

        [ArchivePatch(typeof(LG_SecurityDoor_Locks), nameof(LG_SecurityDoor_Locks.SetupForGateKey))]
        private class LG_SecurityDoor_Locks__SetupForGateKey__Patch
        {
            private static void Postfix(LG_SecurityDoor_Locks __instance)
            {
                if (__instance.GetComponent<LG_SecurityDoor_Locks_Marker>() == null)
                    __instance.gameObject.AddComponent<LG_SecurityDoor_Locks_Marker>().SetupNavMarker(__instance);
            }
        }

        [ArchivePatch(typeof(LG_SecurityDoor_Locks), nameof(LG_SecurityDoor_Locks.SetupForPowerGenerator))]
        private class LG_SecurityDoor_Locks__SetupForPowerGenerator__Patch
        {
            private static void Postfix(LG_SecurityDoor_Locks __instance)
            {
                if (__instance.GetComponent<LG_SecurityDoor_Locks_Marker>() == null)
                    __instance.gameObject.AddComponent<LG_SecurityDoor_Locks_Marker>().SetupNavMarker(__instance);
            }
        }

        [ArchivePatch(typeof(LG_DisinfectionStation), nameof(LG_DisinfectionStation.Setup))]
        private class LG_DisinfectionStation__Setup__Patch
        {
            private static void Postfix(LG_DisinfectionStation __instance)
            {
                if (__instance.GetComponent<LG_DisinfectionStation_Marker>() == null)
                    __instance.gameObject.AddComponent<LG_DisinfectionStation_Marker>().SetupNavMarker(__instance);
            }
        }
    }
}