using LevelGeneration;
using UnityEngine;

namespace Hikaria.ItemMarker.Handlers.Markers
{
    public class LG_SecurityDoor_Locks_Marker : ItemMarkerBase
    {
        public override void SetupNavMarker(Component comp)
        {
            m_locks = comp.Cast<LG_SecurityDoor_Locks>();
            m_door = m_locks.m_door;
            m_marker = GuiManager.NavMarkerLayer.PlaceCustomMarker(NavMarkerOption.WaypointTitle, m_locks.m_intOpenDoor.gameObject, string.Empty, 0, false);
            m_markerColor = new Color(0.467f, 0.098f, 1f);
            m_markerVisibleUpdateMode = ItemMarkerVisibleUpdateModeType.Manual;
            m_terminalItem = m_door.m_terminalItem.Cast<LG_GenericTerminalItem>();
            m_markerTitle = m_terminalItem.TerminalItemKey;
            m_markerStyle = eNavMarkerStyle.LocationBeacon;
            m_markerIconScale = 0.275f;
            m_locks.add_OnApproached((Action)OnApproached);
            m_door.m_sync.add_OnDoorStateChange((Action<pDoorState, bool>)OnDoorStateChange);

            base.SetupNavMarker(comp);

            m_marker.SetVisualStates(NavMarkerOption.WaypointTitle, NavMarkerOption.WaypointTitle, NavMarkerOption.Empty);
            m_marker.m_titleSubObj.transform.localScale = Vector3.one * 1.75f;
            m_marker.m_title.fontSizeMax = 50;
            m_marker.m_title.fontSizeMin = 15;
        }

        private void OnApproached()
        {
            IsDiscovered = true;
        }

        private void OnDoorStateChange(pDoorState state, bool isRecall)
        {
            switch (state.status)
            {
                case eDoorStatus.Closed_LockedWithBulkheadDC:
                    m_markerColor = ColorExt.Hex("#92A9B7");
                    m_marker.SetColor(m_markerColor);
                    m_markerTitle = $"<color=orange>::REQ::</color>\n<color=white>{m_locks.LinkedBulkheadDoorController.PublicName}</color>";
                    m_marker.SetTitle(m_markerTitle);
                    AttemptInteract(eNavMarkerInteractionType.Show);
                    break;
                case eDoorStatus.Closed_LockedWithKeyItem:
                    m_markerColor = ColorExt.Hex("#FF9DE7");
                    m_marker.SetColor(m_markerColor);
                    m_markerTitle = $"<color=orange>::REQ::</color>\n<color=white>{m_locks.m_gateKeyItemNeeded.PublicName}</color>";
                    m_marker.SetTitle(m_markerTitle);
                    AttemptInteract(eNavMarkerInteractionType.Show);
                    break;
                case eDoorStatus.Closed_LockedWithPowerGenerator:
                    m_markerColor = ColorExt.Hex("#FFA500");
                    m_marker.SetColor(m_markerColor);
                    m_markerTitle = $"<color=orange>::REQ::</color>\n<color=white>{m_locks.m_powerGeneratorNeeded.PublicName}</color>";
                    m_marker.SetTitle(m_markerTitle);
                    AttemptInteract(eNavMarkerInteractionType.Show);
                    break;
                case eDoorStatus.Unlocked:
                case eDoorStatus.Open:
                case eDoorStatus.Opening:
                case eDoorStatus.Closed:
                default:
                    AttemptInteract(eNavMarkerInteractionType.Hide);
                    break;
            }
        }

        protected override void OnManualUpdate()
        {
            switch (m_door.m_sync.GetCurrentSyncState().status)
            {
                case eDoorStatus.Closed_LockedWithBulkheadDC:
                    m_markerColor = ColorExt.Hex("#92A9B7");
                    m_marker.SetColor(m_markerColor);
                    m_markerTitle = $"<color=orange>::REQ::</color>\n<color=white>{m_locks.LinkedBulkheadDoorController.PublicName}</color>";
                    m_marker.SetTitle(m_markerTitle);
                    AttemptInteract(eNavMarkerInteractionType.Show);
                    break;
                case eDoorStatus.Closed_LockedWithKeyItem:
                    m_markerColor = ColorExt.Hex("#FF9DE7");
                    m_marker.SetColor(m_markerColor);
                    m_markerTitle = $"<color=orange>::REQ::</color>\n<color=white>{m_locks.m_gateKeyItemNeeded.PublicName}</color>";
                    m_marker.SetTitle(m_markerTitle);
                    AttemptInteract(eNavMarkerInteractionType.Show);
                    break;
                case eDoorStatus.Closed_LockedWithPowerGenerator:
                    m_markerColor = ColorExt.Hex("#FFA500");
                    m_marker.SetColor(m_markerColor);
                    m_markerTitle = $"<color=orange>::REQ::</color>\n<color=white>{m_locks.m_powerGeneratorNeeded.PublicName}</color>";
                    m_marker.SetTitle(m_markerTitle);
                    AttemptInteract(eNavMarkerInteractionType.Show);
                    break;
                case eDoorStatus.Unlocked:
                case eDoorStatus.Open:
                case eDoorStatus.Opening:
                case eDoorStatus.Closed:
                default:
                    AttemptInteract(eNavMarkerInteractionType.Hide);
                    break;
            }
        }

        protected override void OnDevUpdate()
        {
            OnManualUpdate();
        }

        private LG_SecurityDoor_Locks m_locks;
        private LG_SecurityDoor m_door;
    }
}
