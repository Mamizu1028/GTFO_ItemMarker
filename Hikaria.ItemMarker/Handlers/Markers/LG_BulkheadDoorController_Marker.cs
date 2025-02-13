using LevelGeneration;
using UnityEngine;

namespace Hikaria.ItemMarker.Handlers.Markers
{
    public class LG_BulkheadDoorController_Marker : ItemMarkerBase
    {
        public override void SetupNavMarker(Component comp)
        {
            m_bulkDc = comp.Cast<LG_BulkheadDoorController_Core>();
            m_bulkDc.add_OnStateChangeCallback((Action<eBulkheadDCStatus, eBulkheadDCStatus>)OnStateChangeCallback);
            m_marker = GuiManager.NavMarkerLayer.PrepareGenericMarker(comp.gameObject);
            m_markerColor = new Color(0.573f, 0.663f, 0.718f);
            m_markerVisibleUpdateMode = ItemMarkerVisibleUpdateModeType.Manual;
            m_markerStyle = eNavMarkerStyle.PlayerPingBulkheadDC;
            m_terminalItem = m_bulkDc.m_terminalItem.Cast<LG_GenericTerminalItem>();
            m_markerTitle = m_terminalItem.TerminalItemKey;
            m_markerShowPin = true;

            base.SetupNavMarker(comp);
        }

        private void OnStateChangeCallback(eBulkheadDCStatus oldState, eBulkheadDCStatus newState)
        {
            if (newState == eBulkheadDCStatus.InactiveNoMoreInteraction)
            {
                AttemptInteract(eNavMarkerInteractionType.Hide);
                return;
            }
            AttemptInteract(eNavMarkerInteractionType.Show);
        }

        protected override void OnManualUpdate()
        {
            if (m_bulkDc.m_stateReplicator.State.status == eBulkheadDCStatus.InactiveNoMoreInteraction)
            {
                AttemptInteract(eNavMarkerInteractionType.Hide);
                return;
            }
            AttemptInteract(eNavMarkerInteractionType.Show);
        }

        protected override void OnDevUpdate()
        {
            if (m_markerForceVisibleTimer >= Clock.Time)
            {
                AttemptInteract(eNavMarkerInteractionType.Show);
                return;
            }

            OnManualUpdate();
        }

        private LG_BulkheadDoorController_Core m_bulkDc;
    }
}
