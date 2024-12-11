using AIGraph;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hikaria.Core;
using Hikaria.Core.Interfaces;
using LevelGeneration;
using Player;
using SNetwork;
using System.Collections;
using UnityEngine;
using static Hikaria.ResourceMarker.Managers.ResourceMarkerManager;

namespace Hikaria.ResourceMarker.Handlers
{
    public class ResourceNavMarkerWrapper : MonoBehaviour, IOnResetSession, IOnRecallComplete, IOnBufferCommand
    {
        public void Awake()
        {
            m_item = GetComponent<ItemInLevel>();
            var itemDataBlock = m_item.ItemDataBlock;
            if (!ValidSlots.Contains(itemDataBlock.inventorySlot))
            {
                Destroy(this);
                return;
            }
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
            m_allowDiscoverCheck = m_item.container == null;

            m_marker = GuiManager.NavMarkerLayer.PrepareGenericMarker(gameObject);
            m_markerStyle = GetComponentInChildren<iPlayerPingTarget>()?.PingTargetStyle ?? eNavMarkerStyle.PlayerPingLoot;
            m_markerTitle = m_item.ItemDataBlock.publicName.Replace('_', ' ');
            m_marker.SetTitle(m_markerTitle);
            m_marker.SetStyle(m_markerStyle);
            m_marker.SetIconScale(0.6f);
            m_marker.SetAlpha(1f);

            if (ResourceMarkerDescriptions.Value.TryGetValue(m_item.ItemDataBlock.persistentID, out var desc))
            {
                m_marker.SetColor(desc.Color);
                m_markerTitle = desc.Title;
                m_marker.SetTitle(m_markerTitle);
                m_marker.SetIconScale(desc.IconScale);
                m_markerVisibleCheckMode = desc.VisibleCheckMode;
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
            if (!ResourceMarkerModeLookup.TryGetValue(m_markerVisibleCheckMode, out var markers))
            {
                markers = new();
                ResourceMarkerModeLookup[m_markerVisibleCheckMode] = markers;
            }
            markers.Add(this);

            IsDiscovered = false;
            m_marker.SetVisible(false);
            enabled = m_markerVisibleCheckMode == VisibleCheckModeType.World;

            ResourceMarkerLookup[m_item.GetInstanceID()] = this;

            GameEventAPI.RegisterSelf(this);
        }

        private void Update()
        {
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

            if (m_markerVisibleCheckMode == VisibleCheckModeType.World)
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

        private void FixedUpdate()
        {
            if (!IsPlacedInLevel)
                return;

            if (m_marker.IsVisible)
            {
                if (AimButtonHeld)
                    m_marker.SetAlpha(m_markerAlphaADS);
                else
                    m_marker.SetAlpha(m_markerAlpha);
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
            ResourceMarkerLookup.Remove(m_item.GetInstanceID());
            ResourceMarkerModeLookup[m_markerVisibleCheckMode].Remove(this);
            GuiManager.NavMarkerLayer.RemoveMarker(m_marker);

            GameEventAPI.UnregisterSelf(this);
        }

        public void ManualUpdate()
        {
            if (!IsPlacedInLevel)
                return;

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

            if (m_item.CourseNode == null)
                return;

            if (LocalPlayerAgent.CourseNode == null)
            {
                AttemptInteract(eNavMarkerInteractionType.Hide);
                return;
            }

            switch (m_markerVisibleCheckMode)
            {
                case VisibleCheckModeType.Zone:
                    if (m_item.CourseNode.m_zone.ID == LocalPlayerAgent.CourseNode.m_zone.ID)
                        AttemptInteract(eNavMarkerInteractionType.Show);
                    else
                        AttemptInteract(eNavMarkerInteractionType.Hide);
                    break;
                case VisibleCheckModeType.CourseNode:
                    if (AIG_CourseGraph.GetDistanceBetweenToNodes(m_item.CourseNode, LocalPlayerAgent.CourseNode) <= m_markerVisibleCourseNodeDistance)
                        AttemptInteract(eNavMarkerInteractionType.Show);
                    else
                        AttemptInteract(eNavMarkerInteractionType.Hide);
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

            AttemptInteract(eNavMarkerInteractionType.Show);
            m_marker.Ping(m_markerPingFadeOutTime);
            m_markerForceVisibleTimer = Clock.Time + m_markerPingFadeOutTime;

            if (m_markerVisibleCheckMode != VisibleCheckModeType.World)
                CoroutineManager.StartCoroutine(HideDelay(m_markerPingFadeOutTime + 0.01f).WrapToIl2Cpp());
        }

        public void OnTerminalPing()
        {
            AttemptInteract(eNavMarkerInteractionType.Show);
            m_marker.Ping(m_markerPingFadeOutTime);
            m_markerForceVisibleTimer = Clock.Time + m_markerPingFadeOutTime;

            if (m_markerVisibleCheckMode != VisibleCheckModeType.World)
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
            if (!RecallBuffer(bufferType))
            {
                IsPlacedInLevel = m_item.internalSync.GetCurrentState().status == ePickupItemStatus.PlacedInLevel;
            }

            UpdateItemUsesLeft();
            Update();
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
                m_markerForceVisibleTimer = 0f;
                m_isPlacedInLevel = value;
                enabled = m_markerVisibleCheckMode == VisibleCheckModeType.World && value;
                if (!value && m_marker.m_pingRoutine != null)
                {
                    CoroutineManager.StopCoroutine(m_marker.m_pingRoutine);
                    m_marker.Scale(m_marker.m_pingObj, m_marker.m_pinStartScale, m_marker.m_pinStartScale, Color.white, Color.white, 0f);
                }
                ManualUpdate();
            }
        }

        private bool AimButtonHeld
        {
            get
            {
                if (LocalPlayerAgent == null)
                    return false;
                var wieldSlot = LocalPlayerAgent.Inventory.WieldedSlot;
                if (wieldSlot < InventorySlot.GearStandard || wieldSlot > InventorySlot.GearClass)
                    return false;
                return LocalPlayerAgent.Inventory.WieldedItem?.AimButtonHeld ?? false;
            }
        }
        private bool MarkerIsVisibleAndInFocus => m_marker.IsVisible && m_marker.m_currentState == NavMarkerState.InFocus;
        private bool MarkerIsVisible => m_marker.IsVisible;
        public bool IsDiscovered { get; set; } = false;
        public bool AllowDiscoverCheck => m_allowDiscoverCheck;

        private bool m_markerAlwaysVisible = false;
        private float m_markerPingFadeOutTime = 12f;
        private VisibleCheckModeType m_markerVisibleCheckMode = VisibleCheckModeType.World;
        private float m_markerVisibleWorldDistance = 30f;
        private int m_markerVisibleCourseNodeDistance = 1;
        private float m_markerAlpha = 1f;
        private float m_markerAlphaADS = 0.4f;
        private string m_markerTitle = string.Empty;
        private int m_itemUsesLeft;
        private ItemInLevel m_item;
        private float m_itemCostOfBullet = 1f;
        private bool m_itemShowUses;
        private eNavMarkerStyle m_markerStyle = eNavMarkerStyle.PlayerPingLoot;

        private PlayerAgent LocalPlayerAgent 
        {
            get
            {
                if (!m_localPlayer)
                    m_localPlayer = PlayerManager.GetLocalPlayerAgent();
                return m_localPlayer;
            }
        }

        private PlayerAgent m_localPlayer;
        private bool m_allowDiscoverCheck;
        private bool m_isPlacedInLevel = true;
        private NavMarker m_marker;
        private float m_updateTimer = 0f;
        private float m_markerForceVisibleTimer = 0f;

        public static readonly InventorySlot[] ValidSlots = { InventorySlot.ResourcePack, InventorySlot.Consumable, InventorySlot.Pickup, InventorySlot.InPocket };
        public static readonly Dictionary<int, ResourceNavMarkerWrapper> ResourceMarkerLookup = new();
        public static readonly Dictionary<VisibleCheckModeType, HashSet<ResourceNavMarkerWrapper>> ResourceMarkerModeLookup = new()
        {
            { VisibleCheckModeType.World, new() },
            { VisibleCheckModeType.Zone, new() },
            { VisibleCheckModeType.CourseNode, new() },
        };

        public struct pResourceMarkerState
        {
            public bool isDiscovered;
            public bool isPlacedInLevel;
        }

        private readonly Dictionary<eBufferType, pResourceMarkerState> m_buffers = new();
    }
}
