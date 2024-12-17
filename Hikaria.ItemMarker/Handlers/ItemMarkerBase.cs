using AIGraph;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hikaria.Core;
using Hikaria.Core.Interfaces;
using Hikaria.ItemMarker.Managers;
using LevelGeneration;
using Player;
using SNetwork;
using System.Collections;
using UnityEngine;

namespace Hikaria.ItemMarker.Handlers
{
    public class ItemMarkerBase : MonoBehaviour, IOnResetSession, IOnRecallComplete, IOnBufferCommand
    {
        protected virtual void Start()
        {
            if (m_marker == null)
            {
                Destroy(this);
                return;
            }
            var size = m_marker.m_title.rectTransform.sizeDelta;
            size.x *= 3;
            m_marker.m_title.rectTransform.sizeDelta = size;

            enabled = m_markerVisibleUpdateMode == ItemMarkerVisibleUpdateModeType.World;
            if (!enabled)
                CoroutineManager.StartPersistantCoroutine(UpdateMarkerAlphaCoroutine().WrapToIl2Cpp());
            GameEventAPI.RegisterListener(this);
            ItemMarkerManager.RegisterItemMarkerAutoUpdate(this);
            if (m_terminalItem != null)
            {
                ItemMarkerManager.RegisterTerminalItemMarker(m_terminalItem.GetInstanceID(), this);
            }
        }

        public virtual void SetupNavMarker(Component comp)
        {
            m_trackingObj ??= gameObject;
            m_marker ??= GuiManager.NavMarkerLayer.PrepareGenericMarker(comp.gameObject);
            if (m_markerStyle == eNavMarkerStyle.LocationBeacon)
                m_markerStyle = GetComponentInChildren<iPlayerPingTarget>(true)?.PingTargetStyle ?? eNavMarkerStyle.LocationBeacon;
            m_marker.SetAlpha(m_markerAlpha);
            m_marker.SetStyle(m_markerStyle);
            m_marker.SetIconScale(m_markerIconScale);
            m_marker.SetColor(m_markerColor);
            m_marker.SetVisible(false);
            m_marker.m_title.fontSizeMax = m_markerTitleFontSizeMax;
            m_terminalItem ??= comp.GetComponentInChildren<LG_GenericTerminalItem>();
            if (!string.IsNullOrEmpty(m_markerTitle))
                m_marker.SetTitle(m_markerTitle);
            else if (m_terminalItem != null && !string.IsNullOrEmpty(m_terminalItem.TerminalItemKey))
                m_marker.SetTitle(m_terminalItem.TerminalItemKey);
            else
                m_marker.SetTitle(comp.gameObject.name);
            m_marker.m_title.rectTransform.sizeDelta = new Vector2(m_marker.m_title.rectTransform.sizeDelta.x * 3f, m_marker.m_title.rectTransform.sizeDelta.y);
            if (m_markerAlwaysShowTitle)
            {
                m_marker.m_stateOptions[(int)NavMarkerState.Visible] |= NavMarkerOption.Title;
            }
            if (m_markerAlwaysShowDistance)
            {
                m_marker.m_stateOptions[(int)NavMarkerState.Visible] |= NavMarkerOption.Distance;
            }
            foreach (var collider in comp.GetComponentsInChildren<Collider>(true))
            {
                if (collider.GetComponent<ItemMarkerTag>() == null)
                    collider.gameObject.AddComponent<ItemMarkerTag>().Setup(this);
            }
        }

        public virtual void OnPlayerPing()
        {
            if (!IsDiscovered)
                IsDiscovered = true;

            AttemptInteract(eNavMarkerInteractionType.Show);
            m_marker.Ping(m_markerPingFadeOutTime);
            m_markerForceVisibleTimer = Clock.Time + m_markerPingFadeOutTime;

            if (!enabled)
                CoroutineManager.StartCoroutine(HideDelay(m_markerPingFadeOutTime + 0.01f).WrapToIl2Cpp());
        }

        public virtual void OnTerminalPing()
        {
            AttemptInteract(eNavMarkerInteractionType.Show);
            m_marker.Ping(m_markerPingFadeOutTime);
            m_markerForceVisibleTimer = Clock.Time + m_markerPingFadeOutTime;

            if (!enabled)
                CoroutineManager.StartCoroutine(HideDelay(m_markerPingFadeOutTime + 0.01f).WrapToIl2Cpp());
        }

