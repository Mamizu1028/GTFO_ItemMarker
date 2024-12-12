using AIGraph;
using GameData;
using Hikaria.ItemMarker.Handlers;
using Hikaria.ItemMarker.Managers;
using LevelGeneration;
using Player;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Loader;
using UnityEngine;
using static Hikaria.ItemMarker.Managers.ItemMarkerManager;

namespace Hikaria.ItemMarker.Features
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    [HideInModSettings]
    [DoNotSaveToConfig]
    public class ItemMarker : Feature
    {
        public override string Name => "物品标记";

        public override void Init()
        {
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<ItemNavMarkerWrapper>();
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<ItemScanner>();
            ItemMarkerManager.Init();
        }

        public override void OnGameDataInitialized()
        {
            foreach (var block in ItemDataBlock.GetAllBlocks())
            {
                if (!ItemNavMarkerWrapper.ValidSlots.Contains(block.inventorySlot))
                    continue;

                if (!ItemMarkerDescriptions.Value.TryGetValue(block.persistentID, out var desc))
                {
                    desc = new()
                    {
                        ItemID = block.persistentID,
                        DataBlockName = block.name,
                        PublicName = block.publicName,
                        Title = block.publicName.Replace('_', ' '),
                        VisibleUpdateMode = ItemNavMarkerWrapper.GetDefaultUpdateModeForSlot(block.inventorySlot),
                        AlwaysVisible = block.inventorySlot == InventorySlot.InLevelCarry
                    };
                    ItemMarkerDescriptions.Value[block.persistentID] = desc;
                }
                else
                {
                    desc.DataBlockName = block.name;
                    desc.ItemID = block.persistentID;
                    desc.PublicName = block.publicName;
                }
            }
        }

        public override void OnGameStateChanged(int state)
        {
            if (state != (int)eGameStateName.InLevel)
                return;
            foreach (var pair in ItemNavMarkerWrapper.ItemMarkerLookup)
            {
                pair.Value.UpdateItemUsesLeft();
            }
        }

        [ArchivePatch(typeof(PlayerAgent), nameof(PlayerAgent.CourseNode), null, ArchivePatch.PatchMethodType.Setter)]
        private class PlayerAgent__set_CourseNode__Patch
        {
            private static AIG_CourseNode preNode;
            private static bool nodeChanged;
            private static bool zoneChanged;
            private static void Prefix(PlayerAgent __instance, AIG_CourseNode value)
            {
                if (!__instance.IsLocallyOwned)
                    return;
                nodeChanged = preNode?.NodeID != value?.NodeID;
                zoneChanged = preNode?.m_zone.ID != value?.m_zone.ID;
            }

            private static void Postfix(AIG_CourseNode value)
            {
                if (nodeChanged)
                {
                    nodeChanged = false;
                    foreach (var marker in ItemNavMarkerWrapper.ItemMarkerModeLookup[VisibleUpdateModeType.CourseNode])
                    {
                        marker.ManualUpdate();
                    }
                }

                if (zoneChanged)
                {
                    zoneChanged = false;
                    foreach (var marker in ItemNavMarkerWrapper.ItemMarkerModeLookup[VisibleUpdateModeType.Zone])
                    {
                        marker.ManualUpdate();
                    }
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
                    foreach (var marker in __instance.GetComponentsInChildren<ItemNavMarkerWrapper>(true))
                    {
                        marker.IsDiscovered = true;
                        marker.ManualUpdate();
                    }
                }
            }
        }

        [ArchivePatch(typeof(ItemInLevel), nameof(ItemInLevel.Setup))]
        private class ItemInLevel__Setup__Patch
        {
            private static void Postfix(ItemInLevel __instance)
            {
                if (__instance.GetComponent<ItemNavMarkerWrapper>() == null)
                    __instance.gameObject.AddComponent<ItemNavMarkerWrapper>();
            }
        }

        [ArchivePatch(typeof(SyncedNavMarkerWrapper), nameof(SyncedNavMarkerWrapper.OnStateChange))]
        private class SyncedNavMarkerWrapper__OnStateChange__Patch
        {
            private static bool Prefix(SyncedNavMarkerWrapper __instance, pNavMarkerState newState)
            {
                if (__instance.m_playerIndex == -1)
                    return true;

                if (newState.status != eNavMarkerStatus.Visible)
                    return true;

                bool flag = false;
                if (newState.style == eNavMarkerStyle.PlayerPingResourceLocker)
                {
                    //var colliders = Physics.OverlapSphere(newState.worldPos, 0.01f, LayerManager.MASK_PLAYER_INTERACT_SPHERE);
                    //foreach (var collider in colliders)
                    //{
                    //    var container = collider.GetComponentInParent<LG_WeakResourceContainer>();
                    //    if (container == null)
                    //        continue;
                    //    var graphics = container.m_graphics.TryCast<LG_WeakResourceContainer_Graphics>();
                    //    if (graphics == null || graphics.m_status != eResourceContainerStatus.Open)
                    //        continue;
                    //    foreach (var marker in container.GetComponentsInChildren<ResourceNavMarkerWrapper>())
                    //    {
                    //        marker.OnPlayerPing();
                    //        flag = true;
                    //    }
                    //}
                }
                else if (newState.style != eNavMarkerStyle.PlayerPingLookat)
                {
                    var colliders = Physics.OverlapSphere(newState.worldPos, 0.01f, LayerManager.MASK_PLAYER_INTERACT_SPHERE);
                    foreach (var collider in colliders)
                    {
                        var marker = collider.GetComponentInParent<ItemNavMarkerWrapper>();
                        if (marker == null)
                            continue;
                        marker.OnPlayerPing();
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
                var marker = __instance.GetComponentInParent<ItemNavMarkerWrapper>();
                if (marker == null)
                    return;
                marker.OnTerminalPing();
            }
        }

        [ArchivePatch(typeof(ItemSpawnManager), nameof(ItemSpawnManager.SpawnItem))]
        private class ItemSpawnManager__SpawnItem__Patch
        {
            private static void Postfix(global::Item __result)
            {
                var itemInLevel = __result.TryCast<ItemInLevel>();
                if (itemInLevel == null)
                    return;
                if (itemInLevel.CourseNode != null)
                    return;

                if (itemInLevel.internalSync.GetCurrentState().status != ePickupItemStatus.PlacedInLevel)
                    return;

                if (itemInLevel.Get_pItemData().originCourseNode.TryGet(out var node))
                {
                    itemInLevel.CourseNode = node;
                    return;
                }
                var terminalItem = itemInLevel.GetComponent<iTerminalItem>();
                if (terminalItem != null)
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

        [ArchivePatch(typeof(LG_PickupItem_Sync), nameof(LG_PickupItem_Sync.OnStateChange))]
        private class LG_PickupItem_Sync__OnStateChange__Patch
        {
            private static void Postfix(LG_PickupItem_Sync __instance, pPickupItemState newState)
            {
                var item = __instance.item.TryCast<ItemInLevel>();
                if (item == null)
                    return;
                if (!ItemNavMarkerWrapper.ItemMarkerLookup.TryGetValue(item.GetInstanceID(), out var marker))
                    return;

                marker.UpdateItemUsesLeft();

                if (newState.status == ePickupItemStatus.PlacedInLevel)
                {
                    marker.IsPlacedInLevel = true;
                }
                else if (newState.status == ePickupItemStatus.PickedUp)
                {
                    marker.IsPlacedInLevel = false;
                }
            }
        }

        [ArchivePatch(typeof(CarryItemPickup_Core), nameof(CarryItemPickup_Core.PlacedInLevelCustomDataUpdate))]
        private class CarryItemPickup_Core__PlacedInLevelCustomDataUpdate__Patch
        {
            private static void Postfix(CarryItemPickup_Core __instance)
            {
                if (!ItemNavMarkerWrapper.ItemMarkerLookup.TryGetValue(__instance.GetInstanceID(), out var marker))
                    return;
                marker.ManualUpdate();
            }
        }
    }
}