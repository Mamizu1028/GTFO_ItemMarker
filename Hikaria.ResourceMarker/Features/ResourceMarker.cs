using AIGraph;
using GameData;
using Hikaria.ResourceMarker.Handlers;
using Hikaria.ResourceMarker.Managers;
using LevelGeneration;
using Player;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Loader;
using UnityEngine;
using static Hikaria.ResourceMarker.Managers.ResourceMarkerManager;

namespace Hikaria.ResourceMarker.Features
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    public class ResourceMarker : Feature
    {
        public override string Name => "资源标记";

        public override void Init()
        {
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<ResourceNavMarkerWrapper>();
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<ResourceScanner>();
            ResourceMarkerManager.Init();
        }

        public override void OnGameDataInitialized()
        {
            foreach (var block in ItemDataBlock.GetAllBlocks())
            {
                if (!ResourceNavMarkerWrapper.ValidSlots.Contains(block.inventorySlot))
                    continue;

                if (!ResourceMarkerDescriptions.Value.ContainsKey(block.persistentID))
                {
                    ResourceMarkerDescriptions.Value[block.persistentID] = new()
                    {
                        ItemID = block.persistentID,
                        ItemName = block.publicName.Replace('_', ' '),
                        Title = block.publicName
                    };
                }
            }
        }

        public override void OnGameStateChanged(int state)
        {
            if (state != (int)eGameStateName.InLevel)
                return;
            foreach (var pair in ResourceNavMarkerWrapper.ResourceMarkerLookup)
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
                    foreach (var marker in ResourceNavMarkerWrapper.ResourceMarkerModeLookup[VisibleCheckModeType.CourseNode])
                    {
                        marker.ManualUpdate();
                    }
                }

                if (zoneChanged)
                {
                    zoneChanged = false;
                    foreach (var marker in ResourceNavMarkerWrapper.ResourceMarkerModeLookup[VisibleCheckModeType.Zone])
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
                if (__instance.GetComponent<ResourceScanner>() == null)
                    __instance.gameObject.AddComponent<ResourceScanner>();
            }
        }

        [ArchivePatch(typeof(LG_ResourceContainer_Sync), nameof(LG_ResourceContainer_Sync.OnStateChange))]
        private class LG_ResourceContainer_Sync__OnStateChange__Patch
        {
            private static void Postfix(LG_ResourceContainer_Sync __instance, pResourceContainerItemState newState)
            {
                if (newState.status == eResourceContainerStatus.Open)
                {
                    foreach (var marker in __instance.GetComponentsInChildren<ResourceNavMarkerWrapper>(true))
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
                if (__instance.GetComponent<ResourceNavMarkerWrapper>() == null)
                    __instance.gameObject.AddComponent<ResourceNavMarkerWrapper>();
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
                        var marker = collider.GetComponentInParent<ResourceNavMarkerWrapper>();
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
                var marker = __instance.GetComponentInParent<ResourceNavMarkerWrapper>();
                if (marker == null)
                    return;
                marker.OnTerminalPing();
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
                if (!ResourceNavMarkerWrapper.ResourceMarkerLookup.TryGetValue(item.GetInstanceID(), out var marker))
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
    }
}
