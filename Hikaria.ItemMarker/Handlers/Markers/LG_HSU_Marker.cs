using LevelGeneration;
using UnityEngine;

namespace Hikaria.ItemMarker.Handlers.Markers
{
    public class LG_HSU_Marker : ItemMarkerBase
    {
        public override void SetupNavMarker(Component comp)
        {
            m_hsu = comp.Cast<LG_HSU>();
            m_marker = GuiManager.NavMarkerLayer.PrepareGenericMarker(comp.gameObject);
            m_markerColor = new Color(0.467f, 0.098f, 1f);
            m_markerVisibleUpdateMode = ItemMarkerVisibleUpdateModeType.Manual;
            m_markerStyle = eNavMarkerStyle.PlayerPingHSU;
            m_terminalItem = m_hsu.m_terminalItem.Cast<LG_GenericTerminalItem>();
            m_markerTitle = m_terminalItem.TerminalItemKey;
            m_markerShowPin = true;

            base.SetupNavMarker(comp);
        }

        protected override void OnManualUpdate()
        {
            if (m_hsu.m_pickupSampleInteraction.IsActive)
                AttemptInteract(eNavMarkerInteractionType.Show);
            else
                AttemptInteract(eNavMarkerInteractionType.Hide);
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

        private LG_HSU m_hsu;
    }
}
