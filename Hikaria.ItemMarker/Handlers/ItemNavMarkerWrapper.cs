using AIGraph;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hikaria.Core;
using Hikaria.Core.Interfaces;
using LevelGeneration;
using Player;
using SNetwork;
using System.Collections;
using UnityEngine;
using static Hikaria.ItemMarker.Managers.ItemMarkerManager;

namespace Hikaria.ItemMarker.Handlers
{
    public class ItemNavMarkerWrapper : MonoBehaviour, IOnResetSession, IOnRecallComplete, IOnBufferCommand
    {
        public void Setup(ItemInLevel item)
        {
            m_item = item;
            var itemDataBlock = m_item.ItemDataBlock;
            if (!ValidSlots.Contains(itemDataBlock.inventorySlot))
            {
                Destroy(this);
                return;
            }
            m_navMarkerPlacer = m_item.GetComponent<PlaceNavMarkerOnGO>();
            m_itemSlot = itemDataBlock.inventorySlot;
            m_itemShowUses = !itemDataBlock.GUIShowAmmoInfinite && itemDataBlock.GUIShowAmmoTotalRel;
            var ammoType = PlayerAmmoStorage.GetAmmoTypeFromSlot(itemDataBlock.inventorySlot);
            if (ammoType == AmmoType.CurrentConsumable)
            {
                m_itemCostOfBullet = 1f;
                if (itemDataBlock.ConsumableAmmoMax == m_itemCostOfBullet)
                    m_itemShowUses = false;
            }
            else if (ammoType == AmmoType.ResourcePackRel)
                m_itemCostOfBullet = 20f;
            else
                m_itemShowUses = false;
            m_allowDiscoverCheck = m_item.container == null && (m_itemSlot != InventorySlot.InPocket && m_itemSlot != InventorySlot.Pickup);

            m_marker = GuiManager.NavMarkerLayer.PrepareGenericMarker(gameObject);
            m_markerStyle = GetComponentInChildren<iPlayerPingTarget>()?.PingTargetStyle ?? eNavMarkerStyle.PlayerPingLoot;
            m_markerTitle = itemDataBlock.publicName.Replace('_', ' ');
            m_marker.SetTitle(m_markerTitle);
            m_marker.SetStyle(m_markerStyle);
            m_marker.SetIconScale(0.6f);
            m_marker.SetAlpha(1f);
            var size = m_marker.m_title.rectTransform.sizeDelta;
            size.x *= 2;
            m_marker.m_title.rectTransform.sizeDelta = size;
            if (ItemMarkerDescriptions.Value.TryGetValue(itemDataBlock.persistentID, out var desc))
            {
                m_marker.SetColor(desc.Color);
                m_markerTitle = desc.Title;
                m_marker.SetTitle(m_markerTitle);
                m_marker.SetIconScale(desc.IconScale);
                m_markerVisibleUpdateMode = desc.VisibleUpdateMode;
                m_markerVisibleWorldDistance = desc.VisibleWorldDistance;
                m_markerVisibleCourseNodeDistance = desc.VisibleCourseNodeDistance;
                m_markerAlpha = desc.Alpha;
                m_markerAlphaADS = desc.AlphaADS;
                m_markerAlwaysVisible = desc.AlwaysVisible;
                m_markerPingFadeOutTime = desc.PingFadeOutTime;
                if (desc.UseCustomIcon)
                {
                    if (!TryGetCustomIcon(m_item.ItemDataBlock.persistentID, out var sprite))
                        return;
                    var renderers = m_marker.m_iconHolder.GetComponentsInChildren<SpriteRenderer>(true);
                    foreach (var renderer in renderers)
                    {
                        renderer.sprite = sprite;
                    }
                }
            }

            IsDiscovered = false;
            m_marker.SetVisible(false);
            enabled = m_markerVisibleUpdateMode == VisibleUpdateModeType.World;

            ItemMarkerLookup[m_item.GetInstanceID()] = this;
            ItemMarkerModeLookup[m_markerVisibleUpdateMode].Add(this);

            GameEventAPI.RegisterSelf(this);

            CoroutineManager.StartPersistantCoroutine(MarkerAlphaUpdater().WrapToIl2Cpp());
        }