        public virtual void OnPlayerCourseNodeChanged(AIG_CourseNode newNode)
        {
            if (!IsDiscovered)
                return;
            if (newNode == null)
            {
                AttemptInteract(eNavMarkerInteractionType.Hide);
                return;
            }
            if (AIG_CourseGraph.GetDistanceBetweenToNodes(CourseNode, LocalPlayerAgent.CourseNode) <= m_markerVisibleCourseNodeDistance)
                AttemptInteract(eNavMarkerInteractionType.Show);
            else
                AttemptInteract(eNavMarkerInteractionType.Hide);
        }

        public virtual void OnPlayerZoneChanged(LG_Zone newZone)
        {
            if (!IsDiscovered)
                return;
            if (newZone == null)
            {
                AttemptInteract(eNavMarkerInteractionType.Hide);
                return;
            }
            if (CourseNode.m_zone.ID == newZone.ID)
                AttemptInteract(eNavMarkerInteractionType.Show);
            else
                AttemptInteract(eNavMarkerInteractionType.Hide);
        }

        public virtual void OnPlayerDimensionChanged(Dimension newDim)
        {
            if (!IsDiscovered)
                return;
            if (newDim == null)
            {
                AttemptInteract(eNavMarkerInteractionType.Hide);
                return;
            }
            if (CourseNode.m_dimension.DimensionIndex == newDim.DimensionIndex)
                AttemptInteract(eNavMarkerInteractionType.Show);
            else
                AttemptInteract(eNavMarkerInteractionType.Hide);
        }

        public virtual void SetTerminalItemKey(string key)
        {
            if (m_marker != null)
                m_marker.SetTitle(key);
        }

        public bool IsDiscovered
        {
            get => m_isDiscovered;
            set
            {
                m_isDiscovered = value;
                ForceUpdate();
            }
        }

        public void AttemptInteract(eNavMarkerInteractionType interaction)
        {
            if (interaction == eNavMarkerInteractionType.Show)
            {
                if (!m_marker.IsVisible)
                    m_marker.SetVisible(true);
            }
            else if (interaction == eNavMarkerInteractionType.Hide)
            {
                if (m_marker.IsVisible)
                    m_marker.SetVisible(false);
            }
        }

        public virtual void Update()
        {
            if (m_marker == null)
                return;

            if (m_updateTimer > Clock.Time)
                return;

            m_updateTimer = Clock.Time + 0.2f;

            if (m_markerForceVisibleTimer >= Clock.Time)
            {
                AttemptInteract(eNavMarkerInteractionType.Show);
                return;
            }

            if (!IsDiscovered)
            {
                AttemptInteract(eNavMarkerInteractionType.Hide);
                return;
            }

            if (LocalPlayerAgent == null)
                return;

            if (m_markerVisibleUpdateMode == ItemMarkerVisibleUpdateModeType.World)
                OnWorldUpdate();
        }

        public virtual void OnWorldUpdate()
        {
            if (Vector3.Distance(m_marker.TrackingTrans.position, LocalPlayerAgent.transform.position) <= m_markerVisibleWorldDistance)
                AttemptInteract(eNavMarkerInteractionType.Show);
            else
                AttemptInteract(eNavMarkerInteractionType.Hide);
        }

        public virtual void OnDestroy()
        {
            if (m_marker != null)
                GuiManager.NavMarkerLayer.RemoveMarker(m_marker);
            GameEventAPI.UnregisterListener(this);
            ItemMarkerManager.UnregisterItemMarkerAutoUpdate(this);
        }

        public virtual void FixedUpdate()
        {
            UpdateMarkerAlpha();
        }

        protected void ForceUpdate()
        {
            if (m_marker == null)
                return;

            if (m_markerForceVisibleTimer >= Clock.Time)
            {
                AttemptInteract(eNavMarkerInteractionType.Show);
                return;
            }

            if (!IsDiscovered)
            {
                AttemptInteract(eNavMarkerInteractionType.Hide);
                return;
            }

            if (LocalPlayerAgent == null)
            {
                AttemptInteract(eNavMarkerInteractionType.Hide);
                return;
            }

            switch (m_markerVisibleUpdateMode)
            {
                case ItemMarkerVisibleUpdateModeType.World:
                    OnWorldUpdate();
                    break;
                case ItemMarkerVisibleUpdateModeType.CourseNode:
                    OnPlayerCourseNodeChanged(LocalPlayerAgent.CourseNode);
                    break;
                case ItemMarkerVisibleUpdateModeType.Zone:
                    OnPlayerZoneChanged(LocalPlayerAgent.m_lastEnteredZone);
                    break;
                case ItemMarkerVisibleUpdateModeType.Dimension:
                    OnPlayerDimensionChanged(LocalPlayerAgent.Dimension);
                    break;
                case ItemMarkerVisibleUpdateModeType.Manual:
                    OnManualUpdate();
                    break;
            }
        }