        private IEnumerator MarkerAlphaUpdater()
        {
            var yielder = new WaitForFixedUpdate();
            while (m_marker)
            {
                if (m_marker.IsVisible)
                {
                    if (AimButtonHeld)
                        m_marker.SetAlpha(m_markerAlphaADS);
                    else
                        m_marker.SetAlpha(m_markerAlpha);
                }
                yield return yielder;
            }
        }

        private void Update()
        {
            if (m_marker == null || m_item == null)
                return;

            if (!IsPlacedInLevel)
                return;

            if (m_updateTimer > Clock.Time)
                return;

            m_updateTimer = Clock.Time + 0.2f;

            if (m_markerForceVisibleTimer >= Clock.Time)
            {
                if (!m_marker.IsVisible)
                    AttemptInteract(eNavMarkerInteractionType.Show);
                return;
            }

            if (!IsDiscovered)
            {
                if (m_marker.IsVisible)
                    AttemptInteract(eNavMarkerInteractionType.Hide);
                return;
            }


            if (m_markerAlwaysVisible)
            {
                if (!m_marker.IsVisible)
                    AttemptInteract(eNavMarkerInteractionType.Show);
                return;
            }

            if (LocalPlayerAgent == null)
                return;

            if (m_markerVisibleUpdateMode == VisibleUpdateModeType.World)
            {
                if (Vector3.Distance(m_item.transform.position, LocalPlayerAgent.transform.position) <= m_markerVisibleWorldDistance)
                {
                    if (!m_marker.IsVisible)
                        AttemptInteract(eNavMarkerInteractionType.Show);
                }
                else
                {
                    if (m_marker.IsVisible)
                        AttemptInteract(eNavMarkerInteractionType.Hide);
                }
                return;
            }
        }

        private void OnDisable()
        {
            if (!m_marker)
                return;
            if (m_marker.IsVisible)
                AttemptInteract(eNavMarkerInteractionType.Hide);
        }

        private void OnDestroy()
        {
            ItemMarkerLookup.Remove(m_item.GetInstanceID());
            ItemMarkerModeLookup[m_markerVisibleUpdateMode].Remove(this);
            GuiManager.NavMarkerLayer.RemoveMarker(m_marker);

            GameEventAPI.UnregisterSelf(this);
        }

        public void ManualUpdate()
        {
            if (m_marker == null || m_item == null)
                return;

            if (!IsPlacedInLevel)
            {
                if (m_marker.IsVisible)
                    AttemptInteract(eNavMarkerInteractionType.Hide);
                return;
            }

            if (m_markerForceVisibleTimer >= Clock.Time)
            {
                if (!m_marker.IsVisible)
                    AttemptInteract(eNavMarkerInteractionType.Show);
                return;
            }

            if (!IsDiscovered)
            {
                if (m_marker.IsVisible)
                    AttemptInteract(eNavMarkerInteractionType.Hide);
                return;
            }

            if (m_item.Get_pItemData().custom.byteState > 0) // CELL, HSU...
            {
                AttemptInteract(eNavMarkerInteractionType.Hide);
                return;
            }

            if (m_markerAlwaysVisible)
            {
                if (!m_marker.IsVisible)
                {
                    AttemptInteract(eNavMarkerInteractionType.Show);
                }
                return;
            }

            if (LocalPlayerAgent == null)
                return;

            if (m_item.CourseNode == null)
                return;

            if (LocalPlayerAgent.CourseNode == null)
            {
                AttemptInteract(eNavMarkerInteractionType.Hide);
                return;
            }

            switch (m_markerVisibleUpdateMode)
            {
                case VisibleUpdateModeType.Zone:
                    if (m_item.CourseNode.m_zone.ID == LocalPlayerAgent.CourseNode.m_zone.ID)
                        AttemptInteract(eNavMarkerInteractionType.Show);
                    else
                        AttemptInteract(eNavMarkerInteractionType.Hide);
                    break;
                case VisibleUpdateModeType.CourseNode:
                    if (AIG_CourseGraph.GetDistanceBetweenToNodes(m_item.CourseNode, LocalPlayerAgent.CourseNode) <= m_markerVisibleCourseNodeDistance)
                        AttemptInteract(eNavMarkerInteractionType.Show);
                    else
                        AttemptInteract(eNavMarkerInteractionType.Hide);
                    break;
                case VisibleUpdateModeType.Manual:
                    break;
            }
        }

        private IEnumerator HideDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            ManualUpdate();
        }

        public void OnPlayerPing()
        {
            if (!IsDiscovered)
                IsDiscovered = true;

            if (!IsPlacedInLevel)
                return;

            AttemptInteract(eNavMarkerInteractionType.Show);
            m_marker.Ping(m_markerPingFadeOutTime);
            m_markerForceVisibleTimer = Clock.Time + m_markerPingFadeOutTime;

            if (m_markerVisibleUpdateMode != VisibleUpdateModeType.World)
                CoroutineManager.StartCoroutine(HideDelay(m_markerPingFadeOutTime + 0.01f).WrapToIl2Cpp());
        }

        public void OnTerminalPing()
        {
            if (!IsPlacedInLevel)
                return;

            AttemptInteract(eNavMarkerInteractionType.Show);
            m_marker.Ping(m_markerPingFadeOutTime);
            m_markerForceVisibleTimer = Clock.Time + m_markerPingFadeOutTime;

            if (m_markerVisibleUpdateMode != VisibleUpdateModeType.World)
                CoroutineManager.StartCoroutine(HideDelay(m_markerPingFadeOutTime + 0.01f).WrapToIl2Cpp());
        }


        public void AttemptInteract(eNavMarkerInteractionType interaction)
        {
            if (interaction == eNavMarkerInteractionType.Show)
                m_marker.SetVisible(true);
            else if (interaction == eNavMarkerInteractionType.Hide)
                m_marker.SetVisible(false);
        }

        public void UpdateItemUsesLeft()
        {
            if (m_itemShowUses)
            {
                m_itemUsesLeft = Mathf.FloorToInt(m_item.Get_pItemData().custom.ammo / m_itemCostOfBullet);
                m_marker.SetTitle($"{m_markerTitle} ×{m_itemUsesLeft}");
            }
        }

        public void OnResetSession()
        {
            m_buffers.Clear();
        }

        public void OnRecallComplete(eBufferType bufferType)
        {
            if (GameStateManager.CurrentStateName < eGameStateName.InLevel)
                return;

            if (!RecallBuffer(bufferType))
                IsPlacedInLevel = m_item.internalSync.GetCurrentState().status == ePickupItemStatus.PlacedInLevel;

            UpdateItemUsesLeft();
            if (m_markerVisibleUpdateMode == VisibleUpdateModeType.World)
                Update();
            else
                ManualUpdate();
        }

        private void CaptureToBuffer(eBufferType bufferType)
        {
            var state = new pResourceMarkerState();
            state.isDiscovered = IsDiscovered;
            state.isPlacedInLevel = IsPlacedInLevel;
            m_buffers[bufferType] = state;
        }

        public bool RecallBuffer(eBufferType bufferType)
        {
            if (!m_buffers.TryGetValue(bufferType, out var state))
                return false;

            IsDiscovered = state.isDiscovered;
            IsPlacedInLevel = state.isPlacedInLevel;
            return true;
        }

        public void OnBufferCommand(pBufferCommand command)
        {
            if (command.operation == eBufferOperationType.StoreGameState)
            {
                CaptureToBuffer(command.type);
            }
        }

        public bool IsPlacedInLevel
        {
            get => m_isPlacedInLevel;
            set
            {
                m_isPlacedInLevel = value;
                m_markerForceVisibleTimer = 0f;
                enabled = m_markerVisibleUpdateMode == VisibleUpdateModeType.World && value;
                if (m_navMarkerPlacer?.m_marker != null)
                {
                    m_navMarkerPlacer.SetMarkerVisible(false);
                }
                if (!value)
                {
                    if (m_marker.m_pingRoutine != null)
                    {
                        CoroutineManager.StopCoroutine(m_marker.m_pingRoutine);
                        m_marker.Scale(m_marker.m_pingObj, m_marker.m_pinStartScale, m_marker.m_pinStartScale, Color.white, Color.white, 0f);
                    }
                    AttemptInteract(eNavMarkerInteractionType.Hide);
                }
                if (m_markerVisibleUpdateMode != VisibleUpdateModeType.World)
                    ManualUpdate();
            }
        }