        public virtual void OnManualUpdate() { }

        protected IEnumerator HideDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            ForceUpdate();
        }

        private IEnumerator UpdateMarkerAlphaCoroutine()
        {
            var yielder = new WaitForFixedUpdate();

            while (m_marker)
            {
                UpdateMarkerAlpha();
                yield return yielder;
            }
        }


        public void UpdateMarkerAlpha()
        {
            if (m_marker.IsVisible)
            {
                if (AimButtonHeld)
                    m_marker.SetAlpha(m_markerAlphaADS);
                else
                    m_marker.SetAlpha(m_markerAlpha);
            }
        }

        public void OnResetSession()
        {
            ResetBuffer();
        }

        protected virtual void ResetBuffer()
        {
            m_basicBuffers.Clear();
        }

        public void OnRecallComplete(eBufferType bufferType)
        {
            if (GameStateManager.CurrentStateName < eGameStateName.InLevel)
                return;

            RecallBuffer(bufferType);

            ForceUpdate();
        }

        protected virtual void CaptureToBuffer(eBufferType bufferType)
        {
            m_basicBuffers[bufferType] = new()
            {
                IsDiscovered = m_isDiscovered,
                IsVisible = m_marker.IsVisible
            };
        }

        protected virtual void RecallBuffer(eBufferType bufferType)
        {
            if (!m_basicBuffers.TryGetValue(bufferType, out var state))
                return;

            m_marker.SetVisible(state.IsVisible);
            m_isDiscovered = state.IsDiscovered;
        }

        public void OnBufferCommand(pBufferCommand command)
        {
            if (command.operation == eBufferOperationType.StoreGameState)
            {
                CaptureToBuffer(command.type);
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

        public PlayerAgent LocalPlayerAgent
        {
            get
            {
                if (m_localPlayer == null)
                    m_localPlayer = PlayerManager.GetLocalPlayerAgent();
                return m_localPlayer;
            }
        }

        public virtual bool AllowDiscoverScan => !m_isDiscovered;
        public virtual AIG_CourseNode CourseNode => m_terminalItem.SpawnNode;

        internal ItemMarkerVisibleUpdateModeType VisibleUpdateMode => m_markerVisibleUpdateMode;

        public int TerminalItemInstanceID { get; protected set; } = 0;

        protected bool m_markerAlwaysShowTitle = false;
        protected bool m_markerAlwaysShowDistance = false;
        protected float m_markerPingFadeOutTime = 12f;
        protected ItemMarkerVisibleUpdateModeType m_markerVisibleUpdateMode = ItemMarkerVisibleUpdateModeType.World;
        protected float m_markerVisibleWorldDistance = 30f;
        protected int m_markerVisibleCourseNodeDistance = 1;
        protected float m_markerAlpha = 0.9f;
        protected float m_markerAlphaADS = 0.4f;
        protected Color m_markerColor = Color.white;
        protected string m_markerTitle = string.Empty;
        protected eNavMarkerStyle m_markerStyle = eNavMarkerStyle.LocationBeacon;
        protected float m_markerIconScale = 0.4f;
        protected int m_markerTitleFontSizeMax = 50;

        protected LG_GenericTerminalItem m_terminalItem;
        protected GameObject m_trackingObj;

        protected NavMarker m_marker;
        protected float m_updateTimer = 0f;
        protected float m_markerForceVisibleTimer = 0f;

        private struct pBasicState
        {
            public bool IsDiscovered;
            public bool IsVisible;
        }

        private readonly Dictionary<eBufferType, pBasicState> m_basicBuffers = new();

        private PlayerAgent m_localPlayer;
        private bool m_isDiscovered;
    }
}

public enum ItemMarkerVisibleUpdateModeType
{
    World,
    CourseNode,
    Zone,
    Dimension,
    Manual
}