        private bool AimButtonHeld
        {
            get
            {
                if (LocalPlayerAgent == null || !LocalPlayerAgent.Alive)
                    return false;
                var wieldSlot = LocalPlayerAgent.Inventory.WieldedSlot;
                if (wieldSlot < InventorySlot.GearStandard || wieldSlot > InventorySlot.GearClass)
                    return false;
                return LocalPlayerAgent.Inventory.WieldedItem?.AimButtonHeld ?? false;
            }
        }

        private bool MarkerIsVisibleAndInFocus => m_marker.IsVisible && m_marker.m_currentState == NavMarkerState.InFocus;
        private bool MarkerIsVisible => m_marker.IsVisible;
        public bool IsDiscovered
        {
            get => m_isDiscovered;
            set
            {
                m_isDiscovered = value;
                if (m_markerVisibleUpdateMode != VisibleUpdateModeType.World)
                    ManualUpdate();
            }
        }
        public bool m_isDiscovered = false;
        public bool AllowDiscoverCheck => m_allowDiscoverCheck;

        private bool m_markerAlwaysVisible = false;
        private float m_markerPingFadeOutTime = 12f;
        private VisibleUpdateModeType m_markerVisibleUpdateMode = VisibleUpdateModeType.World;
        private float m_markerVisibleWorldDistance = 30f;
        private int m_markerVisibleCourseNodeDistance = 1;
        private float m_markerAlpha = 1f;
        private float m_markerAlphaADS = 0.4f;
        private string m_markerTitle = string.Empty;
        private int m_itemUsesLeft;
        private ItemInLevel m_item;
        private float m_itemCostOfBullet = 1f;
        private bool m_itemShowUses;
        private InventorySlot m_itemSlot;
        private eNavMarkerStyle m_markerStyle = eNavMarkerStyle.PlayerPingLoot;

        private PlayerAgent LocalPlayerAgent 
        {
            get
            {
                if (m_localPlayer == null)
                    m_localPlayer = PlayerManager.GetLocalPlayerAgent();
                return m_localPlayer;
            }
        }

        private PlaceNavMarkerOnGO m_navMarkerPlacer;
        private PlayerAgent m_localPlayer;
        private bool m_allowDiscoverCheck;
        private bool m_isPlacedInLevel = true;
        private NavMarker m_marker;
        private float m_updateTimer = 0f;
        private float m_markerForceVisibleTimer = 0f;

        public static VisibleUpdateModeType GetDefaultUpdateModeForSlot(InventorySlot slot)
        {
            switch (slot)
            {
                case InventorySlot.ResourcePack:
                case InventorySlot.Consumable:
                    return VisibleUpdateModeType.World;
                case InventorySlot.InPocket:
                case InventorySlot.Pickup:
                    return VisibleUpdateModeType.Zone;
                case InventorySlot.InLevelCarry:
                    return VisibleUpdateModeType.Manual;
                default:
                    return VisibleUpdateModeType.World;
            }
        }

        public static readonly InventorySlot[] ValidSlots = { InventorySlot.ResourcePack, InventorySlot.Consumable, InventorySlot.Pickup, InventorySlot.InPocket, InventorySlot.InLevelCarry };
        public static readonly Dictionary<int, ItemNavMarkerWrapper> ItemMarkerLookup = new();
        public static readonly Dictionary<VisibleUpdateModeType, HashSet<ItemNavMarkerWrapper>> ItemMarkerModeLookup = new()
        {
            { VisibleUpdateModeType.World, new() },
            { VisibleUpdateModeType.Zone, new() },
            { VisibleUpdateModeType.CourseNode, new() },
            { VisibleUpdateModeType.Manual, new() },
        };

        public struct pResourceMarkerState
        {
            public bool isDiscovered;
            public bool isPlacedInLevel;
        }

        private readonly Dictionary<eBufferType, pResourceMarkerState> m_buffers = new();
    }
}
